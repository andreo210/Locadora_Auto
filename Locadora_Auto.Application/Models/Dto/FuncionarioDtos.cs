using Locadora_Auto.Domain.Entidades;

namespace Locadora_Auto.Application.Models.Dto
{
    public class FuncionarioBaseDto
    {
        public string Matricula { get; set; } = null!;
        public string Nome { get; set; } = null!;
        public string? Cargo { get; set; }
        public bool Status { get; set; } 
        public string Email { get; set; } = string.Empty;
        public string Telefone { get; set; } = string.Empty;
        public List<string> Permissoes { get; set; } = new();
    }

    public class CriarFuncionarioDto : FuncionarioBaseDto
    {
        public string Cpf { get; set; } = string.Empty;        
        public string Senha { get; set; } = string.Empty;
        public string ConfirmeSenha { get; set; } = string.Empty;
    }

    public class AtualizarFuncionarioDto : FuncionarioBaseDto
    {
        public string? Endereco { get; set; }
        public bool? Ativo { get; set; }
    }

    public class FuncionarioDto : FuncionarioBaseDto
    {
        public int IdFuncionario { get; set; }
        public string UsuarioId { get; set; } = string.Empty;
        public string Cpf { get; set; } = string.Empty;
        public DateTime DataCadastro { get; set; }       
    }
}
