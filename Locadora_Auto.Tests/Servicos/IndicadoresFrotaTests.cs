using Locadora_Auto.Application.Configuration.Ultils.NotificadorServices;
using Locadora_Auto.Application.Services.VeiculoServices;
using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Tests.Fabricas;
using Locadora_Auto.Tests.Fakes;
using Xunit;

namespace Locadora_Auto.Tests.Servicos
{
    /// <summary>
    /// Os indicadores da seção 12 apurados sobre a trilha da RN-37.
    ///
    /// Todos os cenários ancoram os instantes da trilha com <c>Fabrica.DatarMovimento</c> e usam
    /// uma janela que termina agora — o serviço trunca o fim em <c>UtcNow</c> de propósito, então
    /// janela no futuro não é apurável e não faz sentido montar teste sobre ela.
    /// </summary>
    public class IndicadoresFrotaTests
    {
        private static readonly DateTime Agora = DateTime.UtcNow;

        private sealed class Cenario
        {
            public required IndicadoresFrotaService Service { get; init; }
            public required NotificadorService Notificador { get; init; }
            public required ArmazemFake Armazem { get; init; }
            public required Filial Filial { get; init; }
        }

        private static Cenario Montar()
        {
            var armazem = new ArmazemFake();

            var categoria = Fabrica.Categoria();
            armazem.Semear(categoria);

            var filial = Fabrica.Filial();
            armazem.Semear(filial);

            var notificador = new NotificadorService();

            var service = new IndicadoresFrotaService(
                new VeiculosRepositoryFake(armazem),
                new MovimentoVeiculoRepositoryFake(armazem),
                notificador);

            return new Cenario
            {
                Service = service,
                Notificador = notificador,
                Armazem = armazem,
                Filial = filial
            };
        }

        /// <summary>
        /// Um carro que roda o ciclo completo dentro da janela de 10 dias:
        ///
        /// <code>
        /// -30d cadastro     ──▶ Disponivel   (fora da janela: só define como ele entra nela)
        /// -10d ┤ início da janela
        ///  -9d locação      ──▶ Locado       ( 1 dia disponível)
        ///  -5d devolução    ──▶ EmPreparacao ( 4 dias locado)
        ///  -5d +6h liberação──▶ Disponivel   ( 6 horas de pátio)
        /// agora ┤ fim da janela              (~4,75 dias disponível)
        /// </code>
        /// </summary>
        private static Veiculo VeiculoComCicloCompleto(Cenario cenario, string placa = "ABC1D23", bool liberar = true)
        {
            var veiculo = Fabrica.Veiculo(1, cenario.Filial.IdFilial, placa);
            cenario.Armazem.Semear(veiculo);

            var contrato = Fabrica.Contrato();
            veiculo.Locar(contrato);
            veiculo.RegistrarDevolucao(16_000, cenario.Filial.IdFilial, contrato);
            if (liberar) veiculo.LiberarDaPreparacao();

            var trilha = veiculo.Movimentos.ToList();
            Fabrica.DatarMovimento(trilha[0], Agora.AddDays(-30));
            Fabrica.DatarMovimento(trilha[1], Agora.AddDays(-9));
            Fabrica.DatarMovimento(trilha[2], Agora.AddDays(-5));
            if (liberar) Fabrica.DatarMovimento(trilha[3], Agora.AddDays(-5).AddHours(6));

            Fabrica.SemearTrilha(cenario.Armazem, veiculo);

            return veiculo;
        }

        [Fact]
        public async Task Utilizacao_real_e_o_tempo_locado_sobre_o_tempo_de_frota_ativa()
        {
            var cenario = Montar();
            VeiculoComCicloCompleto(cenario);

            var dto = await cenario.Service.ObterAsync(de: Agora.AddDays(-10));

            Assert.NotNull(dto);
            Assert.Equal(1, dto!.VeiculosNoRecorte);
            Assert.Equal(1, dto.VeiculosComTrilha);

            // 4 dias locado em 10 dias de frota ativa
            Assert.Equal(4, dto.DiasLocado, 1);
            Assert.Equal(10, dto.DiasFrotaAtiva, 1);
            Assert.InRange(dto.UtilizacaoRealPercentual, 39.9m, 40.1m);
        }

