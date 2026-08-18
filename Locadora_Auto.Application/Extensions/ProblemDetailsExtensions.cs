using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
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

            /*
             * Conflito de concorrência otimista: entre a leitura e a gravação, outra pessoa
             * alterou a mesma linha (o xmin não bate mais). É condição esperada, não defeito,
             * então vira 409 com mensagem que o usuário entende — e não 500.
             *
             * O tratamento fica aqui, e não em cada serviço, porque vale para qualquer entidade.
             * Serviço que queira reagir de outro jeito (recarregar e reaplicar, por exemplo)
             * captura a exceção antes e notifica normalmente.
             */
            if (exception is DbUpdateConcurrencyException)
            {
                return ApplyHttpContext(context,
                    ProblemFactory.Create(
                        HttpStatusCode.Conflict,
                        "O registro foi alterado por outro usuário enquanto você editava. Recarregue a tela e refaça a operação.",
                        title: "Conflito de concorrência"));
            }

            /*
             * Sobreposição de contrato no mesmo veículo (RN-40/RN-41). A checagem no serviço é
             * mensagem amigável; a garantia é a constraint EXCLUDE de tb_locacao, que barra as
             * duas requisições simultâneas que passaram pelo mesmo `if`. Como o conflito é entre
             * usuários — e não defeito — o status é 409, igual ao de concorrência otimista.
             */
            if (EhSobreposicaoDeIntervalo(exception))
            {
                return ApplyHttpContext(context,
                    ProblemFactory.Create(
                        HttpStatusCode.Conflict,
                        "Este veículo já possui contrato em parte do período selecionado. Atualize a tela e escolha outro veículo ou outro período.",
                        title: "Conflito de agenda do veículo"));
            }

            return ApplyHttpContext(context,
                ProblemFactory.Create(
                    HttpStatusCode.InternalServerError,
                    exception?.Message ?? "Erro inesperado"
                )
            );
        }

        /// <summary>
        /// SQLSTATE 23P01 (<c>exclusion_violation</c>). A exceção chega embrulhada no
        /// <c>DbUpdateException</c> do EF, então é preciso olhar a de dentro também.
        ///
        /// O tipo consultado é <see cref="DbException"/>, e não <c>PostgresException</c>: o
        /// <c>SqlState</c> existe na classe base desde o .NET 5, e assim a camada de Application
        /// não passa a depender do driver.
        ///
        /// Está em <see cref="ViolacaoDeExclusao"/>, e não aqui como privado, porque o
        /// <c>LocacaoService</c> precisa reconhecer a mesma exceção para contar a recusa da seção
        /// 12 — e duas cópias do SQLSTATE em lugares diferentes divergem no primeiro ajuste.
        /// </summary>
        private static bool EhSobreposicaoDeIntervalo(Exception? exception)
            => ViolacaoDeExclusao.EhSobreposicaoDeIntervalo(exception);

        private static ProblemDetails ApplyHttpContext(HttpContext context, ProblemDetails problem)
        {
            problem.Instance = context.Request.Path;
            problem.Extensions["traceId"] = context.TraceIdentifier;
            return problem;
        }
    }

    /// <summary>
    /// Reconhece a violação da constraint <c>EXCLUDE</c> de <c>tb_locacao</c> (RN-41).
    ///
    /// Dois lugares perguntam isso e precisam concordar: o <c>ExceptionProblemFactory</c>, que a
    /// traduz para 409, e o <c>LocacaoService</c>, que conta a recusa no indicador da seção 12.
    /// </summary>
    public static class ViolacaoDeExclusao
    {
        private const string ExclusionViolation = "23P01";

        public static bool EhSobreposicaoDeIntervalo(Exception? exception)
            => (exception as DbException ?? exception?.InnerException as DbException)
                ?.SqlState == ExclusionViolation;
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
