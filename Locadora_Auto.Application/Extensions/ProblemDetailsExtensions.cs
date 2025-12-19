using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Linq.Expressions;
using System.Net;

namespace Locadora_Auto.Application.Extensions
{
    /*
     * ========================= VISÃO GERAL =========================
     * Este conjunto de classes implementa uma abordagem limpa,
     * desacoplada e testável para geração de respostas de erro
     * no padrão RFC 7807 (Problem Details) no ASP.NET Core.
     *
     * Responsabilidades separadas:
     * - ProblemFactory              -> erros HTTP genéricos
     * - ValidationProblemFactory    -> erros de validação
     * - ExceptionProblemFactory     -> conversão de exceções
     * - ProblemException            -> exceção de domínio
     *
     * Uso recomendado:
     * - Controllers: retornam ProblemDetails
     * - Services/Domínio: lançam ProblemException
     * - Pipeline: middleware global de exceção
     */

    // ========================= 1. FACTORY BASE =========================
    public static class ProblemFactory
    {
        /// <summary>
        /// Cria um ProblemDetails genérico para erros HTTP (>= 400)
        /// </summary>
        public static ProblemDetails Create(
            HttpStatusCode status,
            string? detail = null,
            string? title = null,
            string? type = null,
            IDictionary<string, object?>? extensions = null)
        {
            var code = (int)status;

            if (code < 400)
                throw new ArgumentOutOfRangeException(nameof(status), "Status HTTP deve ser >= 400");

            return new ProblemDetails
            {
                Status = code,
                Title = title ?? status.ToString(),
                Type = type,
                Detail = detail,
                Extensions = extensions ?? new Dictionary<string, object?>()
            };
        }
    }

    // ========================= 2. VALIDAÇÃO =========================
    public static class ValidationProblemFactory
    {
        /// <summary>
        /// Cria erro de validação a partir do ModelState
        /// </summary>
        public static ValidationProblemDetails FromModelState(
            ModelStateDictionary modelState,
            HttpStatusCode status = HttpStatusCode.BadRequest)
        {
            return new ValidationProblemDetails(modelState)
            {
                Status = (int)status,
                Title = "Erro de validação"
            };
        }

        /// <summary>
        /// Cria erro de validação para um único campo
        /// </summary>
        public static ValidationProblemDetails Single(
            string key,
            string error,
            HttpStatusCode status = HttpStatusCode.BadRequest)
        {
            return new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [key] = new[] { error }
            })
            {
                Status = (int)status,
                Title = "Erro de validação"
            };
        }

        /// <summary>
        /// Cria erro de validação fortemente tipado via expressão lambda
        /// </summary>
        public static ValidationProblemDetails For<T>(
            Expression<Func<T, object?>> selector,
            string error,
            HttpStatusCode status = HttpStatusCode.BadRequest)
        {
            var key = selector.Body.ToString().Split('.').Last();
            return Single(key, error, status);
        }
    }

    // ========================= 3. EXCEÇÕES =========================
    public static class ExceptionProblemFactory
    {
        /// <summary>
        /// Converte exceções em ProblemDetails
        /// Usado no middleware global
        /// </summary>
        public static ProblemDetails Create(HttpContext context, Exception? exception)
        {
            if (exception is ProblemException problemException)
            {
                return ApplyHttpContext(context, problemException.ProblemDetails);
            }

            return ApplyHttpContext(context,
                ProblemFactory.Create(
                    HttpStatusCode.InternalServerError,
                    exception?.Message ?? "Erro inesperado"
                )
            );
        }

        private static ProblemDetails ApplyHttpContext(HttpContext context, ProblemDetails problem)
        {
            problem.Instance = context.Request.Path;
            problem.Extensions["traceId"] = context.TraceIdentifier;
            return problem;
        }
    }

    // ========================= 4. EXCEÇÃO DE DOMÍNIO =========================
    public sealed class ProblemException : Exception
    {
        public ProblemDetails ProblemDetails { get; }

        public ProblemException(ProblemDetails problemDetails)
            : base(problemDetails.Detail)
        {
            ProblemDetails = problemDetails;
        }
    }
}
