using Locadora_Auto.Application.Services.LocacaoServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Locadora_Auto.Application.Jobs.JobsBackgroundService
{
    /// <summary>
    /// RN-60: marca como <c>Atrasada</c> a locação que passou do fim previsto com o carro ainda na
    /// rua.
    ///
    /// É varredura porque atraso é fato do <b>relógio</b>, não de um clique. Ninguém no balcão vai
    /// marcar como atrasado o contrato de um cliente que sumiu — e é exatamente esse o que precisa
    /// aparecer. Enquanto ele fica <c>EmAndamento</c>, a carteira de atraso é sempre zero e a
    /// cobrança de diária excedente perde o gancho.
    ///
    /// <c>Atrasada</c> hoje não cobra nada: <c>RegistrarDevolucao</c> aceita os dois estados e o
    /// fechamento financeiro (bloco A) ainda não apura. O que ela faz agora é tornar o contrato
    /// visível, que é o pré-requisito de tudo o que vier depois.
    /// </summary>
    public class AtrasoLocacaoBackgroundService : BackgroundService
    {
        /// <summary>
        /// Dez minutos. O atraso não vira dinheiro por minuto — a hora excedente é contada por hora
        /// iniciada (doc 07 §9) —, então varrer mais fino não muda a cobrança de ninguém; e muito
        /// mais grosso atrasaria a lista que o balcão usa para telefonar ao cliente.
        /// </summary>
        private const int IntervaloPadraoSegundos = 600;

        /// <summary>Mesma folga de inicialização das outras varreduras.</summary>
        private static readonly TimeSpan AtrasoInicial = TimeSpan.FromSeconds(30);

        private readonly ILogger<AtrasoLocacaoBackgroundService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _intervalo;
        private readonly bool _habilitado;

        public AtrasoLocacaoBackgroundService(
            ILogger<AtrasoLocacaoBackgroundService> logger,
            IServiceProvider serviceProvider,
            IConfiguration config)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;

            var segundos = config.GetValue<int?>("Jobs:AtrasoLocacao:IntervaloSegundos");

            // intervalo inválido cai no padrão em vez de derrubar a Api
            _intervalo = TimeSpan.FromSeconds(segundos is > 0 ? segundos.Value : IntervaloPadraoSegundos);

            _habilitado = config.GetValue<bool?>("Jobs:AtrasoLocacao:Habilitado") ?? true;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_habilitado)
            {
                _logger.LogInformation(
                    "Marcação automática de locação atrasada desligada por configuração.");
                return;
            }

            _logger.LogInformation(
                "Marcação automática de locação atrasada iniciada, a cada {Intervalo}.", _intervalo);

            if (!await EsperarAsync(AtrasoInicial, stoppingToken)) return;

            while (!stoppingToken.IsCancellationRequested)
            {
                await VarrerAsync(stoppingToken);

                if (!await EsperarAsync(_intervalo, stoppingToken)) break;
            }

            _logger.LogInformation("Marcação automática de locação atrasada finalizada.");
        }

        /// <summary>
        /// Uma passada. Engole a exceção pelo mesmo motivo das outras varreduras: periódica e
        /// idempotente, tick perdido se resolve no próximo.
        /// </summary>
        private async Task VarrerAsync(CancellationToken ct)
        {
            try
            {
                using var escopo = _serviceProvider.CreateScope();
                var locacaoService = escopo.ServiceProvider.GetRequiredService<ILocacaoService>();

                var marcadas = await locacaoService.MarcarAtrasadasAsync(ct);

                if (marcadas == 0)
                {
                    _logger.LogDebug("Nenhuma locação passou do fim previsto.");
                    return;
                }

                // Warning: cada linha aqui é um carro da casa na rua além do combinado. É exceção
                // operacional, e alguém precisa ligar para o cliente
                _logger.LogWarning(
                    "{Marcadas} locação(ões) passaram do fim previsto com o veículo na rua.", marcadas);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // desligamento da Api: não é erro
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao marcar as locações atrasadas; próxima tentativa no intervalo.");
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
