namespace Locadora_Auto.Front.Models.Response
{
    public class ClienteResponse
    {
        public string? Nome { get; set; }
        public string? Telefone { get; set; }
        public string? Email { get; set; }
        public string? NumeroHabilitacao { get; set; }
        public DateTime ValidadeHabilitacao { get; set; }
        public EnderecoResponse? Endereco { get; set; }
        public int IdCliente { get; set; }
        public string? Cpf { get; set; }
        public bool Ativo { get; set; }
        public int TotalLocacoes { get; set; }
        public List<ReservaResponse>? Reservas { get; set; }
    }
}
