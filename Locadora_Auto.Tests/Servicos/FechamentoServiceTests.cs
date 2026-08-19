using Locadora_Auto.Application.Configuration.Ultils.NotificadorServices;
using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Application.Services.LocacaoServices;
using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Tests.Fabricas;
using Locadora_Auto.Tests.Fakes;
using Xunit;

namespace Locadora_Auto.Tests.Servicos
{
    /// <summary>
    /// A porta da Api do fechamento (backlog `A11`). A regra toda está no domínio e é testada lá;
    /// o que se fixa aqui é o que só a borda faz: <b>recusar como notificação</b> em vez de deixar
    /// a exceção do domínio virar 500, e traduzir o resultado em extrato e avisos.
    ///
    /// As guardas do fechamento são dezenas, e o serviço não as repete — captura a
    /// <c>DomainException</c>, que deixou de ser <c>internal</c> exatamente para isso. Estes testes
    /// são o que garante que o caminho da captura funciona.
    /// </summary>
    public class FechamentoServiceTests
    {
        private const int Funcionario = 1;

        private sealed class Cenario
        {
            public required LocacaoService Service { get; init; }
            public required NotificadorService Notificador { get; init; }
            public required Locacao Locacao { get; init; }
            public required Filial FilialDevolucao { get; init; }
        }

        // ======================= apuração =======================

        [Fact]
        public async Task Apurar_devolve_o_extrato_com_as_linhas_e_o_saldo()
        {
            var cenario = Montar();

            var resultado = await cenario.Service.ApurarFechamentoAsync(
                cenario.Locacao.IdLocacao, new ApurarFechamentoDto { IdFuncionarioApuracao = Funcionario });

            Assert.False(cenario.Notificador.TemNotificacao());
            Assert.NotNull(resultado);

            // 3 diárias de R$ 150,00, sem km excedente e com o tanque cheio nas duas pontas
            Assert.Equal(450m, resultado!.Fechamento.Saldo);
            Assert.True(resultado.Fechamento.Selado);
            Assert.Contains(resultado.Fechamento.Linhas, l => l.Tipo == nameof(TipoLinhaFechamento.Diaria));
            Assert.False(resultado.JaEstavaApurado);

            // e o contrato foi liquidado: sem pagamento nem caução, sobra tudo a cobrar
            Assert.Equal(StatusLocacao.ComSaldoResidual, cenario.Locacao.Status);
            Assert.Equal(450m, resultado.SaldoResidual);
        }

        [Fact]
        public async Task A_linha_do_extrato_carrega_a_base_de_calculo_e_a_natureza()
        {
            // RN-31: é o que o cliente lê para conferir a conta item a item
            var cenario = Montar();

            var resultado = await cenario.Service.ApurarFechamentoAsync(
                cenario.Locacao.IdLocacao, new ApurarFechamentoDto { IdFuncionarioApuracao = Funcionario });

            var diaria = resultado!.Fechamento.Linhas.Single(l => l.Tipo == nameof(TipoLinhaFechamento.Diaria));
            Assert.Equal(nameof(NaturezaLinhaFechamento.Debito), diaria.Natureza);
            Assert.NotEmpty(diaria.BaseCalculo);
            Assert.Equal(3m, diaria.Quantidade);
            Assert.Equal(150m, diaria.ValorUnitario);
        }

        [Fact]
        public async Task Apurar_duas_vezes_devolve_a_mesma_conta_e_nao_grava_de_novo()
        {
            // RN-32: é o que separa uma retentativa de rede de uma cobrança em dobro
            var cenario = Montar();
            var dto = new ApurarFechamentoDto { IdFuncionarioApuracao = Funcionario };

            var primeira = await cenario.Service.ApurarFechamentoAsync(cenario.Locacao.IdLocacao, dto);
            var segunda = await cenario.Service.ApurarFechamentoAsync(cenario.Locacao.IdLocacao, dto);

            Assert.False(cenario.Notificador.TemNotificacao());
            Assert.True(segunda!.JaEstavaApurado);
            Assert.Equal(primeira!.Fechamento.Saldo, segunda.Fechamento.Saldo);
            Assert.Equal(primeira.Fechamento.Linhas.Count, segunda.Fechamento.Linhas.Count);
        }

