using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Locadora_Auto.Tests.Fakes
{
    /// <summary>
    /// Banco em memória compartilhado entre os repositórios fake de um mesmo teste.
    /// Serviços que dependem de vários repositórios (Reserva depende de cliente, filial,
    /// categoria e veículo) precisam enxergar o mesmo conjunto de dados — por isso o armazém
    /// é passado para todos os fakes em vez de cada um ter a própria lista.
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

        public int ProximoId<TEntity>() where TEntity : class
        {
            var maior = Tabela<TEntity>()
                .Select(e => ChavePrimaria.Ler(e) as int? ?? 0)
                .DefaultIfEmpty(0)
                .Max();

            return maior + 1;
        }
    }

    /// <summary>
    /// Localiza a chave primária pela convenção das entidades do projeto: atributo [Key],
    /// depois <c>Id</c>, <c>Id{Entidade}</c>, <c>{Entidade}Id</c> e, por último, a primeira
    /// propriedade inteira começando com "Id" — que é o caso de <c>Clientes.IdCliente</c>,
    /// onde o nome da classe está no plural e o da chave não.
    /// </summary>
    internal static class ChavePrimaria
    {
        private static readonly Dictionary<Type, PropertyInfo?> Cache = new();

        public static PropertyInfo? Localizar(Type tipo)
        {
            if (Cache.TryGetValue(tipo, out var cacheada)) return cacheada;

            var propriedades = tipo.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            var encontrada =
                propriedades.FirstOrDefault(p => p.GetCustomAttribute<KeyAttribute>() != null)
                ?? propriedades.FirstOrDefault(p => p.Name == "Id")
                ?? propriedades.FirstOrDefault(p => p.Name == $"Id{tipo.Name}")
                ?? propriedades.FirstOrDefault(p => p.Name == $"{tipo.Name}Id")
                ?? propriedades.FirstOrDefault(p => p.Name.StartsWith("Id") && p.PropertyType == typeof(int));

            Cache[tipo] = encontrada;
            return encontrada;
        }

        public static object? Ler(object entidade)
            => Localizar(entidade.GetType())?.GetValue(entidade);

        /// <summary>Escreve na chave mesmo com set privado — é o que o EF faz depois do insert.</summary>
        public static void Definir(object entidade, int valor)
        {
            var chave = Localizar(entidade.GetType())
                ?? throw new InvalidOperationException(
                    $"Não foi possível localizar a chave primária de {entidade.GetType().Name}.");

            chave.SetValue(entidade, valor);
        }

        public static void AtribuirSeVazia(object entidade, int valor)
        {
            if (Ler(entidade) is int atual && atual == 0)
                Definir(entidade, valor);
        }
    }
}
