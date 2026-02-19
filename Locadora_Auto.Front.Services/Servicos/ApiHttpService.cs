// Services/Servicos/ApiHttpService.cs
using Locadora_Auto.Front.Models.Error;
using Locadora_Auto.Front.Services.Exceptions;
using Locadora_Auto.Front.Services.Models;
using Locadora_Auto.Front.Services.Servicos.Notificacao;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;


public interface IApiHttpService
{
    Task<T?> GetAsync<T>(string url);
    Task<TResponse?> PostAsync<TResponse, TRequest>(string url, TRequest data);
    Task<TResponse?> PutAsync<TRequest, TResponse>(string url, TRequest data);
    Task<TResponse?> PatchAsync<TRequest, TResponse>(string url, TRequest data);
    Task<bool> DeleteAsync(string url);

    // Overloads para quando não espera retorno
    Task<bool> PostAsync<TRequest>(string url, TRequest data);
    Task<bool> PutAsync<TRequest>(string url, TRequest data);
    Task<bool> PatchAsync<TRequest>(string url, TRequest data);
}

public class ApiHttpService : IApiHttpService
{
    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger<ApiHttpService>? _logger;
    private readonly INotificationService _notificationService;

    public ApiHttpService(HttpClient http, ILogger<ApiHttpService>? logger = null, INotificationService notificationService = null)
    {
        _http = http;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
        _notificationService = notificationService;
    }

    // GET
    public async Task<T?> GetAsync<T>(string url)
    {
        var response = await _http.GetAsync(url);
        await TratarErrosResponse(response);
        return await DeserializarObjetoResponse<T>(response);
    }

    // POST com retorno
    public async Task<TResponse?> PostAsync<TResponse,TRequest>(string url, TRequest data)
    {
        var response = await _http.PostAsJsonAsync(url, data, _jsonOptions);
        await TratarErrosResponse(response);
        return await DeserializarObjetoResponse<TResponse>(response);
    }

    // POST sem retorno
    public async Task<bool> PostAsync<TRequest>(string url, TRequest data)
    {
        var response = await _http.PostAsJsonAsync(url, data, _jsonOptions);
        await TratarErrosResponse(response);
        return true;
    }

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
        return true;
    }

    // PATCH com retorno
    public async Task<TResponse?> PatchAsync<TRequest, TResponse>(string url, TRequest data)
    {
        var response = await _http.PatchAsJsonAsync(url, data, _jsonOptions);
        await TratarErrosResponse(response);
        return await DeserializarObjetoResponse<TResponse>(response);
    }

    // PATCH sem retorno
    public async Task<bool> PatchAsync<TRequest>(string url, TRequest data)
    {
        var response = await _http.PatchAsJsonAsync(url, data, _jsonOptions);
        await TratarErrosResponse(response);
        return true;
    }

    // DELETE
    public async Task<bool> DeleteAsync(string url)
    {
        var response = await _http.DeleteAsync(url);
        await TratarErrosResponse(response);
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

    // Tratamento de erros especializado para ValidationProblemDetails
    private async Task TratarErrosResponse(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return;

        var content = await response.Content.ReadAsStringAsync();

        try
        {
            // Tenta desserializar como ValidationProblemDetails primeiro
            var validationError = JsonSerializer.Deserialize<ValidationProblemDetails>( content, _jsonOptions);

            if (validationError?.Errors != null && validationError.Errors.Any())
            {
                _notificationService.ShowValidationErrors(validationError.Errors);
                // Cria uma exceção de validação com os detalhes
                throw new ValidationErrorException(validationError.GetErrorMessage(), response.StatusCode,validationError.Errors);
            }


            // Se não for ValidationProblemDetails, tenta como erro simples
            var simpleError = JsonSerializer.Deserialize<ErrorResponse>(content, _jsonOptions);
            if (simpleError?.Message != null)
            {
                _notificationService.ShowError(simpleError.Message);
                throw new CustomHttpRequestException(simpleError.Message, response.StatusCode, simpleError);
            }
        }
        catch (JsonException)
        {
            // Se não conseguir desserializar, usa o conteúdo bruto
            _logger?.LogDebug("Resposta de erro não está em formato JSON esperado");
        }

        // Fallback para erros padrão HTTP
        throw response.StatusCode switch
        {
            HttpStatusCode.Unauthorized =>
                new CustomHttpRequestException("Token inválido ou expirado", response.StatusCode),

            HttpStatusCode.Forbidden =>
                new CustomHttpRequestException("Acesso negado", response.StatusCode),

            HttpStatusCode.BadRequest =>
                new CustomHttpRequestException("Requisição inválida", response.StatusCode),

            HttpStatusCode.NotFound =>
                new CustomHttpRequestException("Recurso não encontrado", response.StatusCode),

            HttpStatusCode.Conflict =>
                new CustomHttpRequestException("Conflito de dados", response.StatusCode),

            HttpStatusCode.InternalServerError =>
                new CustomHttpRequestException("Erro interno do servidor", response.StatusCode),

            _ => new CustomHttpRequestException(
                $"Erro na requisição: {response.StatusCode}",
                response.StatusCode)
        };
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