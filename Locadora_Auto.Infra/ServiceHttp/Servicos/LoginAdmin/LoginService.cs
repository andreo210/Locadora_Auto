using Locadora_Auto.Infra.Configuration;
using Locadora_Auto.Infra.ServiceHttp.Models.Views.Keycloak;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Net;

namespace Locadora_Auto.Infra.ServiceHttp.Servicos.LoginAdmin
{
    public class LoginService : ILoginService
    {
        private readonly HttpClient _httpClient;
        private readonly KeycloakInternoConfig _dadosInternokeycloak;
        private readonly KeycloakExternoConfig _dadosExternokeycloak;

        #region login keycloak interno
        public LoginService(HttpClient httpClient,
            IOptions<KeycloakInternoConfig> dadosInternokeycloak,
            IOptions<KeycloakExternoConfig> dadosExternokeycloak
            )
        {
            _httpClient = httpClient;
            _dadosInternokeycloak = dadosInternokeycloak.Value;
            _dadosExternokeycloak = dadosExternokeycloak.Value;
        }

        public (TokenServiceView dados, HttpStatusCode status, string mensagem) ObterAutenticacaoAdminInterno()
        {
            KeyValuePair<string, string>[] data = new[]
            {
                new KeyValuePair<string, string>("client_secret", _dadosInternokeycloak.ClientSecretInterno),
                new KeyValuePair<string, string>("grant_type", _dadosInternokeycloak.GrantTypeInterno),
                new KeyValuePair<string, string>("client_id", _dadosInternokeycloak.ClientIdInterno),
            };
            return ObterTokenInterno(_dadosInternokeycloak.UrlLoginKeycloakInterno, data);
        }

        private (TokenServiceView dados, HttpStatusCode status, string mensagem) ObterTokenInterno(string endPoint, KeyValuePair<string, string>[] data)
        {
            try
            {

                HttpResponseMessage resultado = _httpClient.PostAsync(endPoint, new FormUrlEncodedContent(data)).GetAwaiter().GetResult();
                string resposta = resultado.Content.ReadAsStringAsync().Result;
                if (resultado.IsSuccessStatusCode)
                {
                    TokenServiceView auth = JsonConvert.DeserializeObject<TokenServiceView>(resposta);
                    ValuesConfig.KEYCLOAKAUTHINTERNO = auth;
                    return (auth, HttpStatusCode.OK, "OK");
                }
                else
                {
                    return (null, resultado.StatusCode, resultado.Content.ReadAsStringAsync().Result);
                }
            }
            catch (Exception ex)
            {
                return (null, HttpStatusCode.InternalServerError, $"Exception: {ex.Message}");
            }
        }
        #endregion

        #region login keycloak externo
        public (TokenServiceView dados, HttpStatusCode status, string mensagem) ObterAutenticacaoAdminExterno()
        {
            KeyValuePair<string, string>[] data = new[]
            {
                new KeyValuePair<string, string>("client_secret", _dadosExternokeycloak.ClientSecretExterno),
                new KeyValuePair<string, string>("grant_type", _dadosExternokeycloak.GrantTypeExterno),
                new KeyValuePair<string, string>("client_id", _dadosExternokeycloak.ClientIdExterno),
            };
            return ObterTokenExterno(_dadosExternokeycloak.UrlLoginKeycloakExterno, data);
        }
        private (TokenServiceView dados, HttpStatusCode status, string mensagem) ObterTokenExterno(string endPoint, KeyValuePair<string, string>[] data)
        {
            try
            {

                HttpResponseMessage resultado = _httpClient.PostAsync(endPoint, new FormUrlEncodedContent(data)).GetAwaiter().GetResult();
                string resposta = resultado.Content.ReadAsStringAsync().Result;
                if (resultado.IsSuccessStatusCode)
                {
                    TokenServiceView auth = JsonConvert.DeserializeObject<TokenServiceView>(resposta);
                    ValuesConfig.KEYCLOAKAUTHEXTERNO = auth;
                    return (auth, HttpStatusCode.OK, "OK");
                }
                else
                {
                    return (null, resultado.StatusCode, resultado.Content.ReadAsStringAsync().Result);
                }
            }
            catch (Exception ex)
            {
                return (null, HttpStatusCode.InternalServerError, $"Exception: {ex.Message}");
            }
        }

       
        #endregion 
    }
}
