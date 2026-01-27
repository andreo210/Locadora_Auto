using Locadora_Auto.Domain.Auditoria;
using Locadora_Auto.Domain.Entidades.Indentity;

namespace Locadora_Auto.Domain.Entidades
{
    public class Clientes : IAuditoria, ITemporalEntity<ClienteHistorico>
    {
        public int IdCliente { get; private set; }        
        public string? NumeroHabilitacao { get; private set; }
        public DateTime? ValidadeHabilitacao { get; private set; }
        public bool Ativo { get; private set; }
        public int TotalLocacoes { get; private set; }
        public StatusCliente Status { get; private set; }


        //auditoria
        public DateTime DataCriacao { get; set; }
        public string? IdUsuarioCriacao { get; set; }
        public DateTime? DataModificacao { get; set; }
        public string? IdUsuarioModificacao { get; set; }

        //chave estrangeira
        public string IdUser { get; set; } = null!;

        //navegação
        public User? Usuario { get; set; } = null!;
        public Endereco? Endereco { get; set; } = null!;
        public ICollection<Locacao> Locacoes { get; set; } = new List<Locacao>();
        public ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();


        public static Clientes Criar(string numeroHabilitacao, DateTime validadeCnh)
        {
            if (string.IsNullOrWhiteSpace(numeroHabilitacao))
                throw new InvalidOperationException("Nome é obrigatório");

            if (validadeCnh < DateTime.Today)
                throw new InvalidOperationException("CNH inválida");

            return new Clientes
            {
                NumeroHabilitacao = numeroHabilitacao,
                ValidadeHabilitacao = validadeCnh,
                Status = StatusCliente.Ativo,
                Ativo = true,
                DataCriacao = DateTime.Now,
                TotalLocacoes = 0
            };
        }

        public void Bloquear()
        {
            Status = StatusCliente.Bloqueado;
        }

        public void MarcarInadimplente()
        {
            Status = StatusCliente.Inadimplente;
        }

        public void Regularizar()
        {
            Status = StatusCliente.Ativo;
        }

        public bool PodeLocar()
        {
            return Status == StatusCliente.Ativo &&
                   ValidadeHabilitacao >= DateTime.Today;
        }


    }
    public enum StatusCliente
    {
        Ativo,
        Inadimplente,
        Bloqueado
    }
}
