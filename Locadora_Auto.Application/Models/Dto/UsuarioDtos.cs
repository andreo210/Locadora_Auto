namespace Locadora_Auto.Application.Models.Dto
{
    public class UserDto
    {
        public string Id { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string NomeCompleto { get; set; } = null!;
        public string Cpf { get; set; } = null!;
        public bool Ativo { get; set; }
    }

    public class CreateUserDto
    {
        public string Email { get; set; } = null!;
        public string NomeCompleto { get; set; } = null!;
        public string Cpf { get; set; } = null!;
        public bool Ativo { get; set; }
        public string Password { get; set; } = null!;
        public string RepeatPassword { get; set; } = null!;
    }
}
