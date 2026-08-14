using {{RootNamespace}}.Domain;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;

namespace {{RootNamespace}}.Infra.Data
{
    /// <summary>
    /// Repositório genérico base:
    /// - leitura sem tracking por padrão (a maioria das consultas só alimenta DTO);
    /// - filtro / ordenação / includes passados como parâmetro, em vez de um método por consulta;
    /// - CancellationToken em tudo;
    /// - sem controle de transação (isso é papel do IUnitOfWork).
    ///
    /// O repositório concreto normalmente só repassa o contexto; escreva LINQ nele apenas
    /// quando a consulta não couber nos parâmetros daqui.
    /// </summary>
    public abstract class RepositorioGlobal<TEntity> : IRepositorioGlobal<TEntity> where TEntity : class
    {
        protected readonly DbContext Context;
        protected readonly DbSet<TEntity> DbSet;

        protected RepositorioGlobal(DbContext context)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            DbSet = Context.Set<TEntity>();
        }

        // ======================= CONSULTAS =======================

        public virtual async Task<IReadOnlyList<TEntity>> ObterAsync(
            Expression<Func<TEntity, bool>>? filtro = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? ordenarPor = null,
            Func<IQueryable<TEntity>, IQueryable<TEntity>>? incluir = null,
            bool rastreado = false,
            CancellationToken ct = default)
        {
            IQueryable<TEntity> query = rastreado ? DbSet : DbSet.AsNoTracking();

            if (incluir != null) query = incluir(query);
            if (filtro != null) query = query.Where(filtro);
            if (ordenarPor != null) query = ordenarPor(query);

            return await query.ToListAsync(ct);
        }

        public virtual IQueryable<TEntity> ObterTodos() => DbSet.AsNoTracking();

        public virtual async Task<TEntity?> ObterPorIdAsync(
            object id,
            bool rastreado = false,
            CancellationToken ct = default)
        {
            var entidade = await DbSet.FindAsync(new[] { id }, ct);

            // FindAsync sempre rastreia; solta a entidade quando o chamador só vai ler
            if (entidade != null && !rastreado)
                Context.Entry(entidade).State = EntityState.Detached;

            return entidade;
        }

        public virtual async Task<TEntity?> ObterPrimeiroAsync(
            Expression<Func<TEntity, bool>> filtro,
            Func<IQueryable<TEntity>, IQueryable<TEntity>>? incluir = null,
            bool rastreado = false,
            CancellationToken ct = default)
        {
            IQueryable<TEntity> query = rastreado ? DbSet : DbSet.AsNoTracking();

            if (incluir != null) query = incluir(query);

            return await query.FirstOrDefaultAsync(filtro, ct);
        }

        public virtual async Task<bool> ExisteAsync(
            Expression<Func<TEntity, bool>> filtro,
            CancellationToken ct = default)
            => await DbSet.AsNoTracking().AnyAsync(filtro, ct);

        public virtual async Task<int> ContarAsync(
            Expression<Func<TEntity, bool>>? filtro = null,
            CancellationToken ct = default)
            => filtro == null
                ? await DbSet.AsNoTracking().CountAsync(ct)
                : await DbSet.AsNoTracking().CountAsync(filtro, ct);

        public virtual async Task<IReadOnlyList<TEntity>> ObterPaginadoAsync(
            Expression<Func<TEntity, bool>> filtro,
            int pagina,
            int itensPorPagina,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? ordenarPor = null,
            CancellationToken ct = default)
        {
            IQueryable<TEntity> query = DbSet.AsNoTracking().Where(filtro);

            if (ordenarPor != null) query = ordenarPor(query);

            var pular = (pagina - 1) * itensPorPagina;
            return await query.Skip(pular).Take(itensPorPagina).ToListAsync(ct);
        }

        public async Task<PaginatedResult<TConsulta>> ObterPaginadoComFiltroAsync<TConsulta>(
            Expression<Func<TConsulta, bool>>? filtro = null,
            Func<IQueryable<TConsulta>, IOrderedQueryable<TConsulta>>? ordenarPor = null,
            Func<IQueryable<TConsulta>, IQueryable<TConsulta>>? incluir = null,
            int? pagina = null,
            int? itensPorPagina = null,
            bool asNoTracking = true,
            bool asSplitQuery = false,
            CancellationToken ct = default)
            where TConsulta : class
        {
            IQueryable<TConsulta> query = Context.Set<TConsulta>();

            if (asNoTracking) query = query.AsNoTracking();
            if (asSplitQuery) query = query.AsSplitQuery();
            if (incluir != null) query = incluir(query);
            if (filtro != null) query = query.Where(filtro);

            // conta antes de paginar: Total é o tamanho do resultado filtrado, não da página
            var total = await query.CountAsync(ct);

            if (ordenarPor != null)
            {
                query = ordenarPor(query);
            }
            else if (pagina.HasValue)
            {
                // sem ORDER BY, Skip/Take devolve páginas com itens repetidos ou faltando
                var nomeChave = ObterNomeChavePrimaria<TConsulta>();
                query = query.OrderBy(e => EF.Property<object>(e, nomeChave));
            }

            IReadOnlyList<TConsulta> itens;

            if (pagina.HasValue && itensPorPagina.HasValue)
            {
                var pular = (pagina.Value - 1) * itensPorPagina.Value;
                itens = await query.Skip(pular).Take(itensPorPagina.Value).ToListAsync(ct);
            }
            else
            {
                itens = await query.ToListAsync(ct);
            }

            var totalPaginas = 1;
            if (itensPorPagina.HasValue && itensPorPagina.Value > 0)
                totalPaginas = (int)Math.Ceiling(total / (double)itensPorPagina.Value);

            return new PaginatedResult<TConsulta>
            {
                Items = itens,
                Total = total,
                Pagina = pagina ?? 1,
                TotalPaginas = totalPaginas,
                ItensPorPagina = itensPorPagina ?? itens.Count
            };
        }

