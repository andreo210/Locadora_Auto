using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Tests.Fabricas;
using Xunit;

namespace Locadora_Auto.Tests.Dominio
{
    /// <summary>
    /// RN-13 a RN-16: reposição do tanque no regime full-to-full. É a linha que mais gera atrito no
    /// balcão, e a única do fechamento que <b>não bloqueia quando falta cadastro</b>: sem tanque ou
    /// sem preço do litro, a apuração não cobra e diz por quê na própria linha.
    /// </summary>
    public class ApuracaoDeCombustivelTests
    {
        private const decimal Tanque = 48m;
        private const decimal PrecoLitro = 6.20m;
        private const decimal TaxaServico = 40m;

        private static ApuracaoDeCombustivel Apurar(
            NivelCombustivel retirada,
            NivelCombustivel devolucao,
            decimal? tanque = Tanque,
            decimal precoLitro = PrecoLitro,
            decimal taxaServico = TaxaServico)
            => ApuracaoDeCombustivel.Calcular(retirada, devolucao, tanque, precoLitro, taxaServico);

        // ======================= doc 07 §10, critérios de aceite =======================

        [Fact]
        public void Combustivel_e_cobrado_pela_diferenca_de_nivel()
        {
            // tanque de 48 L, Cheio → Meio: meio tanque são 24 litros
            var apuracao = Apurar(NivelCombustivel.Cheio, NivelCombustivel.Meio);

            Assert.Equal(SituacaoDoCombustivel.Cobravel, apuracao.Situacao);
            Assert.Equal(24, apuracao.LitrosFaltantes);
            Assert.Equal(148.80m, apuracao.TotalDoCombustivel);
            Assert.Equal(40m, apuracao.TotalDaTaxa);
            Assert.Equal(188.80m, apuracao.Total);
        }

        [Fact]
        public void Devolver_com_mais_combustivel_nao_gera_credito()
        {
            // RN-16: prática consolidada de mercado, e precisa estar no contrato para não virar
            // reclamação
            var apuracao = Apurar(NivelCombustivel.Meio, NivelCombustivel.Cheio);

            Assert.Equal(SituacaoDoCombustivel.SemDiferenca, apuracao.Situacao);
            Assert.Equal(0m, apuracao.Total);
            Assert.Equal(0, apuracao.LitrosFaltantes);
        }

        // ======================= bordas do cálculo =======================

        [Fact]
        public void Devolver_no_mesmo_nivel_nao_cobra_nada()
        {
            var apuracao = Apurar(NivelCombustivel.TresQuartos, NivelCombustivel.TresQuartos);

            Assert.Equal(SituacaoDoCombustivel.SemDiferenca, apuracao.Situacao);
            Assert.Equal(0m, apuracao.Total);
        }

        [Fact]
        public void Litros_faltantes_sao_arredondados_para_cima()
        {
            // RN-14: um quarto de um tanque de 45 L são 11,25 L — e o posto não vende fração de
            // litro, então quem abastece paga 12
            var apuracao = Apurar(NivelCombustivel.Cheio, NivelCombustivel.TresQuartos, tanque: 45m);

            Assert.Equal(12, apuracao.LitrosFaltantes);
        }

        [Theory]
        [InlineData(NivelCombustivel.Vazio, 0)]
        [InlineData(NivelCombustivel.UmQuarto, 0.25)]
        [InlineData(NivelCombustivel.Meio, 0.5)]
        [InlineData(NivelCombustivel.TresQuartos, 0.75)]
        [InlineData(NivelCombustivel.Cheio, 1)]
        public void Cada_ponto_do_ponteiro_vale_uma_fracao_do_tanque(NivelCombustivel nivel, double fracao)
        {
            Assert.Equal((decimal)fracao, ApuracaoDeCombustivel.FracaoDe(nivel));
        }

        [Fact]
        public void Tanque_nao_cadastrado_nao_cobra_e_se_explica()
        {
            // doc 07 §4: melhor perder a cobrança que emitir número inventado. É o estado da frota
            // cadastrada antes de o campo existir, então vai aparecer muito até a tela entrar
            var apuracao = Apurar(NivelCombustivel.Cheio, NivelCombustivel.Vazio, tanque: null);

            Assert.Equal(SituacaoDoCombustivel.TanqueNaoCadastrado, apuracao.Situacao);
            Assert.False(apuracao.Cobravel);
            Assert.Equal(0m, apuracao.Total);
            Assert.Contains("não tem capacidade de tanque cadastrada", apuracao.BaseCalculoDoCombustivel());
        }

        [Fact]
        public void Preco_do_litro_zerado_nao_cobra_mas_declara_os_litros()
        {
            // o zero da filial significa "ninguém configurou", não "de graça" — e o extrato precisa
            // dizer quantos litros deixaram de ser cobrados
            var apuracao = Apurar(NivelCombustivel.Cheio, NivelCombustivel.Meio, precoLitro: 0m);

            Assert.Equal(SituacaoDoCombustivel.PrecoNaoConfigurado, apuracao.Situacao);
            Assert.Equal(24, apuracao.LitrosFaltantes);
            Assert.Equal(0m, apuracao.Total);
            Assert.Contains("não tem preço do litro configurado", apuracao.BaseCalculoDoCombustivel());
        }

