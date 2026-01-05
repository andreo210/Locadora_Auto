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

        protected IActionResult ProblemResponse(
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

        protected IActionResult NotFound(string message)=> ProblemResponse(HttpStatusCode.NotFound, message);

        protected IActionResult Forbidden(string message)=> ProblemResponse(HttpStatusCode.Forbidden, message);


        protected IActionResult ValidationResponse(ModelStateDictionary modelState)
        {
            return ValidationProblem(
                ValidationProblemFactory.FromModelState(modelState)
            );
        }

        protected IActionResult ValidationResponse(
            string key,
            string error)
        {
            return ValidationProblem(
                ValidationProblemFactory.Single(key, error)
            );
        }
    }
}
