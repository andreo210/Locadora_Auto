using Locadora_Auto.Domain.Entidades.Indentity;

namespace Locadora_Auto.Domain.Entidades
{
    public class Funcionario
    {
        public int IdFuncionario { get; set; }
        public string Matricula { get; set; } = null!;
        public string? Cargo { get; set; }
        public bool Status { get; set; }

        //chave estrangeira
        public string IdUser { get; set; } = null!;


        //navegação
        public User? Usuario { get; set; } = null!;
        public ICollection<Locacao> Locacoes { get; set; } = [];

    }

}
