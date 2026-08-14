using {{RootNamespace}}.Domain;
using System.Linq.Expressions;

namespace {{RootNamespace}}.Tests.Fakes
{
    /// <summary>
    /// Implementação em memória de <see cref="IRepositorioGlobal{TEntity}"/> para testar serviços
    /// sem banco. Escrita uma vez e reaproveitada por todos os repositórios, porque as interfaces
    /// concretas (<c>IClienteRepository</c> e companhia) não declaram nada além do genérico.
    ///
    /// O filtro roda de verdade, em LINQ to Objects — é isso que dispensa biblioteca de mock:
    /// o teste afirma "o serviço encontrou o registro certo", não "o serviço chamou tal método".
    ///
    /// Duas diferenças conscientes em relação ao repositório real:
    ///
    /// 1. <c>incluir</c> é ignorado. Include/ThenInclude só existem sobre um provider do EF; em
    ///    memória a navegação já vem montada pelo objeto que o teste criou. Se o teste precisa de
    ///    <c>pedido.Cliente</c> preenchido, ele preenche ao construir a entidade.
    ///
    /// 2. Não existe change tracking: as entidades são as mesmas instâncias que o teste criou, então
    ///    alterar a entidade já "persiste". Por isso o que se verifica não é o conteúdo do armazém,
    ///    e sim <see cref="Salvamentos"/> — se o serviço chamou ou não a gravação.
    ///
    /// O que ele NÃO cobre: tradução da Expression para SQL, tracking, cascade de agregado e
    /// a auditoria do SaveChangesAsync. Isso é escopo de teste de integração com banco.
    /// </summary>
    public class RepositorioFake<TEntity> : IRepositorioGlobal<TEntity> where TEntity : class
    {
        public RepositorioFake(ArmazemFake? armazem = null)
        {
            Armazem = armazem ?? new ArmazemFake();
        }

        /// <summary>Compartilhe o mesmo armazém entre os fakes de um serviço com várias dependências.</summary>
        public ArmazemFake Armazem { get; }

        /// <summary>Quantas vezes o serviço mandou gravar. É a asserção de "chegou a salvar?".</summary>
        public int Salvamentos { get; private set; }

        protected List<TEntity> Tabela => Armazem.Tabela<TEntity>();

        // ======================= CONSULTAS =======================

        public Task<IReadOnlyList<TEntity>> ObterAsync(
            Expression<Func<TEntity, bool>>? filtro = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? ordenarPor = null,
            Func<IQueryable<TEntity>, IQueryable<TEntity>>? incluir = null,
            bool rastreado = false,
            CancellationToken ct = default)
        {
            var query = Tabela.AsQueryable();

            if (filtro != null) query = query.Where(filtro);
            if (ordenarPor != null) query = ordenarPor(query);

            return Task.FromResult<IReadOnlyList<TEntity>>(query.ToList());
        }

        public Task<TEntity?> ObterPrimeiroAsync(
            Expression<Func<TEntity, bool>> filtro,
            Func<IQueryable<TEntity>, IQueryable<TEntity>>? incluir = null,
            bool rastreado = false,
            CancellationToken ct = default)
            => Task.FromResult(Tabela.AsQueryable().FirstOrDefault(filtro));

        public IQueryable<TEntity> ObterTodos() => Tabela.AsQueryable();

        public Task<TEntity?> ObterPorIdAsync(object id, bool rastreado = false, CancellationToken ct = default)
            => Task.FromResult(Tabela.FirstOrDefault(e => Equals(ChavePrimaria.Ler(e), id)));

        public Task<bool> ExisteAsync(Expression<Func<TEntity, bool>> filtro, CancellationToken ct = default)
            => Task.FromResult(Tabela.AsQueryable().Any(filtro));

        public Task<int> ContarAsync(Expression<Func<TEntity, bool>>? filtro = null, CancellationToken ct = default)
            => Task.FromResult(filtro == null
                ? Tabela.Count
                : Tabela.AsQueryable().Count(filtro));

        public Task<IReadOnlyList<TEntity>> ObterPaginadoAsync(
            Expression<Func<TEntity, bool>> filtro,
            int pagina,
            int itensPorPagina,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? ordenarPor = null,
            CancellationToken ct = default)
        {
            var query = Tabela.AsQueryable().Where(filtro);

            if (ordenarPor != null) query = ordenarPor(query);

            var itens = query.Skip((pagina - 1) * itensPorPagina).Take(itensPorPagina).ToList();
            return Task.FromResult<IReadOnlyList<TEntity>>(itens);
        }

