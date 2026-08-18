using System.Linq.Expressions;

namespace Locadora_Auto.Domain
{
    public interface IRepositorioGlobal<TEntity>
    {
        Task<IReadOnlyList<TEntity>> ObterAsync(
            Expression<Func<TEntity, bool>>? filtro = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? ordenarPor = null,
            Func<IQueryable<TEntity>, IQueryable<TEntity>>? incluir = null,
            bool rastreado = false,
            CancellationToken ct = default);

        Task<TEntity?> ObterPrimeiroAsync(
            Expression<Func<TEntity, bool>> filtro,
            Func<IQueryable<TEntity>, IQueryable<TEntity>>? incluir = null,
            bool rastreado = false,
            CancellationToken ct = default);

        IQueryable<TEntity> ObterTodos();

        Task<TEntity> ObterPorIdAsync(object id , bool? rastreado = false, CancellationToken ct = default);

        Task<bool> ExisteAsync(
            Expression<Func<TEntity, bool>> filtro,
            CancellationToken ct = default);
        Task<IReadOnlyList<TEntity>> ObterPaginadoAsync(
            Expression<Func<TEntity, bool>> filtro,
            int pagina,
            int ItemPorPagina,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? ordenarPor = null,
            CancellationToken ct = default);
        Task<PaginatedResult<TEntity>> ObterPaginadoComFiltroAsync<TEntity>(
            Expression<Func<TEntity, bool>>? filtro = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? ordenarPor = null,
            Func<IQueryable<TEntity>, IQueryable<TEntity>>? incluir = null,
            int? pagina = null,  // Se null, retorna todos (sem paginação)
            int? itensPorPagina = null,
            bool asNoTracking = true,
            bool asSplitQuery = false,
            CancellationToken ct = default)
             where TEntity : class;
          

        Task<IReadOnlyList<TEntity>> ObterComFiltroAsync<TEntity>(
            Expression<Func<TEntity, bool>>? filtro = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? ordenarPor = null,
            Func<IQueryable<TEntity>, IQueryable<TEntity>>? incluir = null,
            bool asNoTracking = true,
            bool asSplitQuery = false,
            CancellationToken ct = default)
            where TEntity : class;

        Task<IReadOnlyList<TResult>> ObterComFiltroEProjecaoAsync<TEntity, TResult>(
        Expression<Func<TEntity, TResult>> projecao,
        Expression<Func<TEntity, bool>>? filtro = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? ordenarPor = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? incluir = null,
        bool asNoTracking = true,
        bool asSplitQuery = false,
        CancellationToken ct = default)
        where TEntity : class
        where TResult : class;

        Task<int> ContarAsync(Expression<Func<TEntity, bool>>? filtro = null,CancellationToken ct = default);

        Task<TEntity> InserirSalvarAsync(TEntity entidade, CancellationToken ct = default);
        Task<List<TEntity>> InserirSalvarListasAsync(List<TEntity> entidades, CancellationToken ct = default);

        Task InserirAsync(TEntity entidade, CancellationToken ct = default);

        Task<bool> AtualizarSalvarAsync(TEntity entidade, CancellationToken ct = default);

        void Atualizar(TEntity entidade);

        Task ExcluirSalvarAsync(TEntity entidade, CancellationToken ct = default);

        Task Excluir(TEntity entidade, CancellationToken ct = default);

        Task<int> SalvarAsync(CancellationToken ct = default);

        /// <summary>
        /// Descarta tudo o que o contexto está rastreando, sem gravar.
        ///
        /// Existe para <b>um</b> caso: gravar alguma coisa depois de um <c>SaveChanges</c> que
        /// falhou. O que falhou continua pendente no contexto — junto com todo o resto do grafo
        /// alterado — e a gravação seguinte tentaria mandá-lo de novo, batendo no mesmo erro. Hoje
        /// quem precisa disso é o registro da tentativa de sobreposição recusada pelo banco
        /// (seção 12), que só pode ser gravado depois de a abertura do contrato ter falhado.
        ///
        /// Só faça isso quando a operação em curso já está perdida de qualquer forma: limpar no
        /// meio de um fluxo que ainda pretende gravar descarta alteração em silêncio.
        /// </summary>
        void LimparRastreamento();

    }
}
