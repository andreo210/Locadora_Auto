namespace {{RootNamespace}}.Front.Services.Servicos
{
    /// <summary>Espelho do <c>PaginatedResult&lt;T&gt;</c> devolvido pela Api.</summary>
    public class PaginatedResponse<T>
    {
        public List<T> Items { get; set; } = new();

        /// <summary>Total no servidor — é ele que define quantas páginas existem.</summary>
        public int Total { get; set; }

        public int Pagina { get; set; }

        public int TotalPaginas { get; set; }

        public int ItensPorPagina { get; set; }

        public bool TemPaginaAnterior => Pagina > 1;

        public bool TemProximaPagina => Pagina < TotalPaginas;
    }
}
