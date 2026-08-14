namespace {{RootNamespace}}.Domain
{
    /// <summary>
    /// Página de resultados com os metadados que a tela precisa para montar a paginação.
    /// O repositório devolve <c>PaginatedResult&lt;Entidade&gt;</c>; o serviço reprojeta para
    /// <c>PaginatedResult&lt;Dto&gt;</c> antes de entregar ao controller.
    /// </summary>
    public class PaginatedResult<TEntity>
    {
        public IReadOnlyList<TEntity> Items { get; set; } = new List<TEntity>();
        public int Total { get; set; }
        public int Pagina { get; set; }
        public int TotalPaginas { get; set; }
        public int ItensPorPagina { get; set; }

        public bool TemPaginaAnterior => Pagina > 1;
        public bool TemProximaPagina => Pagina < TotalPaginas;

        public int PrimeiroItem => (Pagina - 1) * ItensPorPagina + 1;
        public int UltimoItem => Math.Min(Pagina * ItensPorPagina, Total);
    }
}
