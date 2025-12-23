namespace Locadora_Auto.Domain.Entidades
{
    public class Dano
    {
        public int IdDano { get; set; }

        public int IdLocacao { get; set; }
        public int IdVistoria { get; set; }
        public Vistoria Vistoria { get; set; } = null!;

        public string Descricao { get; set; } = null!;
        public string Tipo { get; set; } = null!; // risco, amassado, quebra, etc.
        public decimal ValorEstimado { get; set; }

        public DateTime DataRegistro { get; set; } = DateTime.UtcNow;
    }
}

