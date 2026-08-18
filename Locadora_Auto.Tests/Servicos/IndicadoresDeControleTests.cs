using Locadora_Auto.Application.Configuration.Ultils.NotificadorServices;
using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Application.Services.VeiculoServices;
using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Tests.Fabricas;
using Locadora_Auto.Tests.Fakes;
using Xunit;

namespace Locadora_Auto.Tests.Servicos
{
    /// <summary>
    /// Os três indicadores de <b>controle</b> da seção 12 — os que sobraram depois da utilização e
    /// do tempo de preparação.
    ///
    /// Os três respondem perguntas diferentes das métricas de frota: bloqueios vencidos pergunta
    /// "que carro sumiu da oferta e ninguém percebeu", tentativas recusadas pergunta "que balcão
    /// está errando a escolha da placa", e transições sem documento é auditoria pura — tem que dar
    /// zero, sempre.
    /// </summary>
    public class IndicadoresDeControleTests
    {
        private const int Responsavel = 1;

        private sealed class Cenario
        {
            public required IndicadoresFrotaService Service { get; init; }
            public required ArmazemFake Armazem { get; init; }
            public required Filial Filial { get; init; }
        }

        private static Cenario Montar()
        {
            var armazem = new ArmazemFake();

            armazem.Semear(Fabrica.Categoria());
            armazem.Semear(Fabrica.Funcionario());

            var filial = Fabrica.Filial();
            armazem.Semear(filial);

            return new Cenario
            {
                Service = Fabrica.IndicadoresFrotaService(armazem, new NotificadorService()),
                Armazem = armazem,
                Filial = filial
            };
        }

        /// <summary>
        /// Veículo já com a trilha semeada — sem ela o serviço nem considera o carro, porque a
        /// apuração inteira anda sobre os movimentos.
        /// </summary>
        private static Veiculo VeiculoComTrilha(ArmazemFake armazem, string placa = "ABC1D23")
        {
            var veiculo = Fabrica.Veiculo(placa: placa);
            armazem.Semear(veiculo);
            Fabrica.SemearTrilha(armazem, veiculo);
            return veiculo;
        }

        // ======================= bloqueios vencidos =======================

        [Fact]
        public async Task Bloqueio_no_prazo_nao_conta_como_vencido()
        {
            var cenario = Montar();
            var veiculo = VeiculoComTrilha(cenario.Armazem);

            var bloqueio = veiculo.Bloquear(MotivoBloqueio.Documental, DateTime.UtcNow.AddDays(3), Responsavel);
            cenario.Armazem.Semear(bloqueio);

            var indicadores = await cenario.Service.ObterAsync();

            Assert.Equal(0, indicadores!.BloqueiosVencidos);
        }

        [Fact]
        public async Task Bloqueio_aberto_passado_do_prazo_conta_como_vencido()
        {
            var cenario = Montar();
            var veiculo = VeiculoComTrilha(cenario.Armazem);

            var bloqueio = veiculo.Bloquear(MotivoBloqueio.Documental, DateTime.UtcNow.AddDays(3), Responsavel);
            // o prazo só pode nascer futuro (RN-52); jogá-lo para trás é o que o relógio faria
            typeof(BloqueioVeiculo).GetProperty(nameof(BloqueioVeiculo.DataPrevistaLiberacao))!
                .SetValue(bloqueio, DateTime.UtcNow.AddDays(-2));

            cenario.Armazem.Semear(bloqueio);

            var indicadores = await cenario.Service.ObterAsync();

            Assert.Equal(1, indicadores!.BloqueiosVencidos);
        }

        [Fact]
        public async Task Bloqueio_ja_liberado_nunca_conta_como_vencido()
        {
            // o indicador conta quem sumiu da oferta e ninguém percebeu, não quem já voltou
            var cenario = Montar();
            var veiculo = VeiculoComTrilha(cenario.Armazem);

            var bloqueio = veiculo.Bloquear(MotivoBloqueio.Documental, DateTime.UtcNow.AddDays(3), Responsavel);
            typeof(BloqueioVeiculo).GetProperty(nameof(BloqueioVeiculo.DataPrevistaLiberacao))!
                .SetValue(bloqueio, DateTime.UtcNow.AddDays(-2));

            veiculo.LiberarBloqueio(bloqueio.IdBloqueioVeiculo);
            cenario.Armazem.Semear(bloqueio);

            var indicadores = await cenario.Service.ObterAsync();

            Assert.Equal(0, indicadores!.BloqueiosVencidos);
        }

        // ======================= transições sem documento =======================

        [Fact]
        public async Task Trilha_sadia_nao_tem_transicao_sem_documento()
        {
            // é o valor esperado em toda operação normal: contrato, OS, bloqueio e transferência
            // sempre citam o documento, porque o domínio não deixa criar movimento sem ele
            var cenario = Montar();
            var veiculo = Fabrica.Veiculo();
            var contrato = Fabrica.Contrato();

            veiculo.Locar(contrato);
            veiculo.RegistrarDevolucao(16_000, cenario.Filial.IdFilial, contrato);
            veiculo.LiberarDaPreparacao();

            cenario.Armazem.Semear(veiculo);
            cenario.Armazem.Semear(contrato);
            Fabrica.SemearTrilha(cenario.Armazem, veiculo);

            // o EF resolve as FKs no insert; aqui isso é feito à mão, que é o estado gravado real
            foreach (var movimento in veiculo.Movimentos.Where(m => m.TipoOrigem == TipoDocumentoOrigem.Contrato))
            {
                typeof(MovimentoVeiculo).GetProperty(nameof(MovimentoVeiculo.IdLocacaoOrigem))!
                    .SetValue(movimento, contrato.IdLocacao);
            }

            var indicadores = await cenario.Service.ObterAsync();

            Assert.Equal(0, indicadores!.TransicoesSemDocumento);
        }

