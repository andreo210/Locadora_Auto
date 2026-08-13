using System.Linq.Expressions;

namespace {{RootNamespace}}.Domain
{
    /// <summary>
    /// Contrato do repositório genérico. Fica no Domain porque quem define o que precisa ser
    /// buscado é o domínio; a Infra apenas implementa. Todo repositório concreto herda daqui
    /// através de uma interface própria (<c>IClienteRepository : IRepositorioGlobal&lt;Cliente&gt;</c>),
    /// que só declara método extra quando a consulta não cabe em filtro/ordenarPor/incluir.
    /// </summary>
    public interface IRepositorioGlobal<TEntity>
    {
        // ======================= CONSULTAS =======================

        /// <summary>Lista com filtro, ordenação e includes opcionais. Sem tracking por padrão.</summary>
        Task<IReadOnlyList<TEntity>> ObterAsync(
            Expression<Func<TEntity, bool>>? filtro = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? ordenarPor = null,
            Func<IQueryable<TEntity>, IQueryable<TEntity>>? incluir = null,
            bool rastreado = false,
            CancellationToken ct = default);

        /// <summary>Primeiro registro que atende ao filtro. Use <c>rastreado: true</c> para alterar depois.</summary>
        Task<TEntity?> ObterPrimeiroAsync(
            Expression<Func<TEntity, bool>> filtro,
            Func<IQueryable<TEntity>, IQueryable<TEntity>>? incluir = null,
            bool rastreado = false,
            CancellationToken ct = default);

        /// <summary>Query crua sem tracking. Use com parcimônia: vaza EF para quem chamar.</summary>
        IQueryable<TEntity> ObterTodos();

        Task<TEntity?> ObterPorIdAsync(object id, bool rastreado = false, CancellationToken ct = default);

        Task<bool> ExisteAsync(Expression<Func<TEntity, bool>> filtro, CancellationToken ct = default);

        Task<int> ContarAsync(Expression<Func<TEntity, bool>>? filtro = null, CancellationToken ct = default);

        Task<IReadOnlyList<TEntity>> ObterPaginadoAsync(
            Expression<Func<TEntity, bool>> filtro,
            int pagina,
            int itensPorPagina,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? ordenarPor = null,
            CancellationToken ct = default);

        /// <summary>
        /// Consulta paginada com total e metadados. Com <paramref name="pagina"/> e
        /// <paramref name="itensPorPagina"/> nulos devolve tudo numa página só.
        /// O tipo é inferido do filtro; o parâmetro genérico existe para permitir consultar
        /// outra entidade pelo mesmo contexto quando necessário.
        /// </summary>
        Task<PaginatedResult<TConsulta>> ObterPaginadoComFiltroAsync<TConsulta>(
            Expression<Func<TConsulta, bool>>? filtro = null,
            Func<IQueryable<TConsulta>, IOrderedQueryable<TConsulta>>? ordenarPor = null,
            Func<IQueryable<TConsulta>, IQueryable<TConsulta>>? incluir = null,
            int? pagina = null,
            int? itensPorPagina = null,
            bool asNoTracking = true,
            bool asSplitQuery = false,
            CancellationToken ct = default)
            where TConsulta : class;

        Task<IReadOnlyList<TConsulta>> ObterComFiltroAsync<TConsulta>(
            Expression<Func<TConsulta, bool>>? filtro = null,
            Func<IQueryable<TConsulta>, IOrderedQueryable<TConsulta>>? ordenarPor = null,
            Func<IQueryable<TConsulta>, IQueryable<TConsulta>>? incluir = null,
            bool asNoTracking = true,
            bool asSplitQuery = false,
            CancellationToken ct = default)
            where TConsulta : class;

        /// <summary>Aplica a projeção antes de materializar — evita trazer colunas que a tela não usa.</summary>
        Task<IReadOnlyList<TResult>> ObterComFiltroEProjecaoAsync<TConsulta, TResult>(
            Expression<Func<TConsulta, TResult>> projecao,
            Expression<Func<TConsulta, bool>>? filtro = null,
            Func<IQueryable<TConsulta>, IOrderedQueryable<TConsulta>>? ordenarPor = null,
            Func<IQueryable<TConsulta>, IQueryable<TConsulta>>? incluir = null,
            bool asNoTracking = true,
            bool asSplitQuery = false,
            CancellationToken ct = default)
            where TConsulta : class;

        // ======================= ESCRITAS =======================
        // O par "InserirSalvar/Inserir" existe para permitir várias escritas na mesma
        // unidade de trabalho: use as versões sem "Salvar" e feche com SalvarAsync
        // (ou dentro de IUnitOfWork.ExecuteTransactionAsync).

        Task<TEntity> InserirSalvarAsync(TEntity entidade, CancellationToken ct = default);

        Task<List<TEntity>> InserirSalvarListasAsync(List<TEntity> entidades, CancellationToken ct = default);

        Task InserirAsync(TEntity entidade, CancellationToken ct = default);

        Task<bool> AtualizarSalvarAsync(TEntity entidade, CancellationToken ct = default);

        void Atualizar(TEntity entidade);

        Task ExcluirSalvarAsync(TEntity entidade, CancellationToken ct = default);

        Task Excluir(TEntity entidade, CancellationToken ct = default);

        Task<int> SalvarAsync(CancellationToken ct = default);
    }
}
