namespace Locadora_Auto.Application.Models.Dto
{
    public class CriarCategoriaVeiculoDto
    {
        public string Nome { get; set; } = null!;
        public decimal ValorDiaria { get; set; }
        public int? LimiteKm { get; set; }
        public decimal? ValorKmExcedente { get; set; }
    }
    public class AtualizarCategoriaVeiculoDto
    {
        public string Nome { get; set; } = null!;
        public decimal ValorDiaria { get; set; }
        public int? LimiteKm { get; set; }
        public decimal? ValorKmExcedente { get; set; }
    }
    public class CategoriaVeiculoDto
    {
        public int Id { get; set; }
        public string Nome { get; set; } = null!;
        public decimal ValorDiaria { get; set; }
        public int? LimiteKm { get; set; }
        public decimal? ValorKmExcedente { get; set; }
        public int? TotalVeiculos { get; set; }
    }

}
