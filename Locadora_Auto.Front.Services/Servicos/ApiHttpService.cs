using Locadora_Auto.Front.Models.Error;
using Locadora_Auto.Front.Services.Exceptions;
using Locadora_Auto.Front.Services.Models;
using Locadora_Auto.Front.Services.Utils.Notificacao;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;


public interface IApiHttpService
{
    Task<T?> GetAsync<T>(string url);
    Task<(TResponse? objeto, HttpStatusCode code)> PostAsync<TResponse, TRequest>(string url, TRequest data);
    Task<TResponse?> PutAsync<TRequest, TResponse>(string url, TRequest data);
    Task<TResponse?> PatchAsync<TRequest, TResponse>(string url, TRequest data);
    Task<bool> DeleteAsync(string url);

    // Overloads para quando não espera retorno
    Task<bool> PostAsync<TRequest>(string url, TRequest data);
    Task<bool> PostMultipartAsync(string url, List<IBrowserFile> arquivos, string campoArquivo);
    Task<bool> PutAsync<TRequest>(string url, TRequest data);
    Task<bool> PatchAsync<TRequest>(string url, TRequest data);
}

public class ApiHttpService : IApiHttpService
{
    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger<ApiHttpService>? _logger;
    private readonly INotificationService NotificationService;

    public ApiHttpService(HttpClient http, ILogger<ApiHttpService>? logger = null, INotificationService _NotificationService = null)
    {
        _http = http;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
        NotificationService = _NotificationService;
    }

    // Propriedade para acessar o NotificationService com segurança
   


    // GET
    public async Task<T?> GetAsync<T>(string url)
    {
        var response = await _http.GetAsync(url);
        await TratarErrosResponse(response);
        if (!response.IsSuccessStatusCode)
        {
            return default;
        }
        return await DeserializarObjetoResponse<T>(response);
    }


    public async Task<(TResponse? objeto, HttpStatusCode code)> PostAsync<TResponse, TRequest>(string url, TRequest data)
    {
        var response = await _http.PostAsJsonAsync(url, data);
         await TratarErrosResponse(response);

        if (!response.IsSuccessStatusCode)
        {
            return (default, response.StatusCode);
        }

        var resposta = await DeserializarObjetoResponse<TResponse>(response);
        return (resposta, response.StatusCode);
    }


    // POST sem retorno
    public async Task<bool> PostAsync<TRequest>(string url, TRequest data)
    {
        var response = await _http.PostAsJsonAsync(url, data, _jsonOptions);
        await TratarErrosResponse(response);
        return true;
    }

