using {{RootNamespace}}.Application.Extensions;
using {{RootNamespace}}.Application.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace {{RootNamespace}}.Application.Models.Mappers
{
    /// <summary>
    /// Ponte entre notificação e resposta HTTP: é o que transforma o que o serviço reportou
    /// no ProblemDetails que o controller devolve.
    /// </summary>
    public static class NotificationProblemAdapterMapper
    {
        public static ProblemDetails ToProblemDetails(HttpContext context, IEnumerable<Notificacao> notificacoes)
        {
            // o maior status vence: se uma das notificações é 403, a resposta inteira não pode sair 400
            var status = notificacoes
                .Select(n => (int)n.Status)
                .DefaultIfEmpty(400)
                .Max();

            var problem = ProblemFactory.Create(
                (HttpStatusCode)status,
                title: "Erro de regra de negócio");

            problem.Instance = context.Request.Path;

            // agrupado por campo para a tela destacar o input; sem campo, cai em "geral"
            problem.Extensions["errors"] = notificacoes
                .GroupBy(n => n.Campo ?? "geral")
                .ToDictionary(g => g.Key, g => g.Select(n => n.Mensagem).ToArray());

            return problem;
        }
    }
}
