namespace Locadora_Auto.Front.Services.Servicos
{
    public class PaginatedResponse<T>
    {
        public List<T> Items { get; set; } = new();
        public int Total { get; set; }
        public int Pagina { get; set; }
        public int TotalPaginas { get; set; }
        public int ItensPorPagina { get; set; }

        public bool TemPaginaAnterior => Pagina > 1;
        public bool TemProximaPagina => Pagina < TotalPaginas;
    }
}
