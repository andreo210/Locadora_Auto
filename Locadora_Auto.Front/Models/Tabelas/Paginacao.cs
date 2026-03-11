namespace Locadora_Auto.Front.Models.Tabelas
{
    public class Paginacao
    {
        public int PaginaAtual { get; set; } = 1;
        public int ItensPorPagina { get; set; } = 10;
        public int TotalItens { get; set; }
        public int TotalPaginas => (int)Math.Ceiling(TotalItens / (double)ItensPorPagina);
    }

    public class FiltrosBase
    {
        public string? TermoBusca { get; set; }
        public int Pagina { get; set; } = 1;
        public int ItensPorPagina { get; set; } = 10;
        public string? OrdenarPor { get; set; }
        public string? Ordem { get; set; } = "asc";
    }

    public class ResultadoPaginado<T>
    {
        public List<T> Items { get; set; } = new();
        public int Total { get; set; }
        public int Pagina { get; set; }
        public int TotalPaginas { get; set; }
    }
}
