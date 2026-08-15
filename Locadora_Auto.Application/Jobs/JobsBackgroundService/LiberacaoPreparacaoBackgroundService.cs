using Locadora_Auto.Application.Services.VeiculoServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Locadora_Auto.Application.Jobs.JobsBackgroundService
{
    /// <summary>
    /// RN-45, parte automática: acorda de tempos em tempos e devolve à oferta os veículos cujo
    /// prazo de preparação venceu sem o pátio ter declarado nada.
    ///
    /// É um <see cref="BackgroundService"/>, e não Hangfire, por escolha: a varredura é
    /// idempotente — ela olha o estado atual do pátio, não uma fila de tarefas —, então tick
    /// perdido se resolve no próximo e não há nada que precise sobreviver a um restart. Fila
    /// persistente, retry e dashboard só pagam o custo quando há trabalho que não pode se perder ou
    /// mais de uma instância disputando; nenhum dos dois é o caso aqui, e o Hangfire ainda exigiria
    /// storage próprio no banco (hoje há só o pacote no csproj, sem extension e com
    /// <c>HangfireConnection</c> vazia).
    ///
    /// Com mais de uma instância da Api as duas varreriam junto. O resultado continua correto: a
    /// segunda encontra o carro já fora de <c>EmPreparacao</c> e a guarda do domínio o descarta, e
    /// mesmo empatando na leitura o token de concorrência (<c>xmin</c>) derruba a segunda gravação.
    /// </summary>
    public class LiberacaoPreparacaoBackgroundService : BackgroundService
    {
        /// <summary>
        /// Cinco minutos. É a granularidade da liberação, não o prazo dela: o carro sai do pátio
        /// no máximo um tick depois de vencer. Menor que isso é varrer o banco à toa — a
        /// preparação se mede em horas —, e muito maior tornaria inútil o parâmetro da filial que
        /// declara preparação curta ou zero.
        /// </summary>
        private const int IntervaloPadraoSegundos = 300;

        /// <summary>
        /// Folga para o host terminar de subir antes da primeira varredura. Sem ela a primeira
        /// passada disputa com a abertura do pool do Npgsql e falha à toa no log de inicialização.
        /// </summary>
        private static readonly TimeSpan AtrasoInicial = TimeSpan.FromSeconds(30);

        private readonly ILogger<LiberacaoPreparacaoBackgroundService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _intervalo;
        private readonly bool _habilitado;

        public LiberacaoPreparacaoBackgroundService(
            ILogger<LiberacaoPreparacaoBackgroundService> logger,
            IServiceProvider serviceProvider,
            IConfiguration config)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;

            var segundos = config.GetValue<int?>("Jobs:LiberacaoPreparacao:IntervaloSegundos");

            // intervalo inválido cai no padrão em vez de derrubar a Api na inicialização: um zero
            // digitado no appsettings viraria laço apertado contra o banco
            _intervalo = TimeSpan.FromSeconds(segundos is > 0 ? segundos.Value : IntervaloPadraoSegundos);

            _habilitado = config.GetValue<bool?>("Jobs:LiberacaoPreparacao:Habilitado") ?? true;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_habilitado)
            {
                _logger.LogInformation(
                    "Liberação automática da preparação desligada por configuração; só a liberação manual do pátio vale.");
                return;
            }

            _logger.LogInformation(
                "Liberação automática da preparação iniciada, a cada {Intervalo}.", _intervalo);

            if (!await EsperarAsync(AtrasoInicial, stoppingToken)) return;

            while (!stoppingToken.IsCancellationRequested)
            {
                await VarrerAsync(stoppingToken);

                if (!await EsperarAsync(_intervalo, stoppingToken)) break;
            }

            _logger.LogInformation("Liberação automática da preparação finalizada.");
        }

        /// <summary>
        /// Uma passada. Engole a exceção de propósito: a varredura é periódica e idempotente, então
        /// falha de banco num tick se resolve no próximo — deixar a exceção subir mataria o laço e
        /// a liberação automática pararia em silêncio até alguém reiniciar a Api, que é o defeito
        /// que este job existe para evitar.
        /// </summary>
        private async Task VarrerAsync(CancellationToken ct)
        {
            try
            {
                // escopo próprio: os serviços e o DbContext são scoped e não existem fora de uma
                // requisição — o job precisa abrir o dele
                using var escopo = _serviceProvider.CreateScope();
                var veiculoService = escopo.ServiceProvider.GetRequiredService<IVeiculoService>();

                var resultado = await veiculoService.LiberarPreparacoesVencidasAsync(ct);

                if (resultado.Liberados == 0)
                {
                    _logger.LogDebug(
                        "Nenhuma preparação vencida. {AindaNoPrazo} veículo(s) no pátio dentro do prazo.",
                        resultado.AindaNoPrazo);
                    return;
                }

                // Warning, e não Information: cada liberação aqui é um carro que voltou à oferta
                // sem ninguém ter conferido. É exceção operacional, não rotina
                _logger.LogWarning(
                    "{Liberados} veículo(s) devolvidos à oferta por vencimento do prazo, sem conferência do pátio " +
                    "({Analisados} no pátio, {AindaNoPrazo} ainda no prazo, {SemCarimbo} sem carimbo de início).",
                    resultado.Liberados,
                    resultado.Analisados,
                    resultado.AindaNoPrazo,
                    resultado.SemCarimbo);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // desligamento da Api: não é erro
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao liberar as preparações vencidas; próxima tentativa no intervalo.");
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
