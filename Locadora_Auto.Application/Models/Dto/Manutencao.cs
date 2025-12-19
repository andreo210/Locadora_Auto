namespace Locadora_Auto.Application.Models.Dto
{
    public class ManutencaoDto
    {
        public int IdManutencao { get; set; }
        public int IdVeiculo { get; set; }
        public string Tipo { get; set; } = null!;
        public string? Descricao { get; set; }
        public decimal Custo { get; set; }
        public string Status { get; set; } = null!;
    }
}
