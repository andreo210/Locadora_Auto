using {{RootNamespace}}.Domain.IRepositorio;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace {{RootNamespace}}.Infra.Data
{
    /// <summary>
    /// Transação explícita sobre o mesmo DbContext scoped que os repositórios usam.
    /// Só é necessário quando duas ou mais escritas precisam cair ou passar juntas.
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        private readonly DbContext _context;
        private IDbContextTransaction? _transaction;
        private bool _disposed;

        public UnitOfWork(DbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public bool HasActiveTransaction => _transaction != null;

        public async Task BeginTransactionAsync(CancellationToken ct = default)
        {
            if (_transaction != null)
                throw new InvalidOperationException("Transação já iniciada.");

            _transaction = await _context.Database.BeginTransactionAsync(ct);
        }

        public async Task CommitAsync(CancellationToken ct = default)
        {
            if (_transaction == null)
                throw new InvalidOperationException("Transação não iniciada.");

            try
            {
                await _context.SaveChangesAsync(ct);
                await _transaction.CommitAsync(ct);
            }
            finally
            {
                await DescartarTransacaoAsync();
            }
        }

        public async Task RollbackAsync(CancellationToken ct = default)
        {
            if (_transaction == null) return;

            try
            {
                await _transaction.RollbackAsync(ct);
            }
            finally
            {
                await DescartarTransacaoAsync();
            }
        }

        /// <summary>
        /// Forma preferida de usar: não deixa transação aberta em caminho de erro.
        /// Dentro do bloco use InserirAsync/Atualizar (sem salvar) — salvar no meio
        /// derrota o propósito da transação.
        /// </summary>
        public async Task<T> ExecuteTransactionAsync<T>(Func<Task<T>> action, CancellationToken ct = default)
        {
            await BeginTransactionAsync(ct);

            try
            {
                var resultado = await action();
                await CommitAsync(ct);
                return resultado;
            }
            catch
            {
                await RollbackAsync(ct);
                throw;
            }
        }

        private async Task DescartarTransacaoAsync()
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing && _transaction != null)
            {
                try { _transaction.Rollback(); }
                catch { /* ignorado no Dispose */ }

                _transaction.Dispose();
            }

            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
