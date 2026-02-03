using Locadora_Auto.Application.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Locadora_Auto.Application.Models.Mappers
{
    public static class NotificationProblemAdapterMapper
    {
        public static ProblemDetails ToProblemDetails(HttpContext context,IEnumerable<Notificacao> notificacoes)
        {
            var status = notificacoes
                .Select(n => (int)n.Status)
                .DefaultIfEmpty(400)
                .Max();

            var problem = ProblemFactory.Create(
                (HttpStatusCode)status,
                title: "Erro de regra de negócio"
            );

            problem.Instance = context.Request.Path;
            problem.Extensions["errors"] = notificacoes
                .GroupBy(n => n.Campo ?? "geral")
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(n => n.Mensagem).ToArray()
                );
            return problem;
        }
    }
}
