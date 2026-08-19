using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Tests.Fabricas;
using Xunit;

namespace Locadora_Auto.Tests.Dominio
{
    /// <summary>
    /// RN-18 e RN-19: a proteção contratada. O caso comum é trivial — cobrada pelas mesmas diárias
    /// que o período —, e o que se testa aqui é o outro: contratada depois do início ou cancelada no
    /// meio, que a RN-19 manda cobrar pró-rata pela janela em que de fato cobriu.
    ///
    /// A pró-rata só é testável com datas controladas, e por isso quase tudo aqui passa pelo cálculo
    /// puro: <c>LocacaoSeguro.Cancelar()</c> carimba <c>UtcNow</c> de propósito, sem porta para
    /// datar cancelamento para trás.
    /// </summary>
    public class ApuracaoDeProtecaoTests
    {
        private static readonly DateTime Retirada = new(2026, 3, 10, 9, 0, 0, DateTimeKind.Utc);

        private const decimal DiariaDaProtecao = 40m;

        private static ApuracaoDeProtecao Apurar(
            DateTime? contratacao = null,
            DateTime? cancelamento = null,
            int diasDeContrato = 3,
            int diariasCobradas = 3)
            => ApuracaoDeProtecao.Calcular(
                Retirada,
                Retirada.AddDays(diasDeContrato),
                contratacao ?? Retirada,
                cancelamento,
                DiariaDaProtecao,
                diariasCobradas);

        // ======================= RN-18: o caso comum =======================

        [Fact]
        public void Protecao_ativa_o_contrato_inteiro_cobra_as_diarias_do_periodo()
        {
            var apuracao = Apurar();

            Assert.True(apuracao.CobriuOContratoInteiro);
            Assert.Equal(3m, apuracao.Diarias);
            Assert.Equal(120m, apuracao.Total);
        }

        [Fact]
        public void Protecao_integral_acompanha_a_diaria_cobrada_e_nao_a_duracao()
        {
            // contrato de 22h é 1 diária pela RN-02, e a proteção é 1 diária também — não 0,9167.
            // Passar pela conta proporcional aqui produziria um centavo que ninguém explica
            var apuracao = ApuracaoDeProtecao.Calcular(
                Retirada, Retirada.AddHours(22), Retirada, null, DiariaDaProtecao, diariasCobradasDoContrato: 1);

            Assert.Equal(1m, apuracao.Diarias);
            Assert.Equal(40m, apuracao.Total);
        }

        [Fact]
        public void Protecao_acompanha_a_diaria_do_teto_de_horas()
        {
            // o teto da RN-05 soma uma diária ao contrato, e a proteção cobre esse dia também
            var apuracao = Apurar(diasDeContrato: 2, diariasCobradas: 3);

            Assert.Equal(3m, apuracao.Diarias);
            Assert.Equal(120m, apuracao.Total);
        }

        // ======================= RN-19: pró-rata =======================

        [Fact]
        public void Protecao_cancelada_no_meio_e_cobrada_pro_rata()
        {
            // contrato de 3 dias, proteção cancelada ao fim do segundo: 2 diárias, não 3
            var apuracao = Apurar(cancelamento: Retirada.AddDays(2));

            Assert.False(apuracao.CobriuOContratoInteiro);
            Assert.Equal(2m, apuracao.Diarias);
            Assert.Equal(80m, apuracao.Total);
        }

        [Fact]
        public void Protecao_contratada_depois_do_inicio_cobra_a_partir_dali()
        {
            // doc 07 §4: o cliente que vê o trânsito da cidade e liga pedindo proteção no dia
            // seguinte. Cobra 2 das 3 diárias
            var apuracao = Apurar(contratacao: Retirada.AddDays(1));

            Assert.False(apuracao.CobriuOContratoInteiro);
            Assert.Equal(2m, apuracao.Diarias);
            Assert.Equal(80m, apuracao.Total);
        }

        [Fact]
        public void Pro_rata_cobra_a_fracao_do_dia()
        {
            // 2 dias e 6 horas de cobertura são 2,25 diárias — a coluna quantidade tem 4 casas
            // exatamente para isto
            var apuracao = Apurar(cancelamento: Retirada.AddDays(2).AddHours(6));

            Assert.Equal(2.25m, apuracao.Diarias);
            Assert.Equal(90m, apuracao.Total);

            // e a linha se explica: o cliente contesta a fração, então a janela tem que estar lá
            Assert.Contains("pró-rata", apuracao.BaseCalculo());
            Assert.Contains("10/03/2026 09:00", apuracao.BaseCalculo());
        }

        [Fact]
        public void Pro_rata_nunca_passa_das_diarias_do_contrato()
        {
            // contrato de 3 dias e 20h vira 4 diárias cobradas ou 3 com teto; a proteção contratada
            // desde o começo e cancelada só depois da devolução não pode custar mais que o período
            var apuracao = ApuracaoDeProtecao.Calcular(
                Retirada, Retirada.AddDays(3).AddHours(20),
                Retirada.AddMinutes(1), dataCancelamento: null,
                valorDiariaContratada: DiariaDaProtecao, diariasCobradasDoContrato: 3);

            Assert.Equal(3m, apuracao.Diarias);
        }

