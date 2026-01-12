using Locadora_Auto.Application.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Net;

namespace Locadora_Auto.Api.V1.Controllers
{
    public abstract class MainController : ControllerBase
    {
        protected IActionResult OkResponse(object? result = null)
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