        [Fact]
        public async Task Movimento_de_contrato_sem_a_locacao_e_contado()
        {
            // o cenário que o indicador existe para pegar: a trilha registrou a transição e perdeu
            // o documento que a autorizou — a conciliação de frota deixa de fechar
            var cenario = Montar();
            var veiculo = Fabrica.Veiculo();
            veiculo.Locar(Fabrica.Contrato());

            cenario.Armazem.Semear(veiculo);
            Fabrica.SemearTrilha(cenario.Armazem, veiculo);

            var indicadores = await cenario.Service.ObterAsync();

            Assert.Equal(1, indicadores!.TransicoesSemDocumento);
        }

        [Fact]
        public async Task Movimento_de_patio_nao_e_contado_como_falta()
        {
            // pátio, cadastro, prazo e desmobilização são o próprio ato e não têm documento a
            // citar; tratá-los como falta faria o indicador nascer com um número permanente
            var cenario = Montar();
            var veiculo = Fabrica.Veiculo();
            var contrato = Fabrica.Contrato();

            veiculo.Locar(contrato);
            veiculo.RegistrarDevolucao(16_000, cenario.Filial.IdFilial, contrato);
            veiculo.LiberarDaPreparacao();

            cenario.Armazem.Semear(veiculo);
            cenario.Armazem.Semear(contrato);
            Fabrica.SemearTrilha(cenario.Armazem, veiculo);

            foreach (var movimento in veiculo.Movimentos.Where(m => m.TipoOrigem == TipoDocumentoOrigem.Contrato))
            {
                typeof(MovimentoVeiculo).GetProperty(nameof(MovimentoVeiculo.IdLocacaoOrigem))!
                    .SetValue(movimento, contrato.IdLocacao);
            }

            var indicadores = await cenario.Service.ObterAsync();

            // o movimento de pátio existe na trilha e não entrou na conta
            Assert.Contains(veiculo.Movimentos, m => m.TipoOrigem == TipoDocumentoOrigem.Patio);
            Assert.Equal(0, indicadores!.TransicoesSemDocumento);
        }

        // ======================= tentativas recusadas =======================

        [Fact]
        public async Task Recusas_do_periodo_sao_contadas_e_abertas_por_filial()
        {
            var cenario = Montar();
            VeiculoComTrilha(cenario.Armazem);

            cenario.Armazem.Semear(
                RecusaSobreposicao.Criar(1, 10, DateTime.UtcNow, DateTime.UtcNow.AddDays(2), OrigemRecusa.Consulta),
                RecusaSobreposicao.Criar(1, 10, DateTime.UtcNow, DateTime.UtcNow.AddDays(2), OrigemRecusa.Banco),
                RecusaSobreposicao.Criar(2, 20, DateTime.UtcNow, DateTime.UtcNow.AddDays(2), OrigemRecusa.Consulta));

            var indicadores = await cenario.Service.ObterAsync();

            Assert.Equal(3, indicadores!.TentativasSobreposicaoRecusadas);

            var filialDez = Assert.Single(indicadores.RecusasPorFilial, r => r.IdFilial == 10);
            Assert.Equal(2, filialDez.Total);
            Assert.Equal(1, filialDez.PelaConsulta);
            Assert.Equal(1, filialDez.PeloBanco);
        }

        [Fact]
        public async Task Recusa_fora_da_janela_nao_conta()
        {
            var cenario = Montar();
            VeiculoComTrilha(cenario.Armazem);

            var antiga = RecusaSobreposicao.Criar(
                1, 10, DateTime.UtcNow, DateTime.UtcNow.AddDays(2), OrigemRecusa.Consulta);

            typeof(RecusaSobreposicao).GetProperty(nameof(RecusaSobreposicao.DataRecusa))!
                .SetValue(antiga, DateTime.UtcNow.AddDays(-90));

            cenario.Armazem.Semear(antiga);

            // janela padrão de 30 dias
            var indicadores = await cenario.Service.ObterAsync();

            Assert.Equal(0, indicadores!.TentativasSobreposicaoRecusadas);
        }

        [Fact]
        public async Task Filtro_por_filial_recorta_pela_filial_da_tentativa()
        {
            var cenario = Montar();
            var veiculo = VeiculoComTrilha(cenario.Armazem);

            cenario.Armazem.Semear(
                RecusaSobreposicao.Criar(veiculo.IdVeiculo, cenario.Filial.IdFilial,
                    DateTime.UtcNow, DateTime.UtcNow.AddDays(2), OrigemRecusa.Consulta),
                RecusaSobreposicao.Criar(veiculo.IdVeiculo, 99,
                    DateTime.UtcNow, DateTime.UtcNow.AddDays(2), OrigemRecusa.Consulta));

            var indicadores = await cenario.Service.ObterAsync(idFilial: cenario.Filial.IdFilial);

            Assert.Equal(1, indicadores!.TentativasSobreposicaoRecusadas);
        }
    }
}
