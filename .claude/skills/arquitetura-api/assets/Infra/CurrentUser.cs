using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace {{RootNamespace}}.Infra.Data.CurrentUsers
{
    /// <summary>
    /// Abstrai "quem está logado" para o DbContext conseguir auditar sem conhecer HTTP.
    /// Registrado como scoped.
    /// </summary>
    public interface ICurrentUser
    {
        string? UserId { get; }
        bool IsAuthenticated { get; }
    }

    /// <summary>
    /// Implementação sobre o HttpContext. Fora de uma requisição (job em background, seed,
    /// design time do 'dotnet ef') não há usuário e UserId é null — daí o <c>?? "SYSTEM"</c>
    /// na auditoria.
    ///
    /// É justamente essa dependência de HttpContext que exige um IDesignTimeDbContextFactory:
    /// sem ele, qualquer comando de migration quebraria ao resolver o contexto.
    /// </summary>
    public class CurrentUser : ICurrentUser
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUser(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public bool IsAuthenticated =>
            _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

        public string? UserId =>
            IsAuthenticated
                ? _httpContextAccessor.HttpContext!.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                : null;
    }
}
