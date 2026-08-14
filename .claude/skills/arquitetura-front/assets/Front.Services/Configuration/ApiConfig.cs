namespace {{RootNamespace}}.Front.Services.Configuration
{
    /// <summary>
    /// Seção <c>ApiConfig</c> do appsettings. A URL precisa terminar em barra:
    /// sem ela o HttpClient descarta o último segmento ao combinar com a rota relativa.
    /// </summary>
    public class ApiConfig
    {
        public string? BaseUrlApiLocacao { get; set; }
    }
}
