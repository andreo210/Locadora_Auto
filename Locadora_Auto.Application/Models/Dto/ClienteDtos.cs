namespace Locadora_Auto.Application.Models.Dto
{
    public class ClienteBase
    {
        public string Nome { get; set; } = null!;
        public string? Telefone { get; set; }
        public string? Email { get; set; }
        public string? NumeroHabilitacao { get; set; }
        public DateTime? ValidadeHabilitacao { get; set; }
        public EnderecoDto Endereco { get; set; } = null!;
    }

    public class CriarClienteDto : ClienteBase
    {
        public string Cpf { get; set; } = null!;
        public string? Senha { get; set; }
        public string? ConfirmarSenha { get; set; }
    }

    public class AtualizarClienteDto : ClienteBase { }


    public class ClienteDto : ClienteBase
    {
        public int IdCliente { get; set; }
        public string Cpf { get; set; } = null!;
        public bool Ativo { get; set; }
        public int TotalLocacoes { get; set; }
    }

}
