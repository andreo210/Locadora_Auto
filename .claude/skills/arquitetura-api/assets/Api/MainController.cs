using {{RootNamespace}}.Application.Configuration.Ultils.NotificadorServices;
using {{RootNamespace}}.Application.Extensions;
using {{RootNamespace}}.Application.Models.Mappers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Net;

namespace {{RootNamespace}}.Api.Controllers
{
    /// <summary>
    /// Base de todo controller. Concentra a tradução entre o resultado do serviço e a resposta
    /// HTTP, para que nenhum controller precise de try/catch nem de checar notificação na mão.
    /// </summary>
    public abstract class MainController : ControllerBase
    {
        private readonly INotificadorService _notificador;

        protected MainController(INotificadorService notificador)
        {
            _notificador = notificador;
        }

        /// <summary>
        /// Retorno padrão de toda action. Antes de responder sucesso, consulta o notificador
        /// (mesma instância scoped que o serviço usou): havendo notificação, a resposta vira
        /// ProblemDetails com os erros agrupados, e o payload de sucesso é descartado.
        ///
        /// Quando o serviço devolve false/null, chame sem argumento: CustomResponse().
        /// </summary>
        protected ActionResult CustomResponse(object? result = null, HttpStatusCode status = HttpStatusCode.OK)
        {
            if (!_notificador.TemNotificacao())
            {
                if (status == HttpStatusCode.OK) return OkResponse(result);
                if (status == HttpStatusCode.Created) return Created(string.Empty, result);
                if (status == HttpStatusCode.NoContent) return NoContent();
            }

            var problem = NotificationProblemAdapterMapper.ToProblemDetails(
                HttpContext,
                _notificador.ObterNotificacoes());

            return StatusCode(problem.Status!.Value, problem);
        }

        protected ActionResult OkResponse(object? result = null)
            => result is null ? Ok() : Ok(result);

        protected ActionResult ProblemResponse(
            HttpStatusCode status,
            string detail,
            string? title = null,
            string? type = "Padrão RFC 7807",
            IDictionary<string, object?>? extensions = null)
            => StatusCode((int)status, ProblemFactory.Create(status, detail, title, type, extensions));

        protected ActionResult NotFound(string message) => ProblemResponse(HttpStatusCode.NotFound, message);

        protected ActionResult Forbidden(string message) => ProblemResponse(HttpStatusCode.Forbidden, message);

        /// <summary>Erro de formato do DTO (DataAnnotations). Regra de negócio não passa por aqui.</summary>
        protected ActionResult ValidationResponse(ModelStateDictionary modelState)
            => ValidationProblem(ValidationProblemFactory.FromModelState(modelState));

        protected ActionResult ValidationResponse(string key, string error)
            => ValidationProblem(ValidationProblemFactory.Single(key, error));
    }
}
