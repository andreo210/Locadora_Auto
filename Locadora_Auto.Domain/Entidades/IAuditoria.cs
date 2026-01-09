namespace Locadora_Auto.Domain.Entidades
{
    public interface IAuditoria
    {
        DateTime DataCriacao { get; set; }
        string? IdUsuarioCriacao { get; set; }
        DateTime? DataModificacao { get; set; }
        string? IdUsuarioModificacao { get; set; }
    }
}
