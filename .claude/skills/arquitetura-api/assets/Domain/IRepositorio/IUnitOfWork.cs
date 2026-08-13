namespace {{RootNamespace}}.Domain.IRepositorio
{
    /// <summary>
    /// Transação explícita para quando duas ou mais escritas precisam cair ou passar juntas.
    /// Escrita única não precisa disso — <c>InserirSalvarAsync</c> já é transacional.
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        bool HasActiveTransaction { get; }

        Task BeginTransactionAsync(CancellationToken ct = default);

        Task CommitAsync(CancellationToken ct = default);

        Task RollbackAsync(CancellationToken ct = default);

        /// <summary>
        /// Forma preferida: abre, salva, commita e faz rollback automático em caso de exceção.
        /// Dentro do bloco use as versões que não salvam (<c>InserirAsync</c>, <c>Atualizar</c>).
        /// </summary>
        Task<T> ExecuteTransactionAsync<T>(Func<Task<T>> action, CancellationToken ct = default);
    }
}
