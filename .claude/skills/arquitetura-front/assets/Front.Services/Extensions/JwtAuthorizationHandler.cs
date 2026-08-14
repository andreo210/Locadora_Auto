using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace {{RootNamespace}}.Front.Services.Extensions
{
    /// <summary>
    /// Injeta o <c>Bearer</c> em toda chamada à Api. O front autentica por cookie e
    /// guarda os tokens no <c>AuthenticationProperties</c>; é de lá que o token sai.
    /// </summary>
    /// <remarks>
    /// Cuidado com o ciclo de vida: em Blazor Server o <c>HttpContext</c> só existe
    /// durante o render inicial. Numa chamada disparada por clique (já no circuito)
    /// o accessor devolve null e a requisição sai sem token. Quando isso for um
    /// problema real, capture o token no início do circuito e guarde-o num serviço
    /// scoped, em vez de relê-lo do contexto a cada requisição.
    /// </remarks>
    public class JwtAuthorizationHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _accessor;

        public JwtAuthorizationHandler(IHttpContextAccessor accessor)
        {
            _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var contexto = _accessor.HttpContext;

            // Authorization já definido explicitamente (login, refresh) tem precedência.
            if (contexto is not null && request.Headers.Authorization is null)
            {
                var token = await contexto.GetTokenAsync("access_token");

                if (!string.IsNullOrWhiteSpace(token))
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
    }
}
