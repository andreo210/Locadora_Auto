namespace {{RootNamespace}}.Front.Models.Error
{
    /// <summary>Formato reduzido de erro: quando a Api responde uma mensagem só, sem dicionário.</summary>
    public class ErrorResponse
    {
        public string? Message { get; set; }

        public string? Title { get; set; }

        public int? Status { get; set; }
    }
}