        [Fact]
        public async Task Apurar_contrato_inexistente_notifica()
        {
            var cenario = Montar();

            var resultado = await cenario.Service.ApurarFechamentoAsync(
                999, new ApurarFechamentoDto { IdFuncionarioApuracao = Funcionario });

            Assert.Null(resultado);
            Assert.Contains(cenario.Notificador.ObterNotificacoes(), n => n.Mensagem.Contains("não encontrada"));
        }

        [Fact]
        public async Task Apurar_sem_o_funcionario_responsavel_notifica()
        {
            // o indicador de vazamento de receita do doc 07 §12 abre por atendente
            var cenario = Montar();

            var resultado = await cenario.Service.ApurarFechamentoAsync(
                cenario.Locacao.IdLocacao, new ApurarFechamentoDto { IdFuncionarioApuracao = 0 });

            Assert.Null(resultado);
            Assert.Contains(cenario.Notificador.ObterNotificacoes(), n => n.Mensagem.Contains("funcionário responsável"));
        }

        [Fact]
        public async Task Apurar_contrato_ainda_nao_devolvido_notifica()
        {
            // doc 07 §6: `Criada → Fechada` é transição proibida. A recusa vem do domínio e chega
            // ao balcão como mensagem, não como 500
            var cenario = Montar(devolver: false);

            var resultado = await cenario.Service.ApurarFechamentoAsync(
                cenario.Locacao.IdLocacao, new ApurarFechamentoDto { IdFuncionarioApuracao = Funcionario });

            Assert.Null(resultado);
            Assert.Contains(cenario.Notificador.ObterNotificacoes(),
                n => n.Mensagem.Contains("depois da devolução"));
        }

        [Fact]
        public async Task Recusa_de_regra_do_dominio_vira_notificacao_e_nao_erro()
        {
            // RN-22: filial de destino não habilitada para one-way bloqueia o fechamento. Sem a
            // captura, esta recusa de negócio sairia como 500 no balcão
            var cenario = Montar(filialDevolucaoDiferente: true, habilitadaOneWay: false);

            var resultado = await cenario.Service.ApurarFechamentoAsync(
                cenario.Locacao.IdLocacao, new ApurarFechamentoDto { IdFuncionarioApuracao = Funcionario });

            Assert.Null(resultado);
            Assert.Contains(cenario.Notificador.ObterNotificacoes(), n => n.Mensagem.Contains("alçada"));
        }

        [Fact]
        public async Task Alcada_informada_libera_a_apuracao_e_assina_a_linha()
        {
            var cenario = Montar(filialDevolucaoDiferente: true, habilitadaOneWay: false);

            var resultado = await cenario.Service.ApurarFechamentoAsync(
                cenario.Locacao.IdLocacao,
                new ApurarFechamentoDto
                {
                    IdFuncionarioApuracao = Funcionario,
                    IdFuncionarioAlcada = 9,
                    MotivoAlcada = "gerente autorizou o recebimento"
                });

            Assert.False(cenario.Notificador.TemNotificacao());
            var linha = resultado!.Fechamento.Linhas
                .Single(l => l.Tipo == nameof(TipoLinhaFechamento.TaxaRetornoOneWay));
            Assert.Equal(9, linha.IdFuncionarioLancamento);
        }

        // ======================= avisos =======================

        [Fact]
        public async Task Avaria_em_analise_vira_aviso_com_o_prazo()
        {
            // RN-24: não entra na conta, mas o cliente precisa saber que existe e até quando
            var cenario = Montar();
            var vistoria = cenario.Locacao.Vistorias.Single(v => v.Tipo == TipoVistoria.Devolucao);
            vistoria.RegistrarDano("risco na porta", TipoDano.Risco, 900m);
            vistoria.Danos.Last().ColocarEmAnalise();

            var resultado = await cenario.Service.ApurarFechamentoAsync(
                cenario.Locacao.IdLocacao, new ApurarFechamentoDto { IdFuncionarioApuracao = Funcionario });

            Assert.Contains(resultado!.Avisos, a => a.Contains("em análise") && a.Contains("Prazo"));
        }

        [Fact]
        public async Task Multa_recusada_por_redundancia_vira_aviso()
        {
            // RN-26: não some em silêncio — quem a lançou precisa saber por que não entrou
            var cenario = Montar();
            cenario.Locacao.AdicionarMulta(TipoMulta.Atraso, 150m);

            var resultado = await cenario.Service.ApurarFechamentoAsync(
                cenario.Locacao.IdLocacao, new ApurarFechamentoDto { IdFuncionarioApuracao = Funcionario });

            Assert.Contains(resultado!.Avisos, a => a.Contains("Atraso") && a.Contains("já está coberta"));
        }

