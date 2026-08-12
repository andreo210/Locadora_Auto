namespace Locadora_Auto.Front.Models.Response
{
    public class ManutencaoResponse
    {
        public int IdManutencao { get; set; }
        public int IdVeiculo { get; set; }
        public int IdTipoManutencao { get; set; }
        public string? Tipo { get; set; }
        public string? Descricao { get; set; }
        public decimal Custo { get; set; }
        public DateTime DataInicio { get; set; }
        public DateTime? DataFim { get; set; }
        public int IdStatus { get; set; }
        public string? Status { get; set; }
    }
}
