using Locadora_Auto.Infra.Configuration;
using Locadora_Auto.Infra.ServiceHttp.Servicos.LoginAdmin;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net;

namespace Locadora_Auto.Application.Services.JobsBackgroundService
{
    public class TokenExternoBackgroundService : BackgroundService
    {
        private readonly ILoginService _loginService;
        private readonly ILogger<TokenExternoBackgroundService> _logger;
        private readonly TimeSpan _intervalo = TimeSpan.FromMinutes(59);

        public TokenExternoBackgroundService(ILoginService loginService, ILogger<TokenExternoBackgroundService> logger)
        {
            _loginService = loginService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("TokenExternoBackgroundService iniciado.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var retorno = _loginService.ObterAutenticacaoAdminExterno();
                    if (retorno.status == HttpStatusCode.OK)
                    {
                        ValuesConfig.KEYCLOAKAUTHEXTERNO = retorno.dados;
                        _logger.LogInformation("Token externo atualizado com sucesso.");
                    }
                    else
                    {
                        _logger.LogWarning("Falha ao atualizar token externo: {Status} - {Mensagem}", retorno.status, retorno.mensagem);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao tentar atualizar o token externo.");
                }

                await Task.Delay(_intervalo, stoppingToken);
            }

            _logger.LogInformation("TokenExternoBackgroundService finalizado.");
        }
    }
}
