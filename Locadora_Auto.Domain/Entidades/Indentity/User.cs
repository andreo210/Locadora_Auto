using Locadora_Auto.Domain.Auditoria;
using Microsoft.AspNetCore.Identity;

namespace Locadora_Auto.Domain.Entidades.Indentity
{
   
    public class User : IdentityUser, ITemporalEntity<UserHistorico>
    {
        public string? NomeCompleto { get; set; }
        public string? Cpf { get; set; }
        public bool Ativo { get; set; } = true;
        public DateTime DataCriacao { get; set; } = DateTime.Now;

        //navegação
        public Clientes? Cliente { get; set; }
        public Funcionario? Funcionario { get; set; }

    }
    

}
