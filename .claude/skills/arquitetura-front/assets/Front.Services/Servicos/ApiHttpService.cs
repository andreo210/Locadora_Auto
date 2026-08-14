using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using {{RootNamespace}}.Front.Models.Error;
using {{RootNamespace}}.Front.Services.Utils.Notificacao;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;

namespace {{RootNamespace}}.Front.Services.Servicos
{
    /// <summary>
    /// Único ponto de saída HTTP do front. Traduz falha da Api em notificação e
    /// devolve <c>null</c>/<c>false</c> — nenhuma página precisa ler status code.
    /// </summary>
    public interface IApiHttpService
    {
        Task<T?> GetAsync<T>(string url, CancellationToken ct = default);

        /// <summary>Use quando o status importa (distinguir 201 de 200/204).</summary>
        Task<(TResponse? objeto, HttpStatusCode code)> PostAsync<TResponse, TRequest>(string url, TRequest data, CancellationToken ct = default);

        Task<bool> PostAsync<TRequest>(string url, TRequest data, CancellationToken ct = default);
        Task<TResponse?> PutAsync<TRequest, TResponse>(string url, TRequest data, CancellationToken ct = default);
        Task<bool> PutAsync<TRequest>(string url, TRequest data, CancellationToken ct = default);
        Task<TResponse?> PatchAsync<TRequest, TResponse>(string url, TRequest data, CancellationToken ct = default);
        Task<bool> PatchAsync<TRequest>(string url, TRequest data, CancellationToken ct = default);
        Task<bool> DeleteAsync(string url, CancellationToken ct = default);

        /// <summary>Upload multipart. Remova junto com a referência a IBrowserFile se o projeto não sobe arquivo.</summary>
        Task<bool> PostMultipartAsync(string url, List<IBrowserFile> arquivos, string campoArquivo, long tamanhoMaximo = 10 * 1024 * 1024, CancellationToken ct = default);
    }

    public class ApiHttpService : IApiHttpService
    {
        private readonly HttpClient _http;
        private readonly INotificationService _notificacao;
        private readonly ILogger<ApiHttpService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        // A notificação é dependência obrigatória: ela é o caminho de saída do erro.
        // Opcional (= null) só adia o problema para uma NullReferenceException dentro do catch.
        public ApiHttpService(HttpClient http, INotificationService notificacao, ILogger<ApiHttpService> logger)
        {
            _http = http;
            _notificacao = notificacao;
            _logger = logger;

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }

        public async Task<T?> GetAsync<T>(string url, CancellationToken ct = default)
        {
            var resposta = await _http.GetAsync(url, ct);
            await TratarErros(resposta, ct);

            return resposta.IsSuccessStatusCode
                ? await Desserializar<T>(resposta, ct)
                : default;
        }

        public async Task<(TResponse? objeto, HttpStatusCode code)> PostAsync<TResponse, TRequest>(
            string url, TRequest data, CancellationToken ct = default)
        {
            var resposta = await _http.PostAsJsonAsync(url, data, _jsonOptions, ct);
            await TratarErros(resposta, ct);

            if (!resposta.IsSuccessStatusCode)
                return (default, resposta.StatusCode);

            return (await Desserializar<TResponse>(resposta, ct), resposta.StatusCode);
        }

        public async Task<bool> PostAsync<TRequest>(string url, TRequest data, CancellationToken ct = default)
        {
            var resposta = await _http.PostAsJsonAsync(url, data, _jsonOptions, ct);
            await TratarErros(resposta, ct);
            return resposta.IsSuccessStatusCode;
        }

        public async Task<TResponse?> PutAsync<TRequest, TResponse>(string url, TRequest data, CancellationToken ct = default)
        {
            var resposta = await _http.PutAsJsonAsync(url, data, _jsonOptions, ct);
            await TratarErros(resposta, ct);

            return resposta.IsSuccessStatusCode
                ? await Desserializar<TResponse>(resposta, ct)
                : default;
        }

        public async Task<bool> PutAsync<TRequest>(string url, TRequest data, CancellationToken ct = default)
        {
            var resposta = await _http.PutAsJsonAsync(url, data, _jsonOptions, ct);
            await TratarErros(resposta, ct);
            return resposta.IsSuccessStatusCode;
        }

        public async Task<TResponse?> PatchAsync<TRequest, TResponse>(string url, TRequest data, CancellationToken ct = default)
        {
            var resposta = await _http.PatchAsJsonAsync(url, data, _jsonOptions, ct);
            await TratarErros(resposta, ct);

            return resposta.IsSuccessStatusCode
                ? await Desserializar<TResponse>(resposta, ct)
                : default;
        }

