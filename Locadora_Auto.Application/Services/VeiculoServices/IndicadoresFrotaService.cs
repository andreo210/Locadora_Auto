using Locadora_Auto.Application.Configuration.Ultils.NotificadorServices;
using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Domain.IRepositorio;

namespace Locadora_Auto.Application.Services.VeiculoServices
{
    /// <summary>
    /// Traduz a trilha da RN-37 nos indicadores da seção 12. Serviço próprio, e não mais um método
    /// no <c>VeiculoService</c>, porque aqui não há cadastro nem transição: é leitura de período,
    /// com regra de apuração que não se parece com nada do CRUD do veículo.
    ///
    /// A apuração é feita em memória sobre as linhas do período. É consciente e tem limite: quando
    /// a trilha crescer, isto vira agregação no banco ou tabela de fechamento diário. Enquanto a
    /// frota for de centenas de carros e a RN-37 for recente, ler as linhas é mais barato que
    /// manter um fechamento que ninguém audita.
    /// </summary>
    public class IndicadoresFrotaService : IIndicadoresFrotaService
    {
        /// <summary>Janela usada quando o cliente não informa <c>de</c>.</summary>
        private const int JanelaPadraoDias = 30;

        private readonly IVeiculosRepository _veiculoRepository;
        private readonly IMovimentoVeiculoRepository _movimentoRepository;
        private readonly INotificadorService _notificador;

        public IndicadoresFrotaService(
            IVeiculosRepository veiculoRepository,
            IMovimentoVeiculoRepository movimentoRepository,
            INotificadorService notificador)
        {
            _veiculoRepository = veiculoRepository;
            _movimentoRepository = movimentoRepository;
            _notificador = notificador;
        }

        public async Task<IndicadoresFrotaDto?> ObterAsync(
            DateTime? de = null,
            DateTime? ate = null,
            int? idFilial = null,
            int? idCategoria = null,
            CancellationToken ct = default)
        {
            var agora = DateTime.UtcNow;

            // janela no futuro acumularia tempo que ainda não passou: a última situação de cada
            // carro seria esticada até a data pedida e a utilização sairia diluída
            var fim = NormalizarUtc(ate) is { } informado && informado < agora ? informado : agora;
            var inicio = NormalizarUtc(de) ?? fim.AddDays(-JanelaPadraoDias);

            if (inicio >= fim)
            {
                _notificador.Add("Período inválido: a data inicial deve ser anterior à final.");
                return null;
            }

            // filial e categoria vêm do cadastro atual do veículo, não da trilha. Categoria quase
            // não muda, mas a filial muda a cada devolução one-way (RN-47): o recorte por filial é
            // "onde o carro está hoje", e não "onde ele esteve no período". Enquanto a
            // transferência (RN-48/RN-49) não existir, não há de onde tirar a filial histórica.
            var veiculos = await _veiculoRepository.ObterAsync(
                filtro: v => (idFilial == null || v.FilialAtualId == idFilial)
                             && (idCategoria == null || v.IdCategoria == idCategoria),
                ct: ct);

            var indicadores = new IndicadoresFrotaDto
            {
                De = inicio,
                Ate = fim,
                VeiculosNoRecorte = veiculos.Count
            };

            if (veiculos.Count == 0)
                return indicadores;

            var ids = veiculos.Select(v => v.IdVeiculo).ToList();

            // tudo até o fim da janela, e não só o que está dentro dela: para saber em que situação
            // o carro entrou no período é preciso o último movimento anterior a ele
            var movimentos = await _movimentoRepository.ObterAsync(
                filtro: m => ids.Contains(m.IdVeiculo) && m.DataMovimento <= fim,
                ct: ct);

            var apuracao = new Apuracao();

            foreach (var trilha in movimentos.GroupBy(m => m.IdVeiculo))
                apuracao.Somar(trilha.OrderBy(m => m.IdMovimentoVeiculo).ToList(), inicio, fim);

            return apuracao.Fechar(indicadores);
        }

        /// <summary>
        /// Acumulador do período. Anda pela trilha de um veículo por vez somando quanto tempo ele
        /// passou em cada situação, e recolhendo as preparações pelo caminho.
        /// </summary>
        private sealed class Apuracao
        {
            private readonly Dictionary<StatusVeiculo, TimeSpan> _tempoPorStatus = new();
            private readonly List<TimeSpan> _preparacoes = new();

            private int _veiculosComTrilha;
            private int _preparacoesEmAberto;

