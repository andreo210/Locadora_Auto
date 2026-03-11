using Locadora_Auto.Domain;
using Locadora_Auto.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;

namespace Locadora_Auto.Infra.Data
{
   

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
        //private readonly ICurrentUser _currentUser;
        //private LocadoraDbContext dbContext;

        protected RepositorioGlobal(DbContext context/*, ICurrentUser currentUser*/)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            DbSet = Context.Set<TEntity>();
           // _currentUser = currentUser;
        }

       

        public virtual async Task<IReadOnlyList<TEntity>> ObterAsync(
            Expression<Func<TEntity, bool>>? filtro = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? ordenarPor = null,
            Func<IQueryable<TEntity>, IQueryable<TEntity>>? incluir = null,
            bool rastreado = false,
            CancellationToken ct = default)
        {
            IQueryable<TEntity> query = rastreado
               ? DbSet
               : DbSet.AsNoTracking();

            if (incluir != null)
                query = incluir(query);

            if (filtro != null)
                query = query.Where(filtro);

            if (ordenarPor != null)
                query = ordenarPor(query);

            return await query.ToListAsync(ct);
        }

        public virtual IQueryable<TEntity> ObterTodos()
        {
            var entity =  DbSet.AsNoTracking();
            return entity;
        }

        //buscar por id Trackeado
        public virtual async Task<TEntity> ObterPorIdAsync(object id, bool? rastreado = false,CancellationToken ct = default)
        {
            var entity = await DbSet.FindAsync(id);
            if (rastreado.Value)
            {
                Context.Entry(entity).State = EntityState.Detached;
            }
            return entity;
        }

        public virtual async Task<TEntity?> ObterPrimeiroAsync(
             Expression<Func<TEntity, bool>> filtro,
             Func<IQueryable<TEntity>, IQueryable<TEntity>>? incluir = null,
             bool rastreado = false,
             CancellationToken ct = default)
        {
            IQueryable<TEntity> query = rastreado
                ? DbSet
                : DbSet.AsNoTracking();

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

        public async Task<PaginatedResult<TEntity>> ObterPaginadoComFiltroAsync<TEntity>(
            Expression<Func<TEntity, bool>>? filtro = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? ordenarPor = null,
            Func<IQueryable<TEntity>, IQueryable<TEntity>>? incluir = null,
            int? pagina = null,  // Se null, retorna todos (sem paginação)
            int? itensPorPagina = null,
            bool asNoTracking = true,
            bool asSplitQuery = false,
            CancellationToken ct = default)
            where TEntity : class
        {
            // 1. Query base
            IQueryable<TEntity> query = Context.Set<TEntity>();

            // 2. Configurações
            if (asNoTracking)
                query = query.AsNoTracking();

            if (asSplitQuery)
                query = query.AsSplitQuery();

            if (incluir != null)
                query = incluir(query);

            if (filtro != null)
                query = query.Where(filtro);

            // 3. Contar total (sempre útil)
            var total = await query.CountAsync(ct);

            // 4. Aplicar ordenação
            if (ordenarPor != null)
            {
                query = ordenarPor(query);
            }
            else if (pagina.HasValue) // Se tem paginação, precisa de ordenação
            {
                // Tenta encontrar a chave primária da entidade
                var keyName = ObterNomeChavePrimaria<TEntity>();
                query = query.OrderBy(e => EF.Property<object>(e, keyName));
            }

            // 5. Aplicar paginação se solicitado
            IReadOnlyList<TEntity> items;

            if (pagina.HasValue && itensPorPagina.HasValue)
            {
                var skip = (pagina.Value - 1) * itensPorPagina.Value;
                items = await query
                    .Skip(skip)
                    .Take(itensPorPagina.Value)
                    .ToListAsync(ct);
            }
            else
            {
                items = await query.ToListAsync(ct);
            }

            // 6. Calcular total de páginas (se aplicável)
            int totalPaginas = 1;
            if (itensPorPagina.HasValue && itensPorPagina.Value > 0)
            {
                totalPaginas = (int)Math.Ceiling(total / (double)itensPorPagina.Value);
            }

            return new PaginatedResult<TEntity>
            {
                Items = items,
                Total = total,
                Pagina = pagina ?? 1,
                TotalPaginas = totalPaginas,
                ItensPorPagina = itensPorPagina ?? items.Count
            };
        }

       private string ObterNomeChavePrimaria<TEntity>() where TEntity : class
        {
            // Tenta via Data Annotations [Key]
            var keyProperty = typeof(TEntity).GetProperties()
                .FirstOrDefault(p => p.GetCustomAttribute<KeyAttribute>() != null);

            if (keyProperty != null)
                return keyProperty.Name;

            // Tenta nomes comuns de chave primária
            var possibleKeys = new[] { "Id", $"{typeof(TEntity).Name}Id", "Id" + typeof(TEntity).Name };

            foreach (var keyName in possibleKeys)
            {
                if (typeof(TEntity).GetProperty(keyName) != null)
                    return keyName;
            }

            // Fallback para a primeira propriedade (não ideal)
            return typeof(TEntity).GetProperties().FirstOrDefault()?.Name
                   ?? throw new InvalidOperationException("Não foi possível determinar a chave primária da entidade");
        }

        public async Task<IReadOnlyList<TEntity>> ObterComFiltroAsync<TEntity>(
            Expression<Func<TEntity, bool>>? filtro = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? ordenarPor = null,
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


        public async Task<IReadOnlyList<TResult>> ObterComFiltroEProjecaoAsync<TEntity, TResult>(
        Expression<Func<TEntity, TResult>> projecao,
        Expression<Func<TEntity, bool>>? filtro = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? ordenarPor = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? incluir = null,
        bool asNoTracking = true,
        bool asSplitQuery = false,
        CancellationToken ct = default)
        where TEntity : class
        where TResult : class
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

                if (ordenarPor != null)
                    query = ordenarPor(query);

                // Aplica projeção ANTES de materializar
                return await query.Select(projecao).ToListAsync(ct);
        }

        public virtual async Task<TEntity> InserirSalvarAsync(TEntity entidade, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(entidade);
            await DbSet.AddAsync(entidade, ct);
            await SalvarAsync(ct);
            return entidade;
        }
        public virtual async Task<List<TEntity>> InserirSalvarListasAsync(List<TEntity> entidades, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(entidades);
            await DbSet.AddRangeAsync(entidades, ct);
            await SalvarAsync(ct);
            return entidades;
        }

        public virtual Task InserirAsync(TEntity entidade, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(entidade);
            return DbSet.AddAsync(entidade, ct).AsTask();
        }

        public virtual async Task<bool> AtualizarSalvarAsync(TEntity entidade, CancellationToken ct = default)
        {
            if (entidade == null)
                throw new ArgumentNullException(nameof(entidade));

            // Pega a chave primária da entidade
            var keyProperties = Context.Model?
                .FindEntityType(typeof(TEntity))
                .FindPrimaryKey()
                .Properties;

            object[] keyValues = keyProperties.Select(p => p.PropertyInfo.GetValue(entidade)).ToArray();

            // Tenta localizar a entidade rastreada no DbSet
            var entidadeExistente = await DbSet.FindAsync(keyValues, ct);

            if (entidadeExistente != null)
            {
                // Entidade rastreada: atualiza somente os valores que vieram
                Context.Entry(entidadeExistente).CurrentValues.SetValues(entidade);
            }
            else
            {
                // Entidade não rastreada: marca para update (sobrescreve todas as colunas)
                DbSet.Attach(entidade);
                Context.Entry(entidade).State = EntityState.Modified;
            }

            // Salva alterações
            var result = await Context.SaveChangesAsync(ct);

            return result > 0; // retorna true se houve alterações no banco
        }
        //public virtual async Task<bool> AtualizarAsync(TEntity entidade, CancellationToken ct = default)
        //{
        //    if (entidade == null)
        //        throw new ArgumentNullException(nameof(entidade));

        //    // Anexa se não estiver rastreada
        //    var entry = Context.Entry(entidade);

        //    if (entry.State == EntityState.Detached)
        //    {
        //        DbSet.Attach(entidade);
        //        entry = Context.Entry(entidade);
        //    }

        //    // Marca como Modified (EF gera UPDATE)
        //    entry.State = EntityState.Modified;

        //    var dd = Context.Entry(entidade);

        //    Console.WriteLine($"State: {dd.State}");

        //    foreach (var prop in dd.Properties)
        //    {
        //        if (prop.Metadata.IsPrimaryKey())
        //        {
        //            Console.WriteLine($"PK {prop.Metadata.Name} = {prop.CurrentValue}");
        //        }
        //    }

        //    var affected = await Context.SaveChangesAsync(ct);
        //    return affected > 0;
        //}


        public virtual void Atualizar(TEntity entidade)
        {
            ArgumentNullException.ThrowIfNull(entidade);
            DbSet.Update(entidade);
        }

        public virtual async Task ExcluirSalvarAsync(TEntity entidade, CancellationToken ct = default)
        {
            //var entidade = await DbSet.FindAsync(new[] { id }, ct);

            if (entidade == null)
                throw new KeyNotFoundException("Entidade não encontrada.");
            DbSet.Remove(entidade);
            await SalvarAsync(ct);
        }

        public virtual async Task Excluir(TEntity entidade, CancellationToken ct = default)
        {
            //var entidade = await DbSet.FindAsync(new[] { id }, ct);

            if (entidade == null)
                throw new KeyNotFoundException("Entidade não encontrada.");
            DbSet.Remove(entidade);
        }


        public virtual async Task<int> SalvarAsync(CancellationToken ct = default)
        {
            //AplicarAuditoria(_currentUser.UserId);
            return await Context.SaveChangesAsync(ct);
        }        
    }
}
