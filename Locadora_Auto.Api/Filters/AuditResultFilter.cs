using Microsoft.AspNetCore.Mvc.Filters;
using System.Diagnostics;

namespace Locadora_Auto.Api.Filters
{
    /// <summary>
    /// Filtro de resultado personalizado para auditoria.
    /// Registra informações sobre a execução de ações, como nome do controlador,
    /// nome da ação, tipo de resultado, status HTTP e tempo de execução.
    /// </summary>
    public class AuditResultFilter : IResultFilter
    {
        private readonly ILogger<AuditResultFilter> _logger;
        private Stopwatch _stopwatch;

        /// <summary>
        /// Construtor que recebe uma instância de ILogger para registrar os logs de auditoria.
        /// </summary>
        /// <param name="logger">Instância de ILogger injetada pelo sistema.</param>
        public AuditResultFilter(ILogger<AuditResultFilter> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Método executado antes do resultado ser enviado ao cliente.
        /// Inicia o cronômetro para medir o tempo de execução.
        /// </summary>
        /// <param name="context">Contexto da execução do resultado.</param>
        public void OnResultExecuting(ResultExecutingContext context)
        {
            _stopwatch = Stopwatch.StartNew();
        }

        /// <summary>
        /// Método executado após o resultado ser enviado ao cliente.
        /// Registra no log os dados de auditoria da requisição.
        /// </summary>
        /// <param name="context">Contexto da execução do resultado.</param>
        public void OnResultExecuted(ResultExecutedContext context)
        {
            _stopwatch.Stop();

            // Obtém informações da rota atual
            var controller = context.RouteData.Values["controller"];
            var action = context.RouteData.Values["action"];

            // Tipo de resultado retornado (ex: OkResult, ViewResult, etc.)
            var resultType = context.Result.GetType().Name;

            // Código de status HTTP da resposta
            var statusCode = context.HttpContext.Response.StatusCode;

            // Tempo total de execução da ação
            var duration = _stopwatch.ElapsedMilliseconds;

            // Log de auditoria
            _logger.LogInformation($"[AUDITORIA] {controller}/{action} retornou {resultType} com status {statusCode} em {duration}ms.");
        }
    }
}
