using Locadora_Auto.Infra.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Locadora_Auto.Application.Jobs.JobsBackgroundService
{
    public class TarefaDiariaBackgroundService : BackgroundService
    {
        private readonly ILogger<TarefaDiariaBackgroundService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public TarefaDiariaBackgroundService(
            ILogger<TarefaDiariaBackgroundService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("TarefaDiariaBackgroundService iniciado.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var agora = DateTime.Now;
                    var proximaExecucao = new DateTime(agora.Year, agora.Month, agora.Day, 3, 0, 0); // 3:00 AM

                    // Se já passou das 3h hoje, agenda para 3h da manhã do dia seguinte
                    if (agora > proximaExecucao)
                        proximaExecucao = proximaExecucao.AddDays(1);

                    var delay = proximaExecucao - agora;
                    _logger.LogInformation("Próxima execução agendada para: {DataHora}", proximaExecucao);

                    await Task.Delay(delay, stoppingToken);

                    // Executa a ação no banco
                    await ExecutarAcaoDiariaAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao executar tarefa diária.");
                }
            }

            _logger.LogInformation("TarefaDiariaBackgroundService finalizado.");
        }

        private async Task ExecutarAcaoDiariaAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Executando tarefa diária no banco...");

            using (var scope = _serviceProvider.CreateScope())
            {
                var seuDbContext = scope.ServiceProvider.GetRequiredService<LocadoraDbContext>();

                // Aqui vai sua lógica de banco, por exemplo:
                var dataLimite = DateTime.UtcNow.AddDays(-30);
                //seuDbContext.Logs.RemoveRange(seuDbContext.Logs.Where(x => x.Data < dataLimite));

                await seuDbContext.SaveChangesAsync(stoppingToken);
                _logger.LogInformation("Tarefa diária concluída com sucesso.");
            }
        }
    }
}
