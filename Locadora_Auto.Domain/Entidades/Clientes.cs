namespace Locadora_Auto.Domain.Entidades
{
    public class Clientes : IAuditoria
    {
        public int IdCliente { get; set; }
        public string Cpf { get; set; } = null!;
        public string Nome { get; set; } = null!;
        public string? Telefone { get; set; }
        public string? Email { get; set; }
        public string? NumeroHabilitacao { get; set; }
        public DateTime? ValidadeHabilitacao { get; set; }
        public bool Status { get; set; }
        public int TotalLocacoes { get; set; }
         
        //auditoria
        public DateTime DataCriacao { get; set; }
        public string? IdUsuarioCriacao { get; set; }
        public DateTime? DataModificacao { get; set; }
        public string? IdUsuarioModificacao { get; set; }

        //navegação
        public Endereco Endereco { get; set; } = null!;
        public ICollection<Locacao> Locacoes { get; set; } = [];
        public ICollection<Reserva> Reservas { get; set; } = [];
    }

}
