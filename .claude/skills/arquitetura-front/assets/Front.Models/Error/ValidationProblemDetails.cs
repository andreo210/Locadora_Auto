using System.Text.Json.Serialization;

namespace {{RootNamespace}}.Front.Models.Error
{
    /// <summary>
    /// Espelho do <c>ProblemDetails</c> (RFC 7807) que a Api devolve quando o
    /// notificador acumulou erros de regra de negócio.
    /// </summary>
    public class ValidationProblemDetails
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("status")]
        public int? Status { get; set; }

        [JsonPropertyName("detail")]
        public string? Detail { get; set; }

        [JsonPropertyName("instance")]
        public string? Instance { get; set; }

        /// <summary>Campo → mensagens. É daqui que sai o toast de validação.</summary>
        [JsonPropertyName("errors")]
        public Dictionary<string, string[]>? Errors { get; set; }

        [JsonPropertyName("traceId")]
        public string? TraceId { get; set; }

        /// <summary>Reduz o problema a uma linha, para log ou para toast simples.</summary>
        public string ObterMensagem()
        {
            if (Errors is { Count: > 0 })
                return string.Join("; ", Errors.SelectMany(e => e.Value));

            return Detail ?? Title ?? "Erro na validação";
        }
    }
}
