using System.Linq.Expressions;

namespace Locadora_Auto.Domain
{
    public interface IRepositorioGlobal<TEntity>
    {
        Task<IReadOnlyList<TEntity>> ObterAsync(
            Expression<Func<TEntity, bool>>? filtro = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? ordenarPor = null,
            Func<IQueryable<TEntity>, IQueryable<TEntity>>? incluir = null,
            CancellationToken ct = default);

        Task<TEntity?> ObterPrimeiroAsync(
            Expression<Func<TEntity, bool>> filtro,
            Func<IQueryable<TEntity>, IQueryable<TEntity>>? incluir = null,
            CancellationToken ct = default);

        IQueryable<TEntity> ObterTodos();

        Task<TEntity> ObterPorIdNoTracker(object id);

        Task<TEntity> ObterPorId(object id);

        Task<bool> ExisteAsync(
            Expression<Func<TEntity, bool>> filtro,
            CancellationToken ct = default);

        Task<IReadOnlyList<TEntity>> ObterPaginadoAsync(
            Expression<Func<TEntity, bool>> filtro,
            int skip,
            int take,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? ordenarPor = null,
            CancellationToken ct = default);

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

        Task<TEntity> InserirAsync(TEntity entidade, CancellationToken ct = default);

        Task Inserir(TEntity entidade, CancellationToken ct = default);

        Task<bool> AtualizarAsync(TEntity entidade, CancellationToken ct = default);

        void Atualizar(TEntity entidade);

        Task ExcluirAsync(object id, CancellationToken ct = default);

        Task Excluir(object id, CancellationToken ct = default);

        Task<int> SalvarAsync(CancellationToken ct = default);

    }
}
