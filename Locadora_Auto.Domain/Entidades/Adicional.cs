namespace Locadora_Auto.Domain.Entidades
{
    public class Adicional
    {
        public int IdAdicional { get; set; }
        public string Nome { get; set; } = null!;
        public decimal ValorDiaria { get; set; }
    }

}
