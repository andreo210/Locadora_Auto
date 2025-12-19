namespace Locadora_Auto.Domain.Entidades
{
    public class LocacaoAdicional
    {
        public int IdLocacao { get; set; }
        public int IdAdicional { get; set; }
        public int Quantidade { get; set; }

        public Locacao Locacao { get; set; } = null!;
        public Adicional Adicional { get; set; } = null!;
    }

}
