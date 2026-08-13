using System.Collections;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace {{RootNamespace}}.Tests.Fakes
{
    /// <summary>
    /// Banco em memória compartilhado entre os repositórios fake de um mesmo teste.
    /// Serviços que dependem de vários repositórios precisam enxergar o mesmo conjunto de
    /// dados — por isso o armazém é passado para todos os fakes, em vez de cada um ter a
    /// própria lista.
    ///
    /// Uma instância por teste. Nunca guarde o armazém num campo estático: o xUnit executa
    /// coleções de teste em paralelo e o estado vazaria de um teste para outro.
    /// </summary>
    public sealed class ArmazemFake
    {
        private readonly Dictionary<Type, IList> _tabelas = new();

        public List<TEntity> Tabela<TEntity>() where TEntity : class
        {
            if (!_tabelas.TryGetValue(typeof(TEntity), out var tabela))
            {
                tabela = new List<TEntity>();
                _tabelas[typeof(TEntity)] = tabela;
            }

            return (List<TEntity>)tabela;
        }

        /// <summary>
        /// Coloca entidades no armazém como se já estivessem no banco, atribuindo id a quem
        /// ainda não tem — do contrário toda entidade nova ficaria com id 0 e os filtros por
        /// id casariam com a entidade errada.
        /// </summary>
        public ArmazemFake Semear<TEntity>(params TEntity[] entidades) where TEntity : class
        {
            foreach (var entidade in entidades)
            {
                ChavePrimaria.AtribuirSeVazia(entidade, ProximoId<TEntity>());
                Tabela<TEntity>().Add(entidade);
            }

            return this;
        }

        /// <summary>Imita a sequência do banco: maior id da tabela + 1.</summary>
        public int ProximoId<TEntity>() where TEntity : class
        {
            var maior = Tabela<TEntity>()
                .Select(e => ChavePrimaria.Ler(e) switch
                {
                    int inteiro => inteiro,
                    long longo => (int)longo,
                    _ => 0
                })
                .DefaultIfEmpty(0)
                .Max();

            return maior + 1;
        }
    }

    /// <summary>
    /// Localiza a chave primária pela convenção de nomes das entidades: atributo [Key],
    /// depois <c>Id</c>, <c>Id{Entidade}</c>, <c>{Entidade}Id</c> e, por último, a primeira
    /// propriedade de tipo de chave começando com "Id" — que é o caso de entidades cujo nome
    /// de classe está no plural e o da chave não (<c>Clientes.IdCliente</c>).
    ///
    /// O cache é <see cref="ConcurrentDictionary{TKey,TValue}"/> porque o xUnit roda coleções
    /// em paralelo e um <c>Dictionary</c> estático escrito por duas threads corrompe.
    /// </summary>
    internal static class ChavePrimaria
    {
        private static readonly ConcurrentDictionary<Type, PropertyInfo?> Cache = new();

        private static readonly Type[] TiposDeChave = { typeof(int), typeof(long), typeof(Guid) };

        public static PropertyInfo? Localizar(Type tipo) => Cache.GetOrAdd(tipo, t =>
        {
            var propriedades = t.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            return propriedades.FirstOrDefault(p => p.GetCustomAttribute<KeyAttribute>() != null)
                ?? propriedades.FirstOrDefault(p => p.Name == "Id")
                ?? propriedades.FirstOrDefault(p => p.Name == $"Id{t.Name}")
                ?? propriedades.FirstOrDefault(p => p.Name == $"{t.Name}Id")
                ?? propriedades.FirstOrDefault(p => p.Name.StartsWith("Id") && TiposDeChave.Contains(p.PropertyType));
        });

        public static object? Ler(object entidade)
            => Localizar(entidade.GetType())?.GetValue(entidade);

        /// <summary>Escreve na chave mesmo com set privado — é o que o EF faz depois do insert.</summary>
        public static void Definir(object entidade, object valor)
        {
            var chave = Localizar(entidade.GetType())
                ?? throw new InvalidOperationException(
                    $"Não foi possível localizar a chave primária de {entidade.GetType().Name}. " +
                    "Marque-a com [Key] ou siga a convenção Id/Id{Entidade}/{Entidade}Id.");

            chave.SetValue(entidade, Converter(valor, chave.PropertyType));
        }

        /// <summary>Só atribui se a chave ainda estiver zerada — não sobrescreve id que o teste fixou.</summary>
        public static void AtribuirSeVazia(object entidade, int proximoId)
        {
            switch (Ler(entidade))
            {
                case int atual when atual == 0:
                    Definir(entidade, proximoId);
                    break;

                case long atual when atual == 0:
                    Definir(entidade, (long)proximoId);
                    break;

                case Guid atual when atual == Guid.Empty:
                    Definir(entidade, Guid.NewGuid());
                    break;
            }
        }

        /// <summary>
        /// <c>SetValue</c> não converte: um int "encaixotado" numa propriedade long estoura.
        /// </summary>
        private static object Converter(object valor, Type tipoDaChave)
            => tipoDaChave == typeof(long) && valor is int inteiro ? (long)inteiro : valor;
    }
}
