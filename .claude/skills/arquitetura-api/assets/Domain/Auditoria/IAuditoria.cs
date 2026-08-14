namespace {{RootNamespace}}.Domain.Auditoria
{
    /// <summary>
    /// Entidade que registra quem criou e quem alterou. As quatro propriedades são preenchidas
    /// pelo <c>SaveChangesAsync</c> do contexto a partir do <c>ICurrentUser</c> — por isso o
    /// <c>set</c> é público mesmo em entidade com estado encapsulado: quem escreve é a
    /// infraestrutura, não o código de aplicação.
    /// </summary>
    public interface IAuditoria
    {
        DateTime DataCriacao { get; set; }
        string? IdUsuarioCriacao { get; set; }
        DateTime? DataModificacao { get; set; }
        string? IdUsuarioModificacao { get; set; }
    }
}
