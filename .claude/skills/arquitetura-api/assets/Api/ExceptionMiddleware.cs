using {{RootNamespace}}.Application.Extensions;
using System.Net;
using System.Text.Json;

namespace {{RootNamespace}}.Api.Middleware
{
    /// <summary>
    /// Último recurso: converte qualquer exceção não tratada em ProblemDetails, para que nem
    /// erro inesperado quebre o contrato de resposta da Api.
    ///
    /// Registre depois da autenticação e antes do MapControllers:
    /// <c>app.UseMiddleware&lt;ExceptionMiddleware&gt;();</c>
    /// </summary>
    public sealed class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;
        private readonly IHostEnvironment _ambiente;

        public ExceptionMiddleware(
            RequestDelegate next,
            ILogger<ExceptionMiddleware> logger,
            IHostEnvironment ambiente)
        {
            _next = next;
            _logger = logger;
            _ambiente = ambiente;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro não tratado em {Caminho}", context.Request.Path);

                var problem = ExceptionProblemFactory.Create(context, ex);

                // mensagem de exceção inesperada vaza estrutura interna (SQL, caminho de arquivo):
                // detalha só fora de produção
                if (!_ambiente.IsDevelopment() && ex is not ProblemException)
                    problem.Detail = "Erro inesperado ao processar a requisição.";

                // se a resposta já começou a ser escrita, não há o que fazer
                if (context.Response.HasStarted) throw;

                context.Response.StatusCode = problem.Status ?? (int)HttpStatusCode.InternalServerError;
                context.Response.ContentType = "application/problem+json";

                await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
            }
        }
    }
}
