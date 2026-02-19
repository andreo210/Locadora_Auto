namespace Locadora_Auto.Front.Models.Response
{
    public class UserDto
    {
        public string Id { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string NomeCompleto { get; set; } = null!;
        public string Cpf { get; set; } = null!;
        public bool Ativo { get; set; }
    }
}