        [Fact]
        public async Task Tanque_nao_cadastrado_vira_aviso_de_receita_perdida()
        {
            // RN-14: some em silêncio se ninguém avisar
            var cenario = Montar(comTanque: false, nivelDevolucao: NivelCombustivel.Meio);

            var resultado = await cenario.Service.ApurarFechamentoAsync(
                cenario.Locacao.IdLocacao, new ApurarFechamentoDto { IdFuncionarioApuracao = Funcionario });

            Assert.Contains(resultado!.Avisos, a => a.Contains("capacidade de tanque"));
        }

        // ======================= leitura do extrato =======================

        [Fact]
        public async Task Obter_o_extrato_de_contrato_sem_apuracao_notifica()
        {
            var cenario = Montar();

            var extrato = await cenario.Service.ObterFechamentoAsync(cenario.Locacao.IdLocacao);

            Assert.Null(extrato);
            Assert.Contains(cenario.Notificador.ObterNotificacoes(), n => n.Mensagem.Contains("ainda não teve a conta apurada"));
        }

        [Fact]
        public async Task Obter_o_extrato_depois_de_apurar_devolve_a_conta()
        {
            var cenario = Montar();
            await cenario.Service.ApurarFechamentoAsync(
                cenario.Locacao.IdLocacao, new ApurarFechamentoDto { IdFuncionarioApuracao = Funcionario });

            var extrato = await cenario.Service.ObterFechamentoAsync(cenario.Locacao.IdLocacao);

            Assert.NotNull(extrato);
            Assert.True(extrato!.Selado);
            Assert.Equal(450m, extrato.Saldo);
            Assert.NotEmpty(extrato.Linhas);
        }

        /// <summary>
        /// Contrato de 3 diárias devolvido no prazo, sem km excedente e com o tanque cheio nas duas
        /// pontas: a conta base é R$ 450,00 de diárias.
        /// </summary>
        private static Cenario Montar(
            bool devolver = true,
            bool comTanque = true,
            NivelCombustivel nivelDevolucao = NivelCombustivel.Cheio,
            bool filialDevolucaoDiferente = false,
            bool habilitadaOneWay = true)
        {
            var inicio = new DateTime(2026, 3, 10, 9, 0, 0, DateTimeKind.Utc);
            var armazem = new ArmazemFake();

            var categoria = Fabrica.Categoria();
            armazem.Semear(categoria);

            var filialRetirada = Fabrica.Filial();
            armazem.Semear(filialRetirada);

            var filialDevolucao = filialRetirada;

            if (filialDevolucaoDiferente)
            {
                filialDevolucao = Fabrica.Filial("Filial Aeroporto");
                armazem.Semear(filialDevolucao);
                filialDevolucao.DefinirParametrosFinanceiros(
                    habilitadaOneWay: habilitadaOneWay, taxaRetornoOneWay: 250m);
            }

            var veiculo = Fabrica.Veiculo(categoria.Id, filialRetirada.IdFilial);
            if (comTanque) veiculo.DefinirCapacidadeTanque(48m);
            armazem.Semear(veiculo);

            var locacao = Fabrica.Locacao(
                veiculo: veiculo,
                dataInicio: inicio,
                dataFimPrevista: inicio.AddDays(3),
                idFilialRetirada: filialRetirada.IdFilial);

            Fabrica.Retirar(locacao);
            locacao.RegistrarVistoria(1, TipoVistoria.Devolucao, nivelDevolucao, 15_100, null);
            Fabrica.DefinirId(locacao.Vistorias.Single(v => v.Tipo == TipoVistoria.Devolucao), 5);

            if (devolver)
                locacao.RegistrarDevolucao(inicio.AddDays(3), filialDevolucao.IdFilial);

            armazem.Semear(locacao);

            // o `Include` não existe sobre o fake: sem isto o serviço recusaria por falta de filial
            Fabrica.LigarNavegacoesDoFechamento(locacao, veiculo, categoria, filialRetirada, filialDevolucao);

            var notificador = new NotificadorService();

            return new Cenario
            {
                Service = Fabrica.LocacaoService(armazem, notificador),
                Notificador = notificador,
                Locacao = locacao,
                FilialDevolucao = filialDevolucao
            };
        }
    }
}