        [Fact]
        public void Protecao_cancelada_antes_de_comecar_nao_cobra_nada()
        {
            var apuracao = Apurar(
                contratacao: Retirada.AddDays(2),
                cancelamento: Retirada.AddDays(1));

            Assert.Equal(0m, apuracao.Diarias);
            Assert.Equal(0m, apuracao.Total);
        }

        [Fact]
        public void Cancelamento_depois_da_devolucao_conta_como_cobertura_integral()
        {
            // o cliente devolveu e só depois pediu para cancelar a proteção: ela cobriu tudo
            var apuracao = Apurar(cancelamento: Retirada.AddDays(5));

            Assert.True(apuracao.CobriuOContratoInteiro);
            Assert.Equal(3m, apuracao.Diarias);
        }

        [Fact]
        public void Apuracao_sem_periodo_ou_sem_diaria_e_recusada()
        {
            Assert.Throws<DomainException>(() => Apurar(diariasCobradas: 0));
            Assert.Throws<DomainException>(() => ApuracaoDeProtecao.Calcular(
                Retirada, Retirada.AddDays(3), Retirada, null, valorDiariaContratada: 0m, 3));
        }

        // ======================= a linha que a apuração escreve =======================

        [Fact]
        public void Apuracao_escreve_uma_linha_por_protecao()
        {
            var (locacao, periodo) = ContratoComProtecao();

            var total = locacao.ApurarProtecoes(periodo);

            var linha = locacao.Fechamento!.Linhas.Single(l => l.Tipo == TipoLinhaFechamento.Protecao);
            Assert.Equal(3m, linha.Quantidade);
            Assert.Equal(40m, linha.ValorUnitario);
            Assert.Equal(120m, linha.Total);
            Assert.Contains("todo o contrato", linha.BaseCalculo);
            Assert.Equal(120m, total);
        }

        [Fact]
        public void Contrato_sem_protecao_nao_escreve_linha_nenhuma()
        {
            // não há linha zerada aqui, ao contrário do km: quem não contratou proteção não precisa
            // ver "proteção: R$ 0,00" no extrato — nunca houve o que apurar
            var (locacao, periodo) = ContratoComProtecao(comProtecao: false);

            var total = locacao.ApurarProtecoes(periodo);

            Assert.Equal(0m, total);
            Assert.DoesNotContain(locacao.Fechamento!.Linhas, l => l.Tipo == TipoLinhaFechamento.Protecao);
        }

        [Fact]
        public void Protecao_cancelada_depois_da_devolucao_ainda_cobra_o_contrato_inteiro()
        {
            // `Cancelar()` carimba o instante atual, sem porta para datar para trás — então cancelar
            // um contrato já devolvido não devolve dinheiro nenhum ao cliente, e é o certo: ele
            // esteve coberto do começo ao fim
            var (locacao, periodo) = ContratoComProtecao();
            locacao.CancelarSeguro(locacao.Seguros.Single().IdLocacaoSeguro);

            var total = locacao.ApurarProtecoes(periodo);

            var linha = locacao.Fechamento!.Linhas.Single(l => l.Tipo == TipoLinhaFechamento.Protecao);
            Assert.Contains("todo o contrato", linha.BaseCalculo);
            Assert.Equal(120m, total);
        }

        [Fact]
        public void Apurar_as_protecoes_duas_vezes_e_recusado()
        {
            var (locacao, periodo) = ContratoComProtecao();
            locacao.ApurarProtecoes(periodo);

            Assert.Throws<DomainException>(() => locacao.ApurarProtecoes(periodo));
        }

        /// <summary>
        /// Contrato de 3 diárias devolvido, com a conta aberta, o período apurado e — por padrão —
        /// uma proteção de R$ 40,00 a diária contratada junto com o contrato.
        /// </summary>
        private static (Locacao locacao, ApuracaoDePeriodo periodo) ContratoComProtecao(bool comProtecao = true)
        {
            var locacao = Fabrica.Locacao(
                dataInicio: Retirada,
                dataFimPrevista: Retirada.AddDays(3));

            if (comProtecao)
            {
                // ainda em `Criada`: a proteção vendida no balcão cobre desde a retirada
                Fabrica.ContratarSeguro(locacao, idSeguro: 3, valorDiaria: DiariaDaProtecao, franquia: 1500m);
                Fabrica.DefinirId(locacao.Seguros.Single(), 7);
            }

            Fabrica.Devolver(Fabrica.Retirar(locacao), dataFimReal: Retirada.AddDays(3));
            locacao.AbrirFechamento(1);

            var filial = Fabrica.Filial();
            Fabrica.DefinirId(filial, 1);

            return (locacao, locacao.ApurarPeriodo(filial));
        }
    }
}
