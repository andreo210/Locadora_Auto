namespace Locadora_Auto.Application.Models.Dto
{
    public class ClienteBase
    {
        public string Nome { get; set; } = null!;
        public string? Telefone { get; set; }
        public string? Email { get; set; }
        public EnderecoDto Endereco { get; set; } = null!;
    }

    public class CriarClienteDto : ClienteBase
    {
        public string Cpf { get; set; } = null!;
        public string? NumeroHabilitacao { get; set; }
        public string? Senha { get; set; }
        public string? ConfirmarSenha { get; set; }
        public DateTime? ValidadeHabilitacao { get; set; }
        public bool Status = true;
    }

    public class AtualizarClienteDto : ClienteBase
    {
        public bool Status { get; set; } = true;
        public string? NumeroHabilitacao { get; set; }
        public DateTime? ValidadeHabilitacao { get; set; }
    }

    public class ClienteDto : ClienteBase
    {
        public int IdCliente { get; set; }
        public string Cpf { get; set; } = null!;
         public string? NumeroHabilitacao { get; set; }
        public DateTime? ValidadeHabilitacao { get; set; }
        public bool Status { get; set; }
        public int TotalLocacoes { get; set; }
    }

}
