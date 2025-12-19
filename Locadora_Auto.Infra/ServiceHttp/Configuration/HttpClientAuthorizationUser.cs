using Locadora_Auto.Infra.Users;
using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;

namespace Locadora_Auto.Infra.ServiceHttp.Configuration
{
    /// <summary>
    /// Manipulador HTTP que adiciona o token de autenticação do usuário ao cabeçalho Authorization
    /// das requisições HTTP, utilizando o contexto atual da aplicação.
    /// </summary>
    public class HttpClientAuthorizationUser : DelegatingHandler
    {
        private readonly IHttpContextAccessor _accessor;

        /// <summary>
        /// Inicializa o manipulador com acesso ao contexto HTTP atual.
        /// </summary>
        /// <param name="accessor">Acessor para o contexto HTTP.</param>
        public HttpClientAuthorizationUser(IHttpContextAccessor accessor)
        {
            _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
        }

        /// <summary>
        /// Intercepta a requisição HTTP e insere o token de acesso do usuário no cabeçalho Authorization,
        /// caso esteja disponível no contexto atual.
        /// </summary>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var httpContext = _accessor.HttpContext;

            if (httpContext == null)
                return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

            // Adiciona o cabeçalho Authorization se já estiver presente na requisição original
            if (httpContext.Request.Headers.TryGetValue("Authorization", out var authorizationHeader))
            {
                var authorizationValue = authorizationHeader.ToString();

                if (!string.IsNullOrWhiteSpace(authorizationValue) && !request.Headers.Contains("Authorization"))
                {
                    request.Headers.Add("Authorization", authorizationValue);
                }
            }

            // Adiciona o token do usuário, se ainda não houver Authorization definido
            if (request.Headers.Authorization == null)
            {
                var token = httpContext.User?.ObterTokenUsuario();

                if (!string.IsNullOrWhiteSpace(token))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }
            }

            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
    }


}
