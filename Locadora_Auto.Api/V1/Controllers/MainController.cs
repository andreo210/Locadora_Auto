using Locadora_Auto.Application.Extensions;
using Locadora_Auto.Application.Models.Mappers;
using Locadora_Auto.Application.Services.Notificador;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Net;

namespace Locadora_Auto.Api.V1.Controllers
{
    public abstract class MainController : ControllerBase
    {
        private readonly INotificador _notificador;

        protected MainController(INotificador notificador)
        {
            _notificador = notificador;
        }

        protected ActionResult CustomResponse(object? result = null, HttpStatusCode status = HttpStatusCode.OK)
        {
            if (!_notificador.TemNotificacao())
            {
                if (status == HttpStatusCode.OK) return OkResponse(result);
                if(status == HttpStatusCode.Created) return Created(string.Empty, result);
                if (status == HttpStatusCode.NoContent) return NoContent();
            }
                

            var problem = NotificationProblemAdapterMapper.ToProblemDetails(
                HttpContext,
                _notificador.ObterNotificacoes()
            );

            return StatusCode(problem.Status!.Value, problem);
        }

        protected ActionResult OkResponse(object? result = null)
        {
            return result is null ? Ok() : Ok(result);
        }

        protected ActionResult ProblemResponse(
            HttpStatusCode status,
            string detail,
            string? title = null,
            string? type = "Padrão RCF 7807",
            IDictionary<string, object?>? extensions = null)
        {
            return StatusCode(
                (int)status,
                ProblemFactory.Create(status, detail, title, type, extensions)
            );
        }

        protected ActionResult NotFound(string message)=> ProblemResponse(HttpStatusCode.NotFound, message);

        protected ActionResult Forbidden(string message)=> ProblemResponse(HttpStatusCode.Forbidden, message);


        protected ActionResult ValidationResponse(ModelStateDictionary modelState)
        {
            return ValidationProblem(
                ValidationProblemFactory.FromModelState(modelState)
            );
        }

        protected ActionResult ValidationResponse(
            string key,
            string error)
        {
            return ValidationProblem(
                ValidationProblemFactory.Single(key, error)
            );
        }
    }
}