            public void Somar(List<MovimentoVeiculo> trilha, DateTime inicio, DateTime fim)
            {
                var anteriores = trilha.Where(m => m.DataMovimento <= inicio).ToList();
                var dentro = trilha.Where(m => m.DataMovimento > inicio && m.DataMovimento <= fim).ToList();

                DateTime cursor;
                StatusVeiculo situacao;

                if (anteriores.Count > 0)
                {
                    // o carro já existia: entra na janela na situação em que o último movimento o deixou
                    cursor = inicio;
                    situacao = anteriores[^1].StatusDestino;
                }
                else
                {
                    // carro cadastrado dentro da janela: o relógio dele começa no cadastro, não na
                    // borda — contar antes disso inventaria frota que ainda não tinha sido comprada
                    if (dentro.Count == 0) return;

                    cursor = dentro[0].DataMovimento;
                    situacao = dentro[0].StatusDestino;
                    dentro = dentro.Skip(1).ToList();
                }

                _veiculosComTrilha++;

                foreach (var movimento in dentro)
                {
                    Acumular(situacao, movimento.DataMovimento - cursor);
                    cursor = movimento.DataMovimento;
                    situacao = movimento.StatusDestino;
                }

                // o trecho aberto vai até o fim da janela: a última situação continua valendo
                Acumular(situacao, fim - cursor);

                RecolherPreparacoes(trilha, inicio, fim);
            }

            /// <summary>
            /// Cada entrada em <see cref="StatusVeiculo.EmPreparacao"/> dentro da janela vira uma
            /// medição. A preparação é contada pela <b>entrada</b>, não pela saída, para que a
            /// média do período seja a do que o pátio recebeu nele.
            /// </summary>
            private void RecolherPreparacoes(List<MovimentoVeiculo> trilha, DateTime inicio, DateTime fim)
            {
                for (var i = 0; i < trilha.Count; i++)
                {
                    var entrada = trilha[i];

                    if (entrada.StatusDestino != StatusVeiculo.EmPreparacao) continue;
                    if (entrada.DataMovimento < inicio || entrada.DataMovimento > fim) continue;

                    // sem movimento seguinte até o fim da janela, o carro ainda estava no pátio
                    if (i + 1 < trilha.Count)
                        _preparacoes.Add(trilha[i + 1].DataMovimento - entrada.DataMovimento);
                    else
                        _preparacoesEmAberto++;
                }
            }

            private void Acumular(StatusVeiculo situacao, TimeSpan tempo)
            {
                if (tempo <= TimeSpan.Zero) return;

                _tempoPorStatus[situacao] = _tempoPorStatus.TryGetValue(situacao, out var atual)
                    ? atual + tempo
                    : tempo;
            }

            public IndicadoresFrotaDto Fechar(IndicadoresFrotaDto indicadores)
            {
                var total = _tempoPorStatus.Values.Aggregate(TimeSpan.Zero, (soma, t) => soma + t);
                var locado = Tempo(StatusVeiculo.Locado);
                var frotaAtiva = total - Tempo(StatusVeiculo.Indisponivel);

                indicadores.VeiculosComTrilha = _veiculosComTrilha;
                indicadores.DiasLocado = Arredondar(locado.TotalDays);
                indicadores.DiasFrotaAtiva = Arredondar(frotaAtiva.TotalDays);

                indicadores.UtilizacaoRealPercentual = frotaAtiva > TimeSpan.Zero
                    ? Math.Round((decimal)(locado.TotalSeconds / frotaAtiva.TotalSeconds) * 100, 2)
                    : 0m;

                indicadores.PreparacoesEncerradas = _preparacoes.Count;
                indicadores.PreparacoesEmAberto = _preparacoesEmAberto;

                indicadores.TempoMedioPreparacaoHoras = _preparacoes.Count > 0
                    ? Arredondar(_preparacoes.Average(p => p.TotalHours))
                    : null;

                indicadores.TempoPorSituacao = _tempoPorStatus
                    .OrderByDescending(par => par.Value)
                    .Select(par => new TempoPorSituacaoDto
                    {
                        IdStatus = (int)par.Key,
                        Status = par.Key.ToString(),
                        Dias = Arredondar(par.Value.TotalDays),
                        PercentualDoTempo = total > TimeSpan.Zero
                            ? Math.Round((decimal)(par.Value.TotalSeconds / total.TotalSeconds) * 100, 2)
                            : 0m
                    })
                    .ToList();

                return indicadores;
            }

            private TimeSpan Tempo(StatusVeiculo situacao)
                => _tempoPorStatus.TryGetValue(situacao, out var tempo) ? tempo : TimeSpan.Zero;

            private static double Arredondar(double valor) => Math.Round(valor, 2);
        }

        /// <summary>
        /// Mesma regra do conversor global do <c>LocadoraDbContext</c>: Local vira UTC,
        /// Unspecified é remarcado como UTC.
        /// </summary>
        private static DateTime? NormalizarUtc(DateTime? data) => data == null
            ? null
            : data.Value.Kind switch
            {
                DateTimeKind.Utc => data.Value,
                DateTimeKind.Local => data.Value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(data.Value, DateTimeKind.Utc)
            };
    }
}