        [Fact]
        public async Task Tempo_medio_de_preparacao_conta_so_o_que_o_patio_encerrou()
        {
            var cenario = Montar();
            VeiculoComCicloCompleto(cenario);

            var dto = await cenario.Service.ObterAsync(de: Agora.AddDays(-10));

            Assert.NotNull(dto);
            Assert.Equal(6, dto!.TempoMedioPreparacaoHoras);
            Assert.Equal(1, dto.PreparacoesEncerradas);
            Assert.Equal(0, dto.PreparacoesEmAberto);
        }

        [Fact]
        public async Task Preparacao_sem_liberacao_nao_entra_na_media_mas_e_contada_a_parte()
        {
            var cenario = Montar();
            VeiculoComCicloCompleto(cenario, liberar: false);

            var dto = await cenario.Service.ObterAsync(de: Agora.AddDays(-10));

            // sem este par, um pátio que nunca libera carro exibiria a melhor média da rede
            Assert.NotNull(dto);
            Assert.Null(dto!.TempoMedioPreparacaoHoras);
            Assert.Equal(0, dto.PreparacoesEncerradas);
            Assert.Equal(1, dto.PreparacoesEmAberto);

            // e o carro segue contando como parado no pátio até o fim da janela
            var patio = dto.TempoPorSituacao.Single(t => t.Status == nameof(StatusVeiculo.EmPreparacao));
            Assert.Equal(5, patio.Dias, 1);
        }

        [Fact]
        public async Task Tempo_indisponivel_sai_do_denominador_da_utilizacao()
        {
            var cenario = Montar();
            var veiculo = VeiculoComCicloCompleto(cenario);

            // bloqueio administrativo três dias antes do fim da janela
            veiculo.Desativar();
            var bloqueio = veiculo.Movimentos.Last();
            Fabrica.DatarMovimento(bloqueio, Agora.AddDays(-3));
            cenario.Armazem.Semear(bloqueio);

            var dto = await cenario.Service.ObterAsync(de: Agora.AddDays(-10));

            // 10 dias de janela menos 3 de bloqueio: carro fora da oferta por decisão da casa não
            // é frota operacional e não pode puxar a utilização para baixo
            Assert.NotNull(dto);
            Assert.Equal(7, dto!.DiasFrotaAtiva, 1);
            Assert.InRange(dto.UtilizacaoRealPercentual, 57.0m, 57.3m);
        }

        [Fact]
        public async Task Veiculo_cadastrado_dentro_da_janela_so_conta_a_partir_do_cadastro()
        {
            var cenario = Montar();

            var veiculo = Fabrica.Veiculo(1, cenario.Filial.IdFilial);
            cenario.Armazem.Semear(veiculo);
            Fabrica.DatarMovimento(veiculo.Movimentos.Single(), Agora.AddDays(-2));
            Fabrica.SemearTrilha(cenario.Armazem, veiculo);

            var dto = await cenario.Service.ObterAsync(de: Agora.AddDays(-10));

            // contar os 10 dias inventaria frota que ainda não tinha sido comprada
            Assert.NotNull(dto);
            Assert.Equal(2, dto!.DiasFrotaAtiva, 1);
        }

        [Fact]
        public async Task Periodo_invertido_notifica_em_vez_de_devolver_numero_sem_sentido()
        {
            var cenario = Montar();
            VeiculoComCicloCompleto(cenario);

            var dto = await cenario.Service.ObterAsync(
                de: Agora.AddDays(-1),
                ate: Agora.AddDays(-10));

            Assert.Null(dto);
            Assert.True(cenario.Notificador.TemNotificacao());
        }

        [Fact]
        public async Task Recorte_sem_veiculo_devolve_indicador_zerado_e_nao_erro()
        {
            var cenario = Montar();
            VeiculoComCicloCompleto(cenario);

            var dto = await cenario.Service.ObterAsync(de: Agora.AddDays(-10), idFilial: 999);

            Assert.NotNull(dto);
            Assert.Equal(0, dto!.VeiculosNoRecorte);
            Assert.Equal(0m, dto.UtilizacaoRealPercentual);
            Assert.False(cenario.Notificador.TemNotificacao());
        }
    }
}