        public async Task<IReadOnlyList<TConsulta>> ObterComFiltroAsync<TConsulta>(
            Expression<Func<TConsulta, bool>>? filtro = null,
            Func<IQueryable<TConsulta>, IOrderedQueryable<TConsulta>>? ordenarPor = null,
            Func<IQueryable<TConsulta>, IQueryable<TConsulta>>? incluir = null,
            bool asNoTracking = true,
            bool asSplitQuery = false,
            CancellationToken ct = default)
            where TConsulta : class
        {
            IQueryable<TConsulta> query = Context.Set<TConsulta>();

            if (asNoTracking) query = query.AsNoTracking();
            if (asSplitQuery) query = query.AsSplitQuery();
            if (incluir != null) query = incluir(query);
            if (filtro != null) query = query.Where(filtro);
            if (ordenarPor != null) query = ordenarPor(query);

            return await query.ToListAsync(ct);
        }

        public async Task<IReadOnlyList<TResult>> ObterComFiltroEProjecaoAsync<TConsulta, TResult>(
            Expression<Func<TConsulta, TResult>> projecao,
            Expression<Func<TConsulta, bool>>? filtro = null,
            Func<IQueryable<TConsulta>, IOrderedQueryable<TConsulta>>? ordenarPor = null,
            Func<IQueryable<TConsulta>, IQueryable<TConsulta>>? incluir = null,
            bool asNoTracking = true,
            bool asSplitQuery = false,
            CancellationToken ct = default)
            where TConsulta : class
        {
            IQueryable<TConsulta> query = Context.Set<TConsulta>();

            if (asNoTracking) query = query.AsNoTracking();
            if (asSplitQuery) query = query.AsSplitQuery();
            if (incluir != null) query = incluir(query);
            if (filtro != null) query = query.Where(filtro);
            if (ordenarPor != null) query = ordenarPor(query);

            // projeta antes de materializar: o SELECT vai só com as colunas usadas
            return await query.Select(projecao).ToListAsync(ct);
        }

        /// <summary>
        /// Descobre o nome da chave primária para a ordenação padrão da paginação:
        /// atributo [Key], depois os nomes convencionais, e por último a primeira propriedade.
        /// </summary>
        private static string ObterNomeChavePrimaria<TConsulta>() where TConsulta : class
        {
            var propriedadeChave = typeof(TConsulta).GetProperties()
                .FirstOrDefault(p => p.GetCustomAttribute<KeyAttribute>() != null);

            if (propriedadeChave != null)
                return propriedadeChave.Name;

            var candidatos = new[] { "Id", $"{typeof(TConsulta).Name}Id", "Id" + typeof(TConsulta).Name };

            foreach (var nome in candidatos)
            {
                if (typeof(TConsulta).GetProperty(nome) != null)
                    return nome;
            }

            return typeof(TConsulta).GetProperties().FirstOrDefault()?.Name
                   ?? throw new InvalidOperationException("Não foi possível determinar a chave primária da entidade");
        }

        // ======================= ESCRITAS =======================

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

        /// <summary>
        /// Atualiza tanto entidade rastreada quanto desanexada. No segundo caso todas as colunas
        /// são sobrescritas — carregue com <c>rastreado: true</c> antes de alterar para não apagar
        /// campo que você não leu.
        /// </summary>
        public virtual async Task<bool> AtualizarSalvarAsync(TEntity entidade, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(entidade);

            var propriedadesChave = Context.Model
                .FindEntityType(typeof(TEntity))?
                .FindPrimaryKey()?
                .Properties;

            if (propriedadesChave == null)
                throw new InvalidOperationException($"Entidade {typeof(TEntity).Name} não tem chave primária mapeada.");

            var valoresChave = propriedadesChave
                .Select(p => p.PropertyInfo?.GetValue(entidade))
                .ToArray();

            var existente = await DbSet.FindAsync(valoresChave, ct);

            if (existente != null)
                Context.Entry(existente).CurrentValues.SetValues(entidade);
            else
            {
                DbSet.Attach(entidade);
                Context.Entry(entidade).State = EntityState.Modified;
            }

            return await SalvarAsync(ct) > 0;
        }

        public virtual void Atualizar(TEntity entidade)
        {
            ArgumentNullException.ThrowIfNull(entidade);
            DbSet.Update(entidade);
        }

        public virtual async Task ExcluirSalvarAsync(TEntity entidade, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(entidade);

            DbSet.Remove(entidade);
            await SalvarAsync(ct);
        }

        public virtual Task Excluir(TEntity entidade, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(entidade);

            DbSet.Remove(entidade);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Sempre a via assíncrona: é o SaveChangesAsync sobrescrito no contexto que aplica
        /// auditoria e histórico temporal. O SaveChanges() síncrono ignora tudo isso.
        /// </summary>
        public virtual async Task<int> SalvarAsync(CancellationToken ct = default)
            => await Context.SaveChangesAsync(ct);
    }
}
