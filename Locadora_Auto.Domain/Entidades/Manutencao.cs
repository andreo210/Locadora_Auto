namespace Locadora_Auto.Domain.Entidades
{
    public class Manutencao
    {
        public int IdManutencao { get; set; }
        public int IdVeiculo { get; set; }
        public string Tipo { get; set; } = null!;
        public string? Descricao { get; set; }
        public decimal Custo { get; set; }
        public DateTime DataInicio { get; set; }
        public DateTime? DataFim { get; set; }
        public string Status { get; set; } = null!;

        public Veiculo Veiculo { get; set; } = null!;
    }

}
