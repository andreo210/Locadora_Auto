namespace Locadora_Auto.Application.Models.Dto
{
    public class CategoriaVeiculoDto
    {
        public int IdCategoria { get; set; }
        public string Nome { get; set; } = null!;
        public decimal ValorDiaria { get; set; }
        public int? LimiteKm { get; set; }
        public decimal? ValorKmExcedente { get; set; }
    }
}
