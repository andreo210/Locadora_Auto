namespace Locadora_Auto.Domain.Auditoria
{
    public interface IAuditoria
    {
        DateTime DataCriacao { get; set; } 
        string? IdUsuarioCriacao { get; set; }
        DateTime? DataModificacao { get; set; }
        string? IdUsuarioModificacao { get; set; }
    }
}
