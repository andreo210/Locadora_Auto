using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using VaultSharp;
using VaultSharp.Core;
using VaultSharp.V1.AuthMethods.Token;

namespace Locadora_Auto.Application.Configuration.UtilExtensions
{
    public static class VaultService
    {
        private static IVaultClient client;

        private static readonly string keycloakTokenUrl = "http://d-sso.praiagrande.sp.gov.br:8080/realms/interno/protocol/openid-connect/token";
        private static readonly string vaultLoginUrl = "https://vault.praiagrande.sp.gov.br:8200/v1/auth/jwt/login";
        private static readonly string clientId = "vault";
        private static readonly string clientSecret = "V1zZ4Xy5PuBMBeUFDtk0Zep3qpOK9XG5"; // Substitua pela sua secret real
        private static readonly string role = "aplicacoes";

        static VaultService()
        {
            // Construtor não faz nada, pois o token será gerado dinamicamente.
        }

        private static async Task<string> ObterTokenKeycloakAsync()
        {
            using var client = new HttpClient();

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_secret", clientSecret)
            });

            var response = await client.PostAsync(keycloakTokenUrl, content);

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Erro ao obter token do Keycloak: {response.StatusCode}");

            var responseString = await response.Content.ReadAsStringAsync();
            using var json = JsonDocument.Parse(responseString);
            var jwt = json.RootElement.GetProperty("access_token").GetString();

            return jwt!;
        }

        private static async Task<string> AutenticarComVaultAsync(string jwt)
        {
            using var client = new HttpClient();

            var loginPayload = new
            {
                jwt,
                role
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(loginPayload), Encoding.UTF8, "application/json");
            var response = await client.PostAsync(vaultLoginUrl, jsonContent);

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Erro ao autenticar no Vault: {response.StatusCode}");

            var responseString = await response.Content.ReadAsStringAsync();
            using var json = JsonDocument.Parse(responseString);

            var clientToken = json.RootElement
                .GetProperty("auth")
                .GetProperty("client_token")
                .GetString();

            return clientToken!;
        }

        private static async Task InicializarClienteAsync()
        {
            if (client != null)
                return;
            //1° obtem token do keycloak
            var jwt = await ObterTokenKeycloakAsync();

            //2º obtem a senha do vault
            var clientToken = await AutenticarComVaultAsync(jwt);

            var authMethod = new TokenAuthMethodInfo(clientToken);
            var settings = new VaultClientSettings("https://vault.praiagrande.sp.gov.br:8200", authMethod)
            {
                // Ignora certificado SSL autoassinado, caso necessário
                // DisableServerCertificateValidation = true
            };

            client = new VaultClient(settings);
        }

        /// <summary>
        /// Lê um segredo armazenado no Vault no caminho KV v2.
        /// </summary>
        /// <param name="caminho">Caminho lógico do segredo (ex: "segredos/criptografia")</param>
        /// <param name="nome">Nome da chave dentro do segredo (ex: "chaveAES")</param>
        /// <returns>Valor da chave como string</returns>
        public static async Task<string> ObterChaveAsync(string caminho, string endPoint, string chave)
        {
            await InicializarClienteAsync();

            try
            {
                var secret = await client.V1.Secrets.KeyValue.V2.ReadSecretAsync(
                    path: "Recadastro",
                    mountPoint: caminho
                );


                if (secret?.Data?.Data == null || !secret.Data.Data.ContainsKey(chave))
                    throw new Exception($"Chave '{chave}' não encontrada no segredo '{caminho}'.");

                return secret.Data.Data[chave]?.ToString() ?? string.Empty;
            }
            catch (VaultApiException ex)
            {
                throw new Exception($"Erro ao acessar o Vault: {ex.HttpStatusCode} - {ex.Message}");
            }
        }
    }
}