        public Task<PaginatedResult<TConsulta>> ObterPaginadoComFiltroAsync<TConsulta>(
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
            var query = Armazem.Tabela<TConsulta>().AsQueryable();

            if (filtro != null) query = query.Where(filtro);

            // o total é contado antes da paginação, como no repositório real
            var total = query.Count();

            if (ordenarPor != null) query = ordenarPor(query);

            var itens = pagina.HasValue && itensPorPagina.HasValue
                ? query.Skip((pagina.Value - 1) * itensPorPagina.Value).Take(itensPorPagina.Value).ToList()
                : query.ToList();

            var totalPaginas = itensPorPagina is > 0
                ? (int)Math.Ceiling(total / (double)itensPorPagina.Value)
                : 1;

            return Task.FromResult(new PaginatedResult<TConsulta>
            {
                Items = itens,
                Total = total,
                Pagina = pagina ?? 1,
                TotalPaginas = totalPaginas,
                ItensPorPagina = itensPorPagina ?? itens.Count
            });
        }

        public Task<IReadOnlyList<TConsulta>> ObterComFiltroAsync<TConsulta>(
            Expression<Func<TConsulta, bool>>? filtro = null,
            Func<IQueryable<TConsulta>, IOrderedQueryable<TConsulta>>? ordenarPor = null,
            Func<IQueryable<TConsulta>, IQueryable<TConsulta>>? incluir = null,
            bool asNoTracking = true,
            bool asSplitQuery = false,
            CancellationToken ct = default)
            where TConsulta : class
        {
            var query = Armazem.Tabela<TConsulta>().AsQueryable();

            if (filtro != null) query = query.Where(filtro);
            if (ordenarPor != null) query = ordenarPor(query);

            return Task.FromResult<IReadOnlyList<TConsulta>>(query.ToList());
        }

        public Task<IReadOnlyList<TResult>> ObterComFiltroEProjecaoAsync<TConsulta, TResult>(
            Expression<Func<TConsulta, TResult>> projecao,
            Expression<Func<TConsulta, bool>>? filtro = null,
            Func<IQueryable<TConsulta>, IOrderedQueryable<TConsulta>>? ordenarPor = null,
            Func<IQueryable<TConsulta>, IQueryable<TConsulta>>? incluir = null,
            bool asNoTracking = true,
            bool asSplitQuery = false,
            CancellationToken ct = default)
            where TConsulta : class
        {
            var query = Armazem.Tabela<TConsulta>().AsQueryable();

            if (filtro != null) query = query.Where(filtro);
            if (ordenarPor != null) query = ordenarPor(query);

            return Task.FromResult<IReadOnlyList<TResult>>(query.Select(projecao).ToList());
        }

        // ======================= ESCRITAS =======================

        public Task<TEntity> InserirSalvarAsync(TEntity entidade, CancellationToken ct = default)
        {
            Inserir(entidade);
            Salvamentos++;
            return Task.FromResult(entidade);
        }

        public Task<List<TEntity>> InserirSalvarListasAsync(List<TEntity> entidades, CancellationToken ct = default)
        {
            foreach (var entidade in entidades) Inserir(entidade);
            Salvamentos++;
            return Task.FromResult(entidades);
        }

        public Task InserirAsync(TEntity entidade, CancellationToken ct = default)
        {
            Inserir(entidade);
            return Task.CompletedTask;
        }

        public Task<bool> AtualizarSalvarAsync(TEntity entidade, CancellationToken ct = default)
        {
            if (!Tabela.Contains(entidade)) Tabela.Add(entidade);
            Salvamentos++;
            return Task.FromResult(true);
        }

        public void Atualizar(TEntity entidade)
        {
            if (!Tabela.Contains(entidade)) Tabela.Add(entidade);
        }

        public Task ExcluirSalvarAsync(TEntity entidade, CancellationToken ct = default)
        {
            Tabela.Remove(entidade);
            Salvamentos++;
            return Task.CompletedTask;
        }

        public Task Excluir(TEntity entidade, CancellationToken ct = default)
        {
            Tabela.Remove(entidade);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Virtual para o fake da raiz de agregado poder propagar os filhos criados pela raiz —
        /// veja o exemplo em <c>references/servicos.md</c>.
        /// </summary>
        public virtual Task<int> SalvarAsync(CancellationToken ct = default)
        {
            Salvamentos++;
            return Task.FromResult(1);
        }

        private void Inserir(TEntity entidade)
        {
            ChavePrimaria.AtribuirSeVazia(entidade, Armazem.ProximoId<TEntity>());
            Tabela.Add(entidade);
        }
    }
}
