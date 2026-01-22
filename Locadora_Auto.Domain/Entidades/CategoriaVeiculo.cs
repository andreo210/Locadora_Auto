namespace Locadora_Auto.Domain.Entidades
{
    public class CategoriaVeiculo
    {
        public int Id { get; set; }

        public string Nome { get; set; } = null!;

        public decimal ValorDiaria { get; set; }

        public int? LimiteKm { get; set; }

        public decimal? ValorKmExcedente { get; set; }

        public ICollection<Veiculo> Veiculos { get; set; } = new List<Veiculo>();

        
    }


}
