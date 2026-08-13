namespace Locadora_Auto.Front.Models.Response
{
    public class SeguroResponse
    {
        public int IdSeguro { get; set; }
        public string? Nome { get; set; }
        public string? Descricao { get; set; }
        public decimal ValorDiaria { get; set; }
        public decimal Franquia { get; set; }
        public string? Cobertura { get; set; }
        public bool Ativo { get; set; }
    }
}
