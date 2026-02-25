using System.Text.Json.Serialization;

namespace Locadora_Auto.Front.Models.Response
{
    public class FuncionarioResponse
    {
        public int IdFuncionario { get; set; }
        public string UsuarioId { get; set; } = string.Empty;
        public string Cpf { get; set; } = string.Empty;
        public DateTime DataCadastro { get; set; }
        public bool Status { get; set; }
        public string Matricula { get; set; } = null!;
        public string Nome { get; set; } = null!;
        public string? Cargo { get; set; }

        public string Email { get; set; } = string.Empty;
        public string Telefone { get; set; } = string.Empty;
        public List<string> Permissoes { get; set; } = new();
    }
}
