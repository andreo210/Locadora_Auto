using System.Text.Json.Serialization;

namespace Locadora_Auto.Front.Services.Models
{
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

        [JsonPropertyName("errors")]
        public Dictionary<string, string[]>? Errors { get; set; }

        [JsonPropertyName("traceId")]
        public string? TraceId { get; set; }

        // Método de extensão para obter mensagem amigável
        public string GetErrorMessage()
        {
            if (Errors != null && Errors.Any())
            {
                return string.Join("; ", Errors.SelectMany(e => e.Value));
            }

            return Detail ?? Title ?? "Erro na validação";
        }
    }
}