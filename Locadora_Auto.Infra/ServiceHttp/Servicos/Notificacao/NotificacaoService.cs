using Locadora_Auto.Infra.Configuration;
using Locadora_Auto.Infra.ServiceHttp.Servicos;
using Microsoft.Extensions.Options;

namespace Locadora_Auto.Infra.ServiceHttp.Servicos.Notificacao
{
    public class NotificacaoService : HttpService, INotificacaoService
    {
        private readonly HttpClient _httpClient;
        private readonly ApiConfig _config;

        public NotificacaoService(HttpClient httpClient, IOptions<ApiConfig> apiConfig)
        {
            ArgumentNullException.ThrowIfNull(apiConfig.Value.BaseUrlNotificacao, "apiConfig.Value.BaseUrlNotificacao");
            httpClient.BaseAddress = new Uri(apiConfig.Value.BaseUrlNotificacao);
            _httpClient = httpClient;
            _config = apiConfig.Value;
        }

        public async Task EnviarNotificacao(string cpf, string protocolo, string mensagem, string status)
        {
            var notificacao = new
            {
                cpf,
                mensagem,
                protocolo,
                status,
                idServico = _config.CodigoServicoNotificacao
            };

            var requestContent = ObterConteudo(notificacao);
            var resposta = await _httpClient.PostAsync($"api/Notificacao", requestContent);
            resposta.EnsureSuccessStatusCode();
        }
    }
}