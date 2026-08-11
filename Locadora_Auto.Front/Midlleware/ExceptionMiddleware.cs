using Locadora_Auto.Front.Services.Utils.Notificacao;
using System.Net;

namespace Locadora_Auto.Front.Midlleware
{
    public sealed class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, INotificationService notificacaoService)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex, notificacaoService);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception ex, INotificationService notificacaoService)
        {
            // Log detalhado
            _logger.LogError(ex, "ERRO NÃO TRATADO NO FRONTEND: {Message}", ex.Message);

            // Como é frontend puro, sempre mostra notificação
            notificacaoService.ShowError("Ocorreu um erro inesperado. Tente novamente.", "Erro");

            // Para requisições de API (se houver), retorna JSON
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                await context.Response.WriteAsJsonAsync(new
                {
                    success = false,
                    message = "Erro interno do servidor",
                    errorId = context.TraceIdentifier
                });
            }
            else if (!context.Request.Path.StartsWithSegments("/"))
            {
                // Evita loop de redirecionamento
                context.Response.Redirect("/");
            }
        }
    }
}