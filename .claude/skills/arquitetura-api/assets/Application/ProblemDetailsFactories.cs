using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Net;

namespace {{RootNamespace}}.Application.Extensions
{
    /*
     * Geração de respostas de erro no padrão RFC 7807 (ProblemDetails).
     *
     * Formato único de erro é o que permite o cliente tratar falha num lugar só, em vez de
     * adivinhar o formato endpoint a endpoint.
     *
     * - ProblemFactory           -> erros HTTP genéricos
     * - ValidationProblemFactory -> erros de validação (formato / DataAnnotations)
     * - ExceptionProblemFactory  -> conversão de exceção, usada pelo middleware
     * - ProblemException         -> exceção que já carrega o ProblemDetails pronto
     */

    // ========================= 1. FACTORY BASE =========================
    public static class ProblemFactory
    {
        public static ProblemDetails Create(
            HttpStatusCode status,
            string? detail = null,
            string? title = null,
            string? type = null,
            IDictionary<string, object?>? extensions = null)
        {
            var codigo = (int)status;

            if (codigo < 400)
                throw new ArgumentOutOfRangeException(nameof(status), "Status HTTP deve ser >= 400");

            return new ProblemDetails
            {
                Status = codigo,
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
        /// <summary>Erro de formato vindo das DataAnnotations do DTO.</summary>
        public static ValidationProblemDetails FromModelState(
            ModelStateDictionary modelState,
            HttpStatusCode status = HttpStatusCode.BadRequest)
            => new(modelState)
            {
                Status = (int)status,
                Title = "Erro de validação"
            };

        public static ValidationProblemDetails Single(
            string key,
            string error,
            HttpStatusCode status = HttpStatusCode.BadRequest)
            => new(new Dictionary<string, string[]> { [key] = new[] { error } })
            {
                Status = (int)status,
                Title = "Erro de validação"
            };

        /// <summary>Versão tipada: o nome do campo sai da expressão, não de string solta.</summary>
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
        public static ProblemDetails Create(HttpContext context, Exception? exception)
        {
            if (exception is ProblemException problemException)
                return AplicarContexto(context, problemException.ProblemDetails);

            // conflito de concorrência otimista: outra pessoa gravou a mesma linha entre a leitura
            // e a gravação. É condição esperada, não defeito — 409, e não 500.
            if (exception is DbUpdateConcurrencyException)
                return AplicarContexto(context,
                    ProblemFactory.Create(
                        HttpStatusCode.Conflict,
                        "O registro foi alterado por outro usuário enquanto você editava. Recarregue a tela e refaça a operação.",
                        title: "Conflito de concorrência"));

            return AplicarContexto(context,
                ProblemFactory.Create(
                    HttpStatusCode.InternalServerError,
                    exception?.Message ?? "Erro inesperado"));
        }

        private static ProblemDetails AplicarContexto(HttpContext context, ProblemDetails problem)
        {
            problem.Instance = context.Request.Path;
            problem.Extensions["traceId"] = context.TraceIdentifier;
            return problem;
        }
    }

    // ========================= 4. EXCEÇÃO COM STATUS =========================
    /// <summary>
    /// Aborta o fluxo com um status específico. Use com parcimônia: no caminho normal a
    /// resposta de erro nasce do notificador, não de exceção.
    /// </summary>
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
