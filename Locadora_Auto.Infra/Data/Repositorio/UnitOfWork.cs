using Locadora_Auto.Domain.IRepositorio;
using Microsoft.EntityFrameworkCore.Storage;

namespace Locadora_Auto.Infra.Data.Repositorio
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly LocadoraDbContext _context;
        private IDbContextTransaction? _transaction;
        private bool _disposed;

        public UnitOfWork(LocadoraDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Verifica se há uma transação ativa no momento
        /// </summary>
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
                await DisposeTransactionAsync();
            }
        }

        public async Task RollbackAsync(CancellationToken ct = default)
        {
            if (_transaction != null)
            {
                try
                {
                    await _transaction.RollbackAsync(ct);
                }
                finally
                {
                    await DisposeTransactionAsync();
                }
            }
        }

        public async Task<T> ExecuteTransactionAsync<T>(Func<Task<T>> action,CancellationToken ct = default)
        {
            await BeginTransactionAsync(ct);

            try
            {
                var result = await action();
                await CommitAsync(ct);
                return result;
            }
            catch
            {
                await RollbackAsync(ct);
                throw;
            }
        }

        private async Task DisposeTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_transaction != null)
                    {
                        try
                        {
                            _transaction.Rollback();
                        }
                        catch
                        {
                            // Ignorar em Dispose
                        }
                        _transaction.Dispose();
                    }
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}