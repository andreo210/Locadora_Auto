using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Tests.Fabricas;
using Xunit;

namespace Locadora_Auto.Tests.Dominio
{
    /// <summary>
    /// RN-27 a RN-32: a composição da conta e a resolução da caução — o ponto em que as oito
    /// apurações de linha viram um número, o contrato vai a <c>Fechada</c> e a garantia é resolvida.
    ///
    /// É aqui que os dois trilhos que o A4 deixou correndo em paralelo se juntam:
    /// <c>Locacao.ValorFinal</c> deixa de ser um número informado e passa a ser o saldo apurado.
    /// </summary>
    public class FechamentoCompletoTests
    {
        private static readonly DateTime Retirada = new(2026, 3, 10, 9, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime Devolucao = Retirada.AddDays(3);

        // ======================= RN-28: só pagamento confirmado abate =======================

        [Fact]
        public void Apenas_pagamento_pago_abate_a_conta()
        {
            // dinheiro que ainda não caiu não quita contrato
            var cenario = Montar();
            cenario.AdicionarPagamento(200m, confirmar: true);
            cenario.AdicionarPagamento(300m, confirmar: false);
            cenario.Locacao.AbrirFechamento(1);

            var abatido = cenario.Locacao.ApurarPagamentos();

            Assert.Equal(200m, abatido);
            var linha = Assert.Single(cenario.Locacao.Fechamento!.Linhas
                .Where(l => l.Tipo == TipoLinhaFechamento.PagamentoAbatido));
            Assert.Equal(200m, linha.Total);
            Assert.Equal(NaturezaLinhaFechamento.Credito, linha.Natureza);
        }

        [Fact]
        public void Abater_os_pagamentos_duas_vezes_e_recusado()
        {
            var cenario = Montar();
            cenario.AdicionarPagamento(200m, confirmar: true);
            cenario.Locacao.AbrirFechamento(1);
            cenario.Locacao.ApurarPagamentos();

            Assert.Throws<DomainException>(() => cenario.Locacao.ApurarPagamentos());
        }

        // ======================= RN-29: o saldo vira o valor final =======================

        [Fact]
        public void Selar_leva_o_contrato_a_fechada_e_grava_o_saldo_apurado()
        {
            var cenario = Montar();
            var apuracao = cenario.Apurar();

            Assert.Equal(StatusLocacao.Fechada, cenario.Locacao.Status);
            Assert.Equal(apuracao.Saldo, cenario.Locacao.ValorFinal);
        }

        [Fact]
        public void Saldo_negativo_e_gravado_negativo_e_nao_trunca()
        {
            // o cliente pagou mais do que a conta deu: truncar para zero seria a casa ficando com
            // dinheiro que não é dela
            var cenario = Montar();
            cenario.AdicionarPagamento(5_000m, confirmar: true);

            var apuracao = cenario.Apurar();

            Assert.True(apuracao.Saldo < 0);
            Assert.Equal(apuracao.Saldo, cenario.Locacao.ValorFinal);
            Assert.Equal(-apuracao.Saldo, apuracao.CreditoADevolver);
            Assert.Equal(0m, apuracao.SaldoResidual);
        }

        [Fact]
        public void Contrato_com_apuracao_nao_fecha_por_valor_informado()
        {
            // senão o `ValorFinal` e o saldo das linhas passariam a discordar sem ninguém notar
            var cenario = Montar();
            cenario.Locacao.AbrirFechamento(1);

            Assert.Throws<InvalidOperationException>(() => cenario.Locacao.Fechar(1_000m));
        }

        // ======================= RN-30: a caução, depois do saldo =======================

        [Fact]
        public void Caucao_cobre_o_saldo_e_o_restante_volta()
        {
            // doc 07 §10: caução bloqueada de R$ 1.500 contra saldo de R$ 940 — consome R$ 940,
            // devolve R$ 560, e a caução fica em `Utilizada`
            var cenario = Montar(caucao: 1_500m);
            cenario.AjustarSaldoPara(940m);

            var apuracao = cenario.Apurar();
            var caucao = cenario.Locacao.Caucoes.Single();

            Assert.Equal(940m, apuracao.Saldo);
            Assert.Equal(940m, apuracao.CaucaoConsumida);
            Assert.Equal(0m, apuracao.SaldoResidual);

            Assert.Equal(1_500m, caucao.Valor);
            Assert.Equal(940m, caucao.ValorConsumido);
            Assert.Equal(560m, caucao.ValorDisponivel);
            Assert.Equal(Caucao.StatusCaucao.Utilizada, caucao.Status);
        }

        [Fact]
        public void Saldo_maior_que_a_caucao_gera_cobranca_residual()
        {
            // doc 07 §10: caução de R$ 1.500 contra saldo de R$ 2.300 — consome tudo e sobram
            // R$ 800 para cobrar
            var cenario = Montar(caucao: 1_500m);
            cenario.AjustarSaldoPara(2_300m);

            var apuracao = cenario.Apurar();
            var caucao = cenario.Locacao.Caucoes.Single();

            Assert.Equal(1_500m, apuracao.CaucaoConsumida);
            Assert.Equal(800m, apuracao.SaldoResidual);
            Assert.Equal(0m, caucao.ValorDisponivel);
            Assert.Equal(Caucao.StatusCaucao.Utilizada, caucao.Status);
        }

        [Fact]
        public void Saldo_residual_deixa_o_contrato_com_saldo_residual()
        {
            var cenario = Montar(caucao: 1_500m);
            cenario.AjustarSaldoPara(2_300m);
            cenario.Apurar();

            cenario.Locacao.LiquidarSaldo();

            Assert.Equal(StatusLocacao.ComSaldoResidual, cenario.Locacao.Status);
            Assert.Equal(800m, cenario.Locacao.SaldoEmAberto());
        }

        [Fact]
        public void Saldo_quitado_pela_caucao_finaliza_o_contrato()
        {
            var cenario = Montar(caucao: 1_500m);
            cenario.AjustarSaldoPara(940m);
            cenario.Apurar();

            cenario.Locacao.LiquidarSaldo();

            Assert.Equal(StatusLocacao.Finalizada, cenario.Locacao.Status);
            Assert.Equal(0m, cenario.Locacao.SaldoEmAberto());
        }

        [Fact]
        public void Saldo_negativo_nao_consome_caucao_e_ela_volta_inteira()
        {
            // RN-29: reter qualquer parte da garantia de quem já não deve nada seria indefensável
            var cenario = Montar(caucao: 1_500m);
            cenario.AdicionarPagamento(5_000m, confirmar: true);

            var apuracao = cenario.Apurar();
            var caucao = cenario.Locacao.Caucoes.Single();

            Assert.Equal(0m, apuracao.CaucaoConsumida);
            Assert.Equal(1_500m, caucao.ValorDisponivel);
            Assert.Equal(Caucao.StatusCaucao.Devolvida, caucao.Status);
        }

        [Fact]
        public void Resolver_a_caucao_antes_de_selar_e_recusado()
        {
            // doc 07 §6, transição proibida: liberar caução antes de `Fechada` é abrir mão da
            // garantia justamente no momento em que ela serve para alguma coisa
            var cenario = Montar(caucao: 1_500m);
            cenario.Locacao.AbrirFechamento(1);

            Assert.Throws<DomainException>(() => cenario.Locacao.ResolverCaucao());
        }

        // ======================= RN-32: idempotência =======================

        [Fact]
        public void Apurar_de_novo_devolve_a_mesma_conta_sem_mexer_em_nada()
        {
            // doc 07 §10: nenhuma linha nova, a caução não é consumida de novo, o total continua
            var cenario = Montar(caucao: 1_500m);
            cenario.AjustarSaldoPara(940m);

            var primeira = cenario.Apurar();
            var linhas = primeira.Fechamento.Linhas.Count;
            var saldo = primeira.Saldo;

            var segunda = cenario.Apurar();

            Assert.True(segunda.JaEstavaApurado);
            Assert.Same(primeira.Fechamento, segunda.Fechamento);
            Assert.Equal(linhas, segunda.Fechamento.Linhas.Count);
            Assert.Equal(saldo, segunda.Saldo);
            Assert.Equal(940m, segunda.CaucaoConsumida);
            Assert.Equal(940m, cenario.Locacao.Caucoes.Single().ValorConsumido);
        }

        [Fact]
        public void A_primeira_apuracao_nao_se_diz_ja_apurada()
        {
            var cenario = Montar();

            Assert.False(cenario.Apurar().JaEstavaApurado);
        }

        // ======================= a conta completa =======================

        [Fact]
        public void A_apuracao_completa_escreve_as_linhas_de_todas_as_regras()
        {
            var cenario = Montar(caucao: 1_500m, comProtecao: true, comAcessorio: true);
            cenario.AdicionarPagamento(200m, confirmar: true);

            var apuracao = cenario.Apurar();
            var tipos = apuracao.Fechamento.Linhas.Select(l => l.Tipo).ToList();

            Assert.Contains(TipoLinhaFechamento.Diaria, tipos);
            Assert.Contains(TipoLinhaFechamento.KmExcedente, tipos);
            Assert.Contains(TipoLinhaFechamento.Combustivel, tipos);
            Assert.Contains(TipoLinhaFechamento.Protecao, tipos);
            Assert.Contains(TipoLinhaFechamento.Acessorio, tipos);
            Assert.Contains(TipoLinhaFechamento.PagamentoAbatido, tipos);

            // o saldo é a soma das linhas, e o contrato guarda exatamente ele
            Assert.Equal(apuracao.Fechamento.TotalDebitos - apuracao.Fechamento.TotalCreditos,
                         cenario.Locacao.ValorFinal);
            Assert.True(apuracao.Fechamento.Selado);
        }

        [Fact]
        public void A_apuracao_completa_devolve_o_que_nao_cabe_no_saldo()
        {
            // avaria em análise e multa recusada por redundância: nada some em silêncio
            var cenario = Montar();
            cenario.RegistrarAvariaEmAnalise(900m);
            cenario.Locacao.AdicionarMulta(TipoMulta.Atraso, 150m);

            var apuracao = cenario.Apurar();

            Assert.NotNull(apuracao.Avarias);
            Assert.Equal(1, apuracao.Avarias!.AvariasEmAnalise);
            Assert.Equal(Devolucao.AddDays(30), apuracao.Avarias.PrazoDoPosContrato);
            Assert.Single(apuracao.MultasRecusadas);
        }

        // ======================= montagem =======================

        private sealed class Cenario
        {
            public required Locacao Locacao { get; init; }
            public required Veiculo Veiculo { get; init; }
            public required CategoriaVeiculo Categoria { get; init; }
            public required Filial Filial { get; init; }

            public ResultadoDaApuracao Apurar()
                => Locacao.ApurarFechamento(Veiculo, Categoria, Filial, Filial, idFuncionarioApuracao: 1);

            public void AdicionarPagamento(decimal valor, bool confirmar)
            {
                Locacao.AdicionarPagamento(valor, FormaPagamento.Pix);
                var pagamento = Locacao.Pagamentos.Last();
                Fabrica.DefinirId(pagamento, Locacao.Pagamentos.Count);

                if (confirmar)
                    Locacao.ConfirmarPagamento(pagamento.IdPagamento);
            }

            /// <summary>
            /// Põe o saldo no valor pedido lançando uma multa de trânsito pela diferença — é a
            /// linha mais simples da conta, e o que estes testes verificam é a composição, não de
            /// onde o número veio.
            /// </summary>
            public void AjustarSaldoPara(decimal saldo)
            {
                var jaCobrado = 3 * 150m;   // 3 diárias; o resto do cenário não gera cobrança
                Locacao.AdicionarMulta(TipoMulta.MultaTransito, saldo - jaCobrado);
            }

            public void RegistrarAvariaEmAnalise(decimal valor)
            {
                var vistoria = Locacao.Vistorias.Single(v => v.Tipo == TipoVistoria.Devolucao);
                vistoria.RegistrarDano($"avaria de {valor:0}", TipoDano.Risco, valor);
                vistoria.Danos.Last().ColocarEmAnalise();
            }
        }

        /// <summary>
        /// Contrato de 3 diárias devolvido no prazo, sem km excedente e com o tanque cheio nas duas
        /// pontas: a conta base é só R$ 450,00 de diárias, para o teste somar o que quiser em cima.
        /// </summary>
        private static Cenario Montar(
            decimal? caucao = null, bool comProtecao = false, bool comAcessorio = false)
        {
            var categoria = Fabrica.Categoria();
            Fabrica.DefinirId(categoria, 1);

            var veiculo = Fabrica.Veiculo(idCategoria: categoria.Id);
            veiculo.DefinirCapacidadeTanque(48m);

            var locacao = Fabrica.Locacao(
                veiculo: veiculo, dataInicio: Retirada, dataFimPrevista: Devolucao);

            if (comProtecao)
                Fabrica.ContratarSeguro(locacao, idSeguro: 3, valorDiaria: 40m, franquia: 2_000m);

            if (comAcessorio)
                locacao.AdicionarAdicional(idAdicional: 1, valorDiaria: 20m, quantidade: 2);

            if (caucao is { } valor)
            {
                locacao.RegistrarCaucao(valor);
                var registrada = locacao.Caucoes.Single();
                Fabrica.DefinirId(registrada, 4);
                locacao.BloquearCaucao(4);
            }

            locacao.RegistrarVistoria(1, TipoVistoria.Retirada, NivelCombustivel.Cheio, 15_000, null);
            locacao.RegistrarVistoria(1, TipoVistoria.Devolucao, NivelCombustivel.Cheio, 15_100, null);
            var vistoria = locacao.Vistorias.Single(v => v.Tipo == TipoVistoria.Devolucao);
            Fabrica.DefinirId(vistoria, 5);

            locacao.RegistrarDevolucao(Devolucao, 15_100, 1);

            var filial = Fabrica.Filial();
            Fabrica.DefinirId(filial, 1);

            return new Cenario
            {
                Locacao = locacao,
                Veiculo = veiculo,
                Categoria = categoria,
                Filial = filial
            };
        }
    }
}
