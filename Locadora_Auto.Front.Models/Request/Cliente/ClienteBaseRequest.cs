using System.ComponentModel.DataAnnotations;

namespace Locadora_Auto.Front.Models.Request.Cliente
{
    public class ClienteBaseRequest
    {
        [Required(ErrorMessage = "Nome é obrigatório")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Nome deve ter entre 3 e 100 caracteres")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "Telefone é obrigatório")]
        [StringLength(15, MinimumLength = 10, ErrorMessage = "Telefone inválido")]
        public string Telefone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Número de habilitação é obrigatória")]
        [StringLength(20, MinimumLength = 10, ErrorMessage = "Número de habilitação deve ter entre 3 e 20 caracteres")]
        public string NumeroHabilitacao { get; set; } = string.Empty;

        [Required(ErrorMessage = "Validade da Habilitação é obrigatório")]
        public DateTime ValidadeHabilitacao { get; set; }

        [Required(ErrorMessage = "E-mail é obrigatório")]
        [EmailAddress(ErrorMessage = "E-mail inválido")]
        public string Email { get; set; } = string.Empty;

        public EnderecoRequest Endereco { get; set; } = new();
    }
}
