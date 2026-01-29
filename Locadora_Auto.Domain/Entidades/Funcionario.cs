using Locadora_Auto.Domain.Entidades.Indentity;

namespace Locadora_Auto.Domain.Entidades
{
    public class Funcionario
    {
        public int IdFuncionario { get; private set; }
        public string Matricula { get; private set; }
        public string? Cargo { get; private set; }
        public bool Ativo { get; private set; }


        //chave estrangeira
        public string IdUser { get; set; } = null!;


        //navegação
        public User? Usuario { get; set; } = null!;
        public ICollection<Locacao> Locacoes { get; set; } = new List<Locacao>();

        public static Funcionario Criar(string matricula, string cargo)
        {
            if (string.IsNullOrWhiteSpace(matricula))
                throw new InvalidOperationException("maticula é obrigatório");

            if (string.IsNullOrWhiteSpace(cargo))
                throw new InvalidOperationException("cargo é obrigatorio");

            return new Funcionario
            {
                Matricula = matricula,
                Cargo = cargo,
                Ativo = true
            };            
        }

        public void Atualizar(string matricula, string cargo)
        {
            if (string.IsNullOrWhiteSpace(matricula))
                throw new InvalidOperationException("maticula é obrigatório");

            if (string.IsNullOrWhiteSpace(cargo))
                throw new InvalidOperationException("cargo é obrigatorio");

            Matricula = matricula;
            Cargo = cargo;
            Ativo = true;            
        }

        public void Ativar()
        {
            Ativo = true;
        }
        public void Desativar()
        {
            Ativo = false;
        }

    }

}
