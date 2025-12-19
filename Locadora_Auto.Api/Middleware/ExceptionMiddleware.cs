using ElmahCore;
using Locadora_Auto.Application.Extensions;
using Newtonsoft.Json;
using System.Net;

namespace Locadora_Auto.Api.Middleware
{
    public sealed class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                // 1. Log centralizado
                ElmahExtensions.RaiseError(ex);

                // 2. Converte exceção → ProblemDetails
                var problem = ExceptionProblemFactory.Create(context, ex);

                // 3. Escreve resposta
                if (!context.Response.HasStarted)
                {
                    context.Response.StatusCode =
                        problem.Status ?? (int)HttpStatusCode.InternalServerError;

                    context.Response.ContentType = "application/json";

                    var json = JsonConvert.SerializeObject(problem);
                    await context.Response.WriteAsync(json);
                }
            }
        }
    }
}
