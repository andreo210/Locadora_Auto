namespace Locadora_Auto.Application.Models.Dto
{
    public class UsuarioDto
    {
        public string Id { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string NomeCompleto { get; set; } = null!;
        public string Cpf { get; set; } = null!;
        public bool Ativo { get; set; }
    }
}
