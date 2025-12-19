namespace Locadora_Auto.Domain.Entidades
{
    public class HistoricoStatusLocacao
    {
        public int Id { get; set; }
        public int IdLocacao { get; set; }
        public string Status { get; set; } = null!;
        public DateTime DataStatus { get; set; }
        public int IdFuncionario { get; set; }

        public Locacao Locacao { get; set; } = null!;
        public Funcionario Funcionario { get; set; } = null!;
    }

}
