namespace Locadora_Auto.Domain.IRepositorio
{
    public interface IUnitOfWork : IDisposable
    {
        /// <summary>
        /// Verifica se há uma transação ativa no momento
        /// </summary>
        bool HasActiveTransaction { get; }

        /// <summary>
        /// Inicia uma nova transação
        /// </summary>
        Task BeginTransactionAsync(CancellationToken ct = default);

        /// <summary>
        /// Confirma a transação atual
        /// </summary>
        Task CommitAsync(CancellationToken ct = default);

        /// <summary>
        /// Cancela a transação atual
        /// </summary>
        Task RollbackAsync(CancellationToken ct = default);

        /// <summary>
        /// Executa operações em uma transação com tratamento automático
        /// </summary>
        Task<T> ExecuteTransactionAsync<T>(Func<Task<T>> action, CancellationToken ct = default);
    }
}