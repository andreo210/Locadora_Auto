using Locadora_Auto.Infra.Configuration;
using System.Net.Http.Headers;

namespace Locadora_Auto.Infra.ServiceHttp.Configuration
{
    /// <summary>
    /// Manipulador HTTP responsável por adicionar o token de autenticação do administrador interno
    /// no cabeçalho Authorization das requisições HTTP.
    /// </summary>
    public class HttpClientAuthorizationAdminInterno : DelegatingHandler
    {
        /// <summary>
        /// Intercepta a requisição HTTP e insere o token de acesso no cabeçalho Authorization, se disponível.
        /// </summary>
        /// <param name="request">A requisição HTTP que será enviada.</param>
        /// <param name="cancellationToken">Token de cancelamento da operação assíncrona.</param>
        /// <returns>Resposta HTTP após a execução da requisição com o cabeçalho modificado.</returns>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = ValuesConfig.KEYCLOAKAUTHINTERNO?.access_token;

            if (token != null)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }

}
