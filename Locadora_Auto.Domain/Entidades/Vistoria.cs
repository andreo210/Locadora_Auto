namespace Locadora_Auto.Domain.Entidades
{
    public class Vistoria
    {
        public int IdVistoria { get; set; }
        public int IdLocacao { get; set; }
        public string Tipo { get; set; } = null!;
        public string? Observacoes { get; set; }
        public DateTime DataVistoria { get; set; }
        public int IdFuncionario { get; set; }

        public Locacao Locacao { get; set; } = null!;
        public Funcionario Funcionario { get; set; } = null!;
        public ICollection<Dano> Danos { get; set; } = new List<Dano>();

    }

}
