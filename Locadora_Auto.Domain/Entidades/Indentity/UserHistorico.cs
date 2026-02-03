using Locadora_Auto.Domain.Auditoria;

namespace Locadora_Auto.Domain.Entidades.Indentity
{
    public class UserHistorico : ITemporalHistory 
    {
        public int IdHistorico { get; set; }
        public string? Id { get; set; }
        public string? NomeCompleto { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public DateTime DataEvento { get; set; }
        public string? Acao { get; set; }
        public string? UsuarioEvento { get; set; }
    }
}
