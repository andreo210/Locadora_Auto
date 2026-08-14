namespace {{RootNamespace}}.Domain.Auditoria
{
    /// <summary>
    /// Marca a entidade que quer histórico: antes de cada UPDATE/DELETE o contexto grava uma
    /// linha em <typeparamref name="THistory"/> com os valores originais. Opcional por entidade
    /// — sem a interface, nada acontece.
    /// </summary>
    public interface ITemporalEntity<THistory> where THistory : class, ITemporalHistory, new() { }

    /// <summary>
    /// Espelho da entidade no momento anterior à alteração. As propriedades copiadas são casadas
    /// por nome, então o nome tem que bater com o da entidade — divergiu, o campo fica vazio e
    /// nada reclama. Copie só o que você vai consultar depois; histórico não é backup.
    /// </summary>
    public interface ITemporalHistory
    {
        DateTime DataEvento { get; set; }
        string? Acao { get; set; }
        string? UsuarioEvento { get; set; }
    }
}
