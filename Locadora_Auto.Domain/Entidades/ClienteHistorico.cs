using Locadora_Auto.Domain.Auditoria;

namespace Locadora_Auto.Domain.Entidades
{
    public class ClienteHistorico : ITemporalHistory
    {
        public DateTime DataEvento { get; set; }
        public string? Acao { get; set; }
        public string? UsuarioEvento { get; set; }
        public string? NomeCompleto { get; set; }
        public int IdHistorico{ get; set; }
        public int IdCliente { get; set; }
        public string? NumeroHabilitacao { get; set; }
        public DateTime? ValidadeHabilitacao { get; set; }
        public int TotalLocacoes { get; set; }
    }
}
