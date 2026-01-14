using Locadora_Auto.Domain.Auditoria;
using Locadora_Auto.Domain.Entidades.Indentity;

namespace Locadora_Auto.Domain.Entidades
{
    public class Clientes : IAuditoria, ITemporalEntity<ClienteHistorico>
    {
        public int IdCliente { get; set; }        
        public string? NumeroHabilitacao { get; set; }
        public DateTime? ValidadeHabilitacao { get; set; }
        public bool Status { get; set; }
        public int TotalLocacoes { get; set; }

        //chave estrangeira
        public string IdUser { get; set; } = null!;


        //auditoria
        public DateTime DataCriacao { get; set; }
        public string? IdUsuarioCriacao { get; set; }
        public DateTime? DataModificacao { get; set; }
        public string? IdUsuarioModificacao { get; set; }

        //navegação
        public User? Usuario { get; set; } = null!;
        public Endereco? Endereco { get; set; } = null!;
        public ICollection<Locacao> Locacoes { get; set; } = [];
        public ICollection<Reserva> Reservas { get; set; } = [];
    }

}