    // Método para upload de múltiplos arquivos
    public async Task<bool> PostMultipartAsync(string url, List<IBrowserFile> arquivos, string campoArquivo)
    {
        try
        {
            using var content = new MultipartFormDataContent();

            foreach (var arquivo in arquivos)
            {
                var stream = arquivo.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024);
                var streamContent = new StreamContent(stream);
                streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(arquivo.ContentType);
                content.Add(streamContent, campoArquivo, arquivo.Name);
            }

            var response = await _http.PostAsync(url, content);
            await TratarErrosResponse(response);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            // Log do erro
            Console.WriteLine($"Erro no upload: {ex.Message}");
            return false;
        }
    }

    // Método para upload com dados adicionais
    //public async Task<bool> PostMultipartWithDataAsync<TData>(
    //    string url,
    //    List<IBrowserFile> arquivos,
    //    TData? dadosAdicionais = null,
    //    string campoArquivo = "fotos")
    //{
    //    try
    //    {
    //        using var content = new MultipartFormDataContent();

    //        // Adiciona os arquivos
    //        foreach (var arquivo in arquivos)
    //        {
    //            var stream = arquivo.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024);
    //            var streamContent = new StreamContent(stream);
    //            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(arquivo.ContentType);
    //            content.Add(streamContent, campoArquivo, arquivo.Name);
    //        }

    //        // Adiciona dados adicionais se houver
    //        if (dadosAdicionais != null)
    //        {
    //            var json = JsonSerializer.Serialize(dadosAdicionais, _jsonOptions);
    //            var jsonContent = new StringContent(json, Encoding.UTF8, "application/json");
    //            content.Add(jsonContent, "data");
    //        }

    //        var response = await _http.PostAsync(url, content);
    //        await TratarErrosResponse(response);

    //        return response.IsSuccessStatusCode;
    //    }
    //    catch (Exception ex)
    //    {
    //        Console.WriteLine($"Erro no upload: {ex.Message}");
    //        return false;
    //    }
    //}

    // PUT com retorno
    public async Task<TResponse?> PutAsync<TRequest, TResponse>(string url, TRequest data)
    {
        var response = await _http.PutAsJsonAsync(url, data, _jsonOptions);
        await TratarErrosResponse(response);
        return await DeserializarObjetoResponse<TResponse>(response);
    }

    // PUT sem retorno
    public async Task<bool> PutAsync<TRequest>(string url, TRequest data)
    {
        var response = await _http.PutAsJsonAsync(url, data, _jsonOptions);
        await TratarErrosResponse(response);
        if (!response.IsSuccessStatusCode)
        {
            return false;
        }
        return true;
    }

    // PATCH com retorno
    public async Task<TResponse?> PatchAsync<TRequest, TResponse>(string url, TRequest data)
    {
        var response = await _http.PatchAsJsonAsync(url, ObterConteudo(data), _jsonOptions);
        await TratarErrosResponse(response);
        return await DeserializarObjetoResponse<TResponse>(response);
    }

    // PATCH sem retorno
    public async Task<bool> PatchAsync<TRequest>(string url, TRequest data)
    {
        var response = await _http.PatchAsJsonAsync(url, data, _jsonOptions);
        await TratarErrosResponse(response);
        if (!response.IsSuccessStatusCode)
        {
            return false;
        }
        return true;
    }

    // DELETE
    public async Task<bool> DeleteAsync(string url)
    {
        var response = await _http.DeleteAsync(url);
        await TratarErrosResponse(response);
        if(!response.IsSuccessStatusCode)
        {
            return false;
        }
        return true;
    }

    // Deserialize
    private async Task<T?> DeserializarObjetoResponse<T>(HttpResponseMessage response)
    {
        if (response.StatusCode == HttpStatusCode.NoContent)
            return default;

        var json = await response.Content.ReadAsStringAsync();

        if (string.IsNullOrWhiteSpace(json))
            return default;

        return JsonSerializer.Deserialize<T>(json, _jsonOptions);
    }

    protected StringContent? ObterConteudo(object dado)
    {
        var json = JsonSerializer.Serialize(dado);
        var data = new StringContent(json, Encoding.UTF8, "application/json");
        return data;
    }

    // Tratamento de erros especializado para ValidationProblemDetails
    private async Task TratarErrosResponse(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return;

        var content = await response.Content.ReadAsStringAsync();
        var statusCode = response.StatusCode;
        var requestPath = response.RequestMessage?.RequestUri?.PathAndQuery ?? "unknown";

        // ===== LOGS SELETIVOS POR STATUS CODE =====
        switch (statusCode)
        {
            // Erros de servidor (500+) - log como erro
            case HttpStatusCode.InternalServerError:
            case HttpStatusCode.BadGateway:
            case HttpStatusCode.ServiceUnavailable:
            case HttpStatusCode.GatewayTimeout:
                _logger?.LogError("❌ ERRO SERVIDOR: {StatusCode} - {Path} - {Content}",
                    statusCode, requestPath, content);
                break;

            // Erros de autenticação (401, 403) - log como warning
            case HttpStatusCode.Unauthorized:
                _logger?.LogWarning("⚠️ NÃO AUTORIZADO: {StatusCode} - {Path}",
                    statusCode, requestPath);
                break;

            case HttpStatusCode.Forbidden:
                _logger?.LogWarning("⛔ ACESSO NEGADO: {StatusCode} - {Path}",
                    statusCode, requestPath);
                break;

            // Recurso não encontrado (404) - log como info (é normal)
            case HttpStatusCode.NotFound:
                _logger?.LogInformation("📄 RECURSO NÃO ENCONTRADO: {StatusCode} - {Path}",
                    statusCode, requestPath);
                break;

            // Conflito (409) - log como warning
            case HttpStatusCode.Conflict:
                _logger?.LogWarning("⚡ CONFLITO: {StatusCode} - {Path} - {Content}",
                    statusCode, requestPath, content);
                break;

            // BadRequest (400) - log como debug (só em desenvolvimento)
            case HttpStatusCode.BadRequest:
                _logger?.LogDebug("📝 BAD REQUEST: {Path} - {Content}",
                    requestPath, content);
                break;

            // Outros erros - log como warning
            default:
                _logger?.LogWarning("❓ ERRO INESPERADO: {StatusCode} - {Path} - {Content}",
                    statusCode, requestPath, content);
                break;
        }

        // ===== NOTIFICAÇÕES (SUA LÓGICA EXISTENTE) =====
        if (statusCode != HttpStatusCode.BadRequest)
        {
            var mensagem = GetErrorMessageForStatusCode(statusCode);
            NotificationService.ShowError(mensagem);
        }
        else
        {
            // Tenta desserializar como ValidationProblemDetails primeiro
            var validationError = JsonSerializer.Deserialize<ValidationProblemDetails>(content, _jsonOptions);

            if (validationError?.Errors != null && validationError.Errors.Any())
            {
                NotificationService.ShowValidationErrors(validationError.Errors);

                // Log adicional para saber quantos erros de validação foram exibidos
                _logger?.LogInformation("Exibidos {Count} erros de validação para o usuário",
                    validationError.Errors.Count);
            }

            var simpleError = JsonSerializer.Deserialize<ErrorResponse>(content, _jsonOptions);
            if (simpleError?.Message != null)
            {
                NotificationService.ShowError(simpleError.Message);
                await Task.Delay(2000);

                // Log da mensagem de erro simples
                _logger?.LogInformation("Exibida mensagem de erro: {Message}", simpleError.Message);
            }
        }
    }

    private string GetErrorMessageForStatusCode(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.BadRequest => "Requisição inválida",
            HttpStatusCode.Unauthorized => "Sessão expirada. Faça login novamente",
            HttpStatusCode.Forbidden => "Acesso negado",
            HttpStatusCode.NotFound => "Recurso não encontrado",
            HttpStatusCode.Conflict => "Conflito de dados",
            HttpStatusCode.InternalServerError => "Erro interno do servidor",
            _ => $"Erro na requisição: {(int)statusCode}"
        };
    }
}