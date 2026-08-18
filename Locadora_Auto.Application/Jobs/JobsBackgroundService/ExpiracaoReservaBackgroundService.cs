using Locadora_Auto.Application.Services.ReservaServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Locadora_Auto.Application.Jobs.JobsBackgroundService
{
    /// <summary>
    /// Expira as reservas que passaram da data de início sem virar contrato — o no-show.
    ///
    /// Até aqui isso só acontecia se alguém chamasse à mão o
    /// <c>PATCH reservas/expirar-vencidas</c>, e ninguém chama: a reserva vencida não incomoda
    /// quem está no balcão, ela incomoda o <b>cálculo de disponibilidade</b>, que continua
    /// descontando da frota um carro que ninguém foi buscar. O efeito é falta artificial de
    /// veículo — recusar venda com carro no pátio.
    /// </summary>
    public class ExpiracaoReservaBackgroundService : BackgroundService
    {
        /// <summary>
        /// Quinze minutos. Mais espaçado que a preparação (5 min) de propósito: reserva se mede em
        /// horas de antecedência, não em minutos, e o prejuízo de expirar uma reserva 15 minutos
        /// depois do devido é zero — enquanto varrer de minuto em minuto seria consulta à toa.
        /// </summary>
        private const int IntervaloPadraoSegundos = 900;

        /// <summary>Mesma folga de inicialização das outras varreduras.</summary>
        private static readonly TimeSpan AtrasoInicial = TimeSpan.FromSeconds(30);

        private readonly ILogger<ExpiracaoReservaBackgroundService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _intervalo;
        private readonly bool _habilitado;

        public ExpiracaoReservaBackgroundService(
            ILogger<ExpiracaoReservaBackgroundService> logger,
            IServiceProvider serviceProvider,
            IConfiguration config)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;

            var segundos = config.GetValue<int?>("Jobs:ExpiracaoReserva:IntervaloSegundos");

            // intervalo inválido cai no padrão em vez de derrubar a Api: um zero digitado no
            // appsettings viraria laço apertado contra o banco
            _intervalo = TimeSpan.FromSeconds(segundos is > 0 ? segundos.Value : IntervaloPadraoSegundos);

            _habilitado = config.GetValue<bool?>("Jobs:ExpiracaoReserva:Habilitado") ?? true;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_habilitado)
            {
                _logger.LogInformation(
                    "Expiração automática de reserva desligada por configuração; só a porta manual vale.");
                return;
            }

            _logger.LogInformation("Expiração automática de reserva iniciada, a cada {Intervalo}.", _intervalo);

            if (!await EsperarAsync(AtrasoInicial, stoppingToken)) return;

            while (!stoppingToken.IsCancellationRequested)
            {
                await VarrerAsync(stoppingToken);

                if (!await EsperarAsync(_intervalo, stoppingToken)) break;
            }

            _logger.LogInformation("Expiração automática de reserva finalizada.");
        }

        /// <summary>
        /// Uma passada. Engole a exceção pelo mesmo motivo das outras varreduras: ela é periódica e
        /// idempotente, então falha de banco num tick se resolve no próximo — deixar a exceção
        /// subir mataria o laço e a expiração pararia em silêncio.
        /// </summary>
        private async Task VarrerAsync(CancellationToken ct)
        {
            try
            {
                // escopo próprio: serviços e DbContext são scoped e não existem fora de uma
                // requisição
                using var escopo = _serviceProvider.CreateScope();
                var reservaService = escopo.ServiceProvider.GetRequiredService<IReservaService>();

                var expiradas = await reservaService.ExpirarVencidasAsync(ct);

                if (expiradas == 0)
                {
                    _logger.LogDebug("Nenhuma reserva vencida.");
                    return;
                }

                // Information, e não Warning: no-show é rotina do negócio, não exceção operacional.
                // O que ele indica — se subir demais — é problema de política de cancelamento, e
                // isso se lê no indicador, não no log
                _logger.LogInformation("{Expiradas} reserva(s) expiradas por não terem virado contrato.", expiradas);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // desligamento da Api: não é erro
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao expirar as reservas vencidas; próxima tentativa no intervalo.");
            }
        }

        /// <summary>
        /// Espera cancelável. Devolve <c>false</c> quando a Api está desligando, para o laço sair
        /// sem transformar o desligamento normal numa exceção no log.
        /// </summary>
        private static async Task<bool> EsperarAsync(TimeSpan tempo, CancellationToken ct)
        {
            try
            {
                await Task.Delay(tempo, ct);
                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }
    }
}
