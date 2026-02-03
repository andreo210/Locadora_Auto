namespace Locadora_Auto.Domain.Auditoria
{
    public interface ITemporalEntity<THistory> where THistory : class, ITemporalHistory, new()  { }

    public interface ITemporalHistory
    {
        DateTime DataEvento { get; set; }
        string Acao { get; set; }
        string? UsuarioEvento { get; set; }

    }

}
