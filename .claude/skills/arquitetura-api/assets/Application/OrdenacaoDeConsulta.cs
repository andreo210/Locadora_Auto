using {{RootNamespace}}.Application.Models.Dto;
using System.Linq.Expressions;

namespace {{RootNamespace}}.Application.Models.Consultas
{
    /// <summary>
    /// Traduz a coluna clicada na tela para o <c>OrderBy</c> correspondente, no formato que o
    /// <c>ObterPaginadoComFiltroAsync</c> espera.
    ///
    /// Substitui o <c>switch</c> que cada serviço de listagem repetia, com duas linhas por coluna
    /// (uma para cada direção). Aqui a coluna é declarada uma vez e a direção é aplicada pelo
    /// próprio mapa:
    ///
    /// <code>
    /// private static readonly OrdenacaoDeConsulta&lt;Reserva&gt; Ordenacoes =
    ///     OrdenacaoDeConsulta&lt;Reserva&gt;.Padrao(r => r.DataInicio, descendente: true)
    ///         .Com("datafim", r => r.DataFim)
    ///         .Com("status", r => r.Status);
    ///
    /// ordenarPor: Ordenacoes.Montar(consulta)
    /// </code>
    ///
    /// A expressão da chave é montada em tempo de compilação — nada de nome de coluna em string
    /// chegando ao SQL. Coluna desconhecida cai no padrão, que é o comportamento que a tela espera
    /// quando alguém edita a URL na mão.
    ///
    /// É imutável depois de montada: declare como <c>static readonly</c> no serviço, uma vez.
    /// </summary>
    public sealed class OrdenacaoDeConsulta<TEntity>
    {
        private readonly Dictionary<string, Func<IQueryable<TEntity>, bool, IOrderedQueryable<TEntity>>> _colunas
            = new(StringComparer.OrdinalIgnoreCase);

        private readonly Func<IQueryable<TEntity>, bool, IOrderedQueryable<TEntity>> _padrao;
        private readonly bool _descendentePorPadrao;

        private OrdenacaoDeConsulta(
            Func<IQueryable<TEntity>, bool, IOrderedQueryable<TEntity>> padrao,
            bool descendentePorPadrao)
        {
            _padrao = padrao;
            _descendentePorPadrao = descendentePorPadrao;
        }

        /// <summary>
        /// Ordenação usada quando o cliente não pede coluna nenhuma — ou pede uma que não existe.
        /// Paginação sem <c>ORDER BY</c> devolve páginas com item repetido ou faltando, então
        /// sempre existe um padrão.
        /// </summary>
        public static OrdenacaoDeConsulta<TEntity> Padrao<TChave>(
            Expression<Func<TEntity, TChave>> chave,
            bool descendente = false)
            => new(Aplicador(chave), descendente);

        /// <summary>Registra uma coluna ordenável. O nome é o que a tela manda em <c>ordenarPor</c>.</summary>
        public OrdenacaoDeConsulta<TEntity> Com<TChave>(string coluna, Expression<Func<TEntity, TChave>> chave)
        {
            _colunas[coluna] = Aplicador(chave);
            return this;
        }

        public Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> Montar(ConsultaPaginadaRequest consulta)
            => Montar(consulta.OrdenarPor, consulta.Direcao);

        public Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> Montar(string? coluna, string? direcao)
        {
            var aplicador = _padrao;
            var descendente = _descendentePorPadrao;

            if (!string.IsNullOrWhiteSpace(coluna) && _colunas.TryGetValue(coluna.Trim(), out var escolhida))
            {
                aplicador = escolhida;
                descendente = false;   // coluna escolhida na tela sobe, salvo pedido contrário
            }

            if (!string.IsNullOrWhiteSpace(direcao))
                descendente = string.Equals(direcao.Trim(), "desc", StringComparison.OrdinalIgnoreCase);

            return consulta => aplicador(consulta, descendente);
        }

        private static Func<IQueryable<TEntity>, bool, IOrderedQueryable<TEntity>> Aplicador<TChave>(
            Expression<Func<TEntity, TChave>> chave)
            => (consulta, descendente) => descendente
                ? consulta.OrderByDescending(chave)
                : consulta.OrderBy(chave);
    }
}
