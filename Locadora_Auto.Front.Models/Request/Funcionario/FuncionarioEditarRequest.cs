using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Locadora_Auto.Front.Models.Request.Funcionario
{
    public class FuncionarioEditarRequest
    {
        public string Matricula { get; set; } = null!;
        public string Nome { get; set; } = null!;
        public string? Cargo { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Telefone { get; set; } = string.Empty;
        public List<string> Permissoes { get; set; } = new();
        public bool? Ativo { get; set; }
        [Required(ErrorMessage = "Senha é obrigatória")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Senha deve ter no mínimo 6 caracteres")]
        [DataType(DataType.Password)]
        public string Senha { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirmação de senha é obrigatória")]
        [DataType(DataType.Password)]
        [Compare("Senha", ErrorMessage = "As senhas não conferem")]
        public string ConfirmeSenha { get; set; } = string.Empty;
    }
}
