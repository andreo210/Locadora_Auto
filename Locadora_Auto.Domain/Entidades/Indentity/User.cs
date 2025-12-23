using Microsoft.AspNetCore.Identity;

namespace Locadora_Auto.Domain.Entidades.Indentity
{
   
    public class User : IdentityUser
    {
        public string? NomeCompleto { get; set; }
        public string? Cpf { get; set; }
        public bool Ativo { get; set; } = true;
        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    }
    

}