        public async Task<bool> PatchAsync<TRequest>(string url, TRequest data, CancellationToken ct = default)
        {
            var resposta = await _http.PatchAsJsonAsync(url, data, _jsonOptions, ct);
            await TratarErros(resposta, ct);
            return resposta.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteAsync(string url, CancellationToken ct = default)
        {
            var resposta = await _http.DeleteAsync(url, ct);
            await TratarErros(resposta, ct);
            return resposta.IsSuccessStatusCode;
        }

        public async Task<bool> PostMultipartAsync(
            string url, List<IBrowserFile> arquivos, string campoArquivo,
            long tamanhoMaximo = 10 * 1024 * 1024, CancellationToken ct = default)
        {
            using var conteudo = new MultipartFormDataContent();

            foreach (var arquivo in arquivos)
            {
                var stream = arquivo.OpenReadStream(tamanhoMaximo, ct);
                var parte = new StreamContent(stream);
                parte.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(arquivo.ContentType);
                conteudo.Add(parte, campoArquivo, arquivo.Name);
            }

            var resposta = await _http.PostAsync(url, conteudo, ct);
            await TratarErros(resposta, ct);
            return resposta.IsSuccessStatusCode;
        }

        private async Task<T?> Desserializar<T>(HttpResponseMessage resposta, CancellationToken ct)
        {
            if (resposta.StatusCode == HttpStatusCode.NoContent)
                return default;

            var json = await resposta.Content.ReadAsStringAsync(ct);

            if (string.IsNullOrWhiteSpace(json))
                return default;

            try
            {
                return JsonSerializer.Deserialize<T>(json, _jsonOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Resposta de {Url} não pôde ser lida como {Tipo}",
                    resposta.RequestMessage?.RequestUri, typeof(T).Name);
                _notificacao.ShowError("A resposta do servidor veio em formato inesperado");
                return default;
            }
        }

        /// <summary>
        /// Roda depois de toda resposta. É este método que permite às páginas não terem
        /// try/catch para regra de negócio: quando elas veem null/false, o toast já saiu.
        /// </summary>
        private async Task TratarErros(HttpResponseMessage resposta, CancellationToken ct)
        {
            if (resposta.IsSuccessStatusCode)
                return;

            var corpo = await resposta.Content.ReadAsStringAsync(ct);
            var status = resposta.StatusCode;
            var caminho = resposta.RequestMessage?.RequestUri?.PathAndQuery ?? "desconhecido";

            // Log por faixa: 404 é rotina e não deve poluir o log de erro;
            // 5xx sempre carrega o corpo, que é onde está a pista.
            switch (status)
            {
                case HttpStatusCode.NotFound:
                    _logger.LogInformation("Recurso não encontrado: {Caminho}", caminho);
                    break;
                case HttpStatusCode.Unauthorized:
                case HttpStatusCode.Forbidden:
                case HttpStatusCode.Conflict:
                    _logger.LogWarning("{Status} em {Caminho}", (int)status, caminho);
                    break;
                case HttpStatusCode.BadRequest:
                    _logger.LogDebug("Requisição inválida em {Caminho}: {Corpo}", caminho, corpo);
                    break;
                default:
                    _logger.LogError("{Status} em {Caminho}: {Corpo}", (int)status, caminho, corpo);
                    break;
            }

            if (status != HttpStatusCode.BadRequest)
            {
                _notificacao.ShowError(MensagemPorStatus(status));
                return;
            }

            // 400 é o caso importante: aqui chegam as notificações que o serviço da Api
            // acumulou. A mensagem foi escrita lá, uma vez só — não reescreva.
            if (!TentarLer<ValidationProblemDetails>(corpo, out var problema))
            {
                _notificacao.ShowError(MensagemPorStatus(status));
                return;
            }

            if (problema?.Errors is { Count: > 0 })
            {
                _notificacao.ShowValidationErrors(problema.Errors);
                return;
            }

            if (!string.IsNullOrWhiteSpace(problema?.Detail))
            {
                _notificacao.ShowError(problema.Detail);
                return;
            }

            if (TentarLer<ErrorResponse>(corpo, out var simples) && !string.IsNullOrWhiteSpace(simples?.Message))
            {
                _notificacao.ShowError(simples!.Message!);
                return;
            }

            _notificacao.ShowError(MensagemPorStatus(status));
        }

        // Corpo de erro nem sempre é JSON: proxy devolve HTML, gateway devolve texto puro.
        // Deixar o JsonSerializer estourar aqui derruba o tratamento de erro junto.
        private bool TentarLer<T>(string corpo, out T? resultado)
        {
            resultado = default;

            if (string.IsNullOrWhiteSpace(corpo))
                return false;

            try
            {
                resultado = JsonSerializer.Deserialize<T>(corpo, _jsonOptions);
                return resultado is not null;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private static string MensagemPorStatus(HttpStatusCode status) => status switch
        {
            HttpStatusCode.BadRequest => "Requisição inválida",
            HttpStatusCode.Unauthorized => "Sessão expirada. Faça login novamente",
            HttpStatusCode.Forbidden => "Acesso negado",
            HttpStatusCode.NotFound => "Recurso não encontrado",
            HttpStatusCode.Conflict => "Este registro foi alterado por outra pessoa. Recarregue e tente de novo",
            HttpStatusCode.InternalServerError => "Erro interno do servidor",
            _ => $"Erro na requisição: {(int)status}"
        };
    }
}