        [Fact]
        public void Taxa_de_servico_so_entra_quando_ha_litro_a_repor()
        {
            // RN-15: a taxa é do serviço de abastecer. Sem abastecimento não há serviço
            Assert.Equal(0m, Apurar(NivelCombustivel.Cheio, NivelCombustivel.Cheio).TotalDaTaxa);
            Assert.Equal(0m, Apurar(NivelCombustivel.Cheio, NivelCombustivel.Meio, tanque: null).TotalDaTaxa);
            Assert.Equal(40m, Apurar(NivelCombustivel.Cheio, NivelCombustivel.Meio).TotalDaTaxa);
        }

        [Fact]
        public void Preco_ou_taxa_negativos_sao_recusados()
        {
            Assert.Throws<DomainException>(
                () => Apurar(NivelCombustivel.Cheio, NivelCombustivel.Meio, precoLitro: -1m));
            Assert.Throws<DomainException>(
                () => Apurar(NivelCombustivel.Cheio, NivelCombustivel.Meio, taxaServico: -1m));
        }

        // ======================= as linhas que a apuração escreve =======================

        [Fact]
        public void Apuracao_escreve_combustivel_e_taxa_em_linhas_separadas()
        {
            // litro é insumo e taxa é serviço: são coisas diferentes na conta do cliente, e o
            // indicador de receita acessória do doc 07 §12 só fecha se puderem ser contadas à parte
            var (locacao, veiculo, filial) = Cenario();

            locacao.ApurarCombustivel(veiculo, filial);

            var linhas = locacao.Fechamento!.Linhas;
            var combustivel = linhas.Single(l => l.Tipo == TipoLinhaFechamento.Combustivel);
            var taxa = linhas.Single(l => l.Tipo == TipoLinhaFechamento.TaxaServicoAbastecimento);

            Assert.Equal(24m, combustivel.Quantidade);
            Assert.Equal(6.20m, combustivel.ValorUnitario);
            Assert.Equal(148.80m, combustivel.Total);
            Assert.Equal(40m, taxa.Total);
            Assert.Contains("24 L a repor", combustivel.BaseCalculo);
        }

        [Fact]
        public void Sem_diferenca_escreve_a_linha_zerada_e_nenhuma_taxa()
        {
            var (locacao, veiculo, filial) = Cenario(nivelDevolucao: NivelCombustivel.Cheio);

            locacao.ApurarCombustivel(veiculo, filial);

            var linha = Assert.Single(locacao.Fechamento!.Linhas);
            Assert.Equal(TipoLinhaFechamento.Combustivel, linha.Tipo);
            Assert.Equal(0m, linha.Total);
            Assert.Contains("sem reposição e sem crédito", linha.BaseCalculo);
        }

        [Fact]
        public void Apurar_o_combustivel_duas_vezes_e_recusado()
        {
            var (locacao, veiculo, filial) = Cenario();
            locacao.ApurarCombustivel(veiculo, filial);

            Assert.Throws<DomainException>(() => locacao.ApurarCombustivel(veiculo, filial));
        }

        [Fact]
        public void Apurar_com_a_filial_de_retirada_no_lugar_da_de_devolucao_e_recusado()
        {
            // quem paga o posto é a praça que recebeu o carro, e é a política dela que vale
            var (locacao, veiculo, _) = Cenario(filialDevolucao: 3);

            var filialDeRetirada = Fabrica.Filial();
            Fabrica.DefinirId(filialDeRetirada, 1);

            Assert.Throws<InvalidOperationException>(
                () => locacao.ApurarCombustivel(veiculo, filialDeRetirada));
        }

        /// <summary>
        /// Contrato devolvido com a conta aberta, tanque de 48 L, e a filial de devolução com preço
        /// de R$ 6,20 o litro e taxa de serviço de R$ 40,00.
        /// </summary>
        private static (Locacao locacao, Veiculo veiculo, Filial filialDevolucao) Cenario(
            NivelCombustivel nivelRetirada = NivelCombustivel.Cheio,
            NivelCombustivel nivelDevolucao = NivelCombustivel.Meio,
            decimal? tanque = Tanque,
            int filialDevolucao = 1)
        {
            var inicio = new DateTime(2026, 3, 10, 9, 0, 0, DateTimeKind.Utc);

            var veiculo = Fabrica.Veiculo();
            veiculo.DefinirCapacidadeTanque(tanque);

            var locacao = Fabrica.Locacao(
                veiculo: veiculo,
                dataInicio: inicio,
                dataFimPrevista: inicio.AddDays(3));

            locacao.RegistrarVistoria(1, TipoVistoria.Retirada, nivelRetirada, 15_000, null);
            locacao.RegistrarVistoria(1, TipoVistoria.Devolucao, nivelDevolucao, 15_400, null);
            locacao.RegistrarDevolucao(inicio.AddDays(3), 15_400, filialDevolucao);

            locacao.AbrirFechamento(1);

            var filial = Fabrica.Filial();
            Fabrica.DefinirId(filial, filialDevolucao);
            filial.DefinirParametrosFinanceiros(
                precoLitroCombustivel: PrecoLitro,
                taxaServicoAbastecimento: TaxaServico);

            return (locacao, veiculo, filial);
        }
    }
}
