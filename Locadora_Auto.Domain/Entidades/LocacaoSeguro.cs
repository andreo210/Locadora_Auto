namespace Locadora_Auto.Domain.Entidades
{
    public class LocacaoSeguro
    {
        public int IdLocacao { get; set; }
        public Locacao Locacao { get; set; } = null!;

        public int IdSeguro { get; set; }
        public Seguro Seguro { get; set; } = null!;
    }
}

