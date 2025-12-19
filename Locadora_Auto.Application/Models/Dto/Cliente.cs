namespace Locadora_Auto.Application.Models.Dto
{
    public class ClienteCreateDto
    {
        public string Cpf { get; set; } = null!;
        public string Nome { get; set; } = null!;
        public string? Telefone { get; set; }
        public string? Email { get; set; }

        public EnderecoCreateDto Endereco { get; set; } = null!;
    }

    public class ClienteUpdateDto
    {
        public string Nome { get; set; } = null!;
        public string? Telefone { get; set; }
        public string? Email { get; set; }
        public string Status { get; set; } = "ATIVO";
        public EnderecoCreateDto Endereco { get; set; } = null!;
    }

    public class ClienteDto
    {
        public int IdCliente { get; set; }
        public string Cpf { get; set; } = null!;
        public string Nome { get; set; } = null!;
        public string? Telefone { get; set; }
        public string? Email { get; set; }
        public string Status { get; set; } = null!;

        public EnderecoDto Endereco { get; set; } = null!;
    }

}
