namespace Locadora_Auto.Domain
{
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
