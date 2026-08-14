namespace {{RootNamespace}}.Domain.Entidades
{
    /// <summary>
    /// Invariante de domínio quebrada — estado impossível pedido à entidade.
    /// Não use para regra de negócio esperada ("já cadastrado", "sem saldo"): isso é
    /// notificação (<c>INotificadorService.Add</c>), que vira ProblemDetails 4xx.
    /// Chegar aqui é sintoma de bug ou de validação que faltou no serviço.
    /// </summary>
    [Serializable]
    public class DomainException : Exception
    {
        public DomainException() { }

        public DomainException(string? message) : base(message) { }

        public DomainException(string? message, Exception? innerException)
            : base(message, innerException) { }
    }
}
