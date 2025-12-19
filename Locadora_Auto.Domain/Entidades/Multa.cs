namespace Locadora_Auto.Domain.Entidades
{
    public class Multa
    {
        public int IdMulta { get; set; }
        public int IdLocacao { get; set; }
        public string Tipo { get; set; } = null!;
        public decimal Valor { get; set; }
        public string Status { get; set; } = null!;

        public Locacao Locacao { get; set; } = null!;
    }

}
