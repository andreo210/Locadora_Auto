using Locadora_Auto.Domain;
using Locadora_Auto.Infra.Data.CurrentUsers;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Locadora_Auto.Infra.Data
{
    /// <summary>
    /// Interface para auditoria automática.
    /// </summary>
    public interface IAuditable
    {
        DateTime DataCriacao { get; set; }
        string? IdUsuarioCriacao { get; set; }
        DateTime? DataModificacao { get; set; }
        string? IdUsuarioModificacao { get; set; }
    }

    /// <summary>
    /// Repositório genérico base refatorado:
    /// - EF Core moderno
    /// - Sem controle de transação
    /// - Auditoria automática
    /// - Includes fortemente tipados
    /// - Leitura sem tracking por padrão
    /// - CancellationToken
    /// </summary>
    public abstract class RepositorioGlobal<TEntity> : IRepositorioGlobal<TEntity> where TEntity : class
    {
        protected readonly DbContext Context;
        protected readonly DbSet<TEntity> DbSet;
        private readonly ICurrentUser _currentUser;
        private LocadoraDbContext dbContext;

        protected RepositorioGlobal(DbContext context, ICurrentUser currentUser)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            DbSet = Context.Set<TEntity>();
            _currentUser = currentUser;
        }

        protected RepositorioGlobal(LocadoraDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public virtual async Task<IReadOnlyList<TEntity>> ObterAsync(
            Expression<Func<TEntity, bool>>? filtro = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? ordenarPor = null,
            Func<IQueryable<TEntity>, IQueryable<TEntity>>? incluir = null,
            CancellationToken ct = default)
        {
            IQueryable<TEntity> query = DbSet.AsNoTracking();

            if (incluir != null)
                query = incluir(query);

            if (filtro != null)
                query = query.Where(filtro);

            if (ordenarPor != null)
                query = ordenarPor(query);

            return await query.ToListAsync(ct);
        }

        public virtual async Task<TEntity?> ObterPrimeiroAsync(
            Expression<Func<TEntity, bool>> filtro,
            Func<IQueryable<TEntity>, IQueryable<TEntity>>? incluir = null,
            CancellationToken ct = default)
        {
            IQueryable<TEntity> query = DbSet.AsNoTracking();

            if (incluir != null)
                query = incluir(query);

            return await query.FirstOrDefaultAsync(filtro, ct);
        }

        public virtual async Task<bool> ExisteAsync(
            Expression<Func<TEntity, bool>> filtro,
            CancellationToken ct = default)
        {
            return await DbSet.AsNoTracking().AnyAsync(filtro, ct);
        }

        public virtual async Task<int> ContarAsync(
            Expression<Func<TEntity, bool>>? filtro = null,
            CancellationToken ct = default)
        {
            return filtro == null
                ? await DbSet.AsNoTracking().CountAsync(ct)
                : await DbSet.AsNoTracking().CountAsync(filtro, ct);
        }

        public virtual async Task<IReadOnlyList<TEntity>> ObterPaginadoAsync(
            Expression<Func<TEntity, bool>> filtro,
            int skip,
            int take,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? ordenarPor = null,
            CancellationToken ct = default)
        {
            IQueryable<TEntity> query = DbSet.AsNoTracking().Where(filtro);

            if (ordenarPor != null)
                query = ordenarPor(query);

            return await query.Skip(skip).Take(take).ToListAsync(ct);
        }

        public async Task<List<TEntity>> ObterComFiltroAsync<TEntity>(
            Expression<Func<TEntity, bool>>? filtro = null,
            Func<IQueryable<TEntity>, IQueryable<TEntity>>? incluir = null,
            bool asNoTracking = true,
            bool asSplitQuery = false,
            CancellationToken ct = default)
            where TEntity : class
        {
            IQueryable<TEntity> query = Context.Set<TEntity>();

            if (asNoTracking)
                query = query.AsNoTracking();

            if (asSplitQuery)
                query = query.AsSplitQuery();

            if (incluir != null)
                query = incluir(query);

            if (filtro != null)
                query = query.Where(filtro);

            return await query.ToListAsync(ct);
        }


        public virtual async Task<TEntity> InserirAsync(TEntity entidade, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(entidade);
            await DbSet.AddAsync(entidade, ct);
            await SalvarAsync(ct);
            return entidade;
        }
        public virtual Task Inserir(TEntity entidade, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(entidade);
            return DbSet.AddAsync(entidade, ct).AsTask();
        }

        public virtual async Task AtualizarAsync(TEntity entidade, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(entidade);
            DbSet.Attach(entidade);
            await SalvarAsync(ct);
        }
        public virtual void Atualizar(TEntity entidade)
        {
            ArgumentNullException.ThrowIfNull(entidade);
            DbSet.Attach(entidade);
        }

        public virtual async Task ExcluirAsync(object id, CancellationToken ct = default)
        {
            var entidade = await DbSet.FindAsync(new[] { id }, ct);

            if (entidade == null)
                throw new KeyNotFoundException("Entidade não encontrada.");
            DbSet.Remove(entidade);
            await SalvarAsync(ct);
        }

        public virtual async Task Excluir(object id, CancellationToken ct = default)
        {
            var entidade = await DbSet.FindAsync(new[] { id }, ct);

            if (entidade == null)
                throw new KeyNotFoundException("Entidade não encontrada.");
            DbSet.Remove(entidade);
        }


        public virtual async Task<int> SalvarAsync(CancellationToken ct = default)
        {
            AplicarAuditoria(_currentUser.UserId);
            return await Context.SaveChangesAsync(ct);
        }

        protected virtual void AplicarAuditoria(string? usuario)
        {
            var agora = DateTime.UtcNow;

            foreach (var entry in Context.ChangeTracker.Entries<IAuditable>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.DataCriacao = agora;
                    entry.Entity.IdUsuarioCriacao = usuario;
                }

                if (entry.State == EntityState.Modified)
                {
                    entry.Entity.DataModificacao = agora;
                    entry.Entity.IdUsuarioModificacao = usuario;
                }
            }
        }
    }
}
