using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Tests.Fabricas;
using Xunit;

namespace Locadora_Auto.Tests.Dominio
{
    /// <summary>
    /// RN-27 a RN-34: a conta discriminada do contrato. Nada aqui calcula — quem apura período,
    /// km, combustível e taxas é o backlog A5–A10. O que estes testes fixam é a <b>forma</b> da
    /// conta, que é o que o A4 entrega: linha imutável, arredondamento por linha, crédito que não
    /// vira débito, e uma conta que, depois de selada, só cresce por correção assinada.
    ///
    /// Todos passam pela <see cref="Locacao"/>, e não pelas entidades direto, porque
    /// <c>FechamentoLocacao.Abrir</c> e <c>LinhaFechamento.Lancar</c> são <c>internal</c> pela
    /// convenção do agregado: conta solta, sem contrato, não é conta de ninguém.
    /// </summary>
    public class FechamentoLocacaoTests
    {
        // ======================= abertura (RN-32) =======================

        [Fact]
        public void Apuracao_so_abre_depois_da_devolucao()
        {
            // doc 07 §6: `Criada → Fechada` é transição proibida — apurar sem o carro de volta é
            // apurar km, combustível e avaria que ninguém mediu
            var locacao = Fabrica.Locacao();

            Assert.Throws<InvalidOperationException>(() => locacao.AbrirFechamento(idFuncionarioApuracao: 1));
        }

        [Fact]
        public void Apuracao_aberta_nasce_vazia_e_nao_selada()
        {
            var locacao = Fabrica.LocacaoDevolvida();

            var fechamento = locacao.AbrirFechamento(idFuncionarioApuracao: 7);

            Assert.False(fechamento.Selado);
            Assert.Empty(fechamento.Linhas);
            Assert.Equal(0m, fechamento.Saldo);
            Assert.Equal(7, fechamento.IdFuncionarioApuracao);
            Assert.Same(fechamento, locacao.Fechamento);
        }

        [Fact]
        public void Apuracao_exige_o_funcionario_responsavel()
        {
            var locacao = Fabrica.LocacaoDevolvida();

            Assert.Throws<DomainException>(() => locacao.AbrirFechamento(idFuncionarioApuracao: 0));
        }

        [Fact]
        public void Abrir_a_apuracao_duas_vezes_devolve_a_mesma_conta()
        {
            // RN-32: retentativa de rede no balcão não pode produzir duas contas para o mesmo
            // contrato. A garantia dura é o índice único em id_locacao; esta é a recusa amigável
            var locacao = Fabrica.LocacaoDevolvida();
            var primeira = locacao.AbrirFechamento(1);
            locacao.LancarNoFechamento(TipoLinhaFechamento.Diaria, "3 diárias de 24h", 3m, 150m);

            var segunda = locacao.AbrirFechamento(1);

            Assert.Same(primeira, segunda);
            Assert.Single(segunda.Linhas);
        }

        [Fact]
        public void Reabrir_apuracao_ja_selada_devolve_a_conta_selada_e_nao_uma_nova()
        {
            var locacao = ComContaSelada();

            var fechamento = locacao.AbrirFechamento(1);

            Assert.True(fechamento.Selado);
            Assert.Same(locacao.Fechamento, fechamento);
        }

        [Fact]
        public void Lancar_sem_ter_aberto_a_apuracao_e_recusado()
        {
            var locacao = Fabrica.LocacaoDevolvida();

            Assert.Throws<InvalidOperationException>(
                () => locacao.LancarNoFechamento(TipoLinhaFechamento.Diaria, "3 diárias", 3m, 150m));
            Assert.Throws<InvalidOperationException>(() => locacao.SelarFechamento());
        }

        // ======================= a linha (RN-31, RN-33) =======================

        [Fact]
        public void Linha_guarda_tipo_base_quantidade_e_unitario()
        {
            var locacao = Fabrica.LocacaoDevolvida();
            locacao.AbrirFechamento(1);

            var linha = locacao.LancarNoFechamento(
                TipoLinhaFechamento.KmExcedente,
                "franquia de 600 km sobre 3 diárias; rodados 750 km",
                150m,
                1.20m);

            Assert.Equal(TipoLinhaFechamento.KmExcedente, linha.Tipo);
            Assert.Equal("franquia de 600 km sobre 3 diárias; rodados 750 km", linha.BaseCalculo);
            Assert.Equal(150m, linha.Quantidade);
            Assert.Equal(1.20m, linha.ValorUnitario);
            Assert.Equal(180m, linha.Total);
            Assert.False(linha.EhCorrecao);
        }

        [Fact]
        public void Linha_sem_base_de_calculo_e_recusada()
        {
            // doc 07 §9: "não faça em cenário nenhum ... cobrar linha sem documento de suporte"
            var locacao = Fabrica.LocacaoDevolvida();
            locacao.AbrirFechamento(1);

            Assert.Throws<DomainException>(
                () => locacao.LancarNoFechamento(TipoLinhaFechamento.Diaria, "   ", 3m, 150m));
        }

        [Fact]
        public void Linha_com_valor_negativo_e_recusada()
        {
            // o sinal mora no tipo, não no número: crédito é PagamentoAbatido, nunca débito com
            // menos na frente
            var locacao = Fabrica.LocacaoDevolvida();
            locacao.AbrirFechamento(1);

            Assert.Throws<DomainException>(
                () => locacao.LancarNoFechamento(TipoLinhaFechamento.Diaria, "3 diárias", -1m, 150m));
            Assert.Throws<DomainException>(
                () => locacao.LancarNoFechamento(TipoLinhaFechamento.Diaria, "3 diárias", 3m, -150m));
        }

        [Fact]
        public void Linha_zerada_e_valida()
        {
            // doc 07 §10, cenário "km livre não cobra excedente": a linha de R$ 0,00 diz ao cliente
            // que a quilometragem foi apurada e não gerou cobrança — vale mais que a ausência dela
            var locacao = Fabrica.LocacaoDevolvida();
            locacao.AbrirFechamento(1);

            var linha = locacao.LancarNoFechamento(
                TipoLinhaFechamento.KmExcedente, "categoria com km livre", 0m, 0m);

            Assert.Equal(0m, linha.Total);
        }

        [Fact]
        public void Arredondamento_e_away_from_zero_e_nao_bancario()
        {
            // 2,345 vai para 2,35. O ToEven padrão do .NET devolveria 2,34, e num extrato ao
            // consumidor a conta tem que fechar do jeito que qualquer um refaz na calculadora
            Assert.Equal(2.35m, LinhaFechamento.Arredondar(2.345m));
            Assert.Equal(2.34m, Math.Round(2.345m, 2, MidpointRounding.ToEven));
        }

        [Fact]
        public void Arredondamento_e_por_linha_e_nao_so_no_total()
        {
            // RN-33. Três linhas de 0,5 × 4,69 = 2,345 cada: arredondando por linha dá 3 × 2,35 =
            // 7,05. Somando cru primeiro daria 7,035 → 7,04, um centavo a menos que ninguém sabe
            // explicar. É a diferença que a RN existe para fixar.
            var locacao = Fabrica.LocacaoDevolvida();
            locacao.AbrirFechamento(1);

            for (var i = 0; i < 3; i++)
                locacao.LancarNoFechamento(TipoLinhaFechamento.Protecao, "proteção pró-rata", 0.5m, 4.69m);

            Assert.Equal(7.05m, locacao.Fechamento!.TotalDebitos);
            Assert.All(locacao.Fechamento.Linhas, l => Assert.Equal(2.35m, l.Total));
        }

        [Theory]
        [InlineData(TipoLinhaFechamento.Diaria, NaturezaLinhaFechamento.Debito)]
        [InlineData(TipoLinhaFechamento.Combustivel, NaturezaLinhaFechamento.Debito)]
        [InlineData(TipoLinhaFechamento.MultaTransito, NaturezaLinhaFechamento.Debito)]
        [InlineData(TipoLinhaFechamento.PagamentoAbatido, NaturezaLinhaFechamento.Credito)]
        [InlineData(TipoLinhaFechamento.Isencao, NaturezaLinhaFechamento.Credito)]
        public void Natureza_da_linha_sai_do_tipo(TipoLinhaFechamento tipo, NaturezaLinhaFechamento esperada)
        {
            Assert.Equal(esperada, LinhaFechamento.NaturezaDe(tipo));
        }

        // ======================= saldo (RN-27, RN-28, RN-29) =======================

        [Fact]
        public void Saldo_e_debitos_menos_creditos()
        {
            var locacao = Fabrica.LocacaoDevolvida();
            locacao.AbrirFechamento(1);

            locacao.LancarNoFechamento(TipoLinhaFechamento.Diaria, "3 diárias de 24h", 3m, 150m);
            locacao.LancarNoFechamento(TipoLinhaFechamento.KmExcedente, "150 km além da franquia", 150m, 1.20m);
            locacao.LancarNoFechamento(TipoLinhaFechamento.PagamentoAbatido, "sinal pago no ato", 1m, 200m);

            var fechamento = locacao.Fechamento!;
            Assert.Equal(630m, fechamento.TotalDebitos);
            Assert.Equal(200m, fechamento.TotalCreditos);
            Assert.Equal(430m, fechamento.Saldo);
        }

        [Fact]
        public void Saldo_negativo_nao_trunca_para_zero()
        {
            // RN-29: o cliente pagou mais do que a conta deu, e o que sobra é crédito a devolver.
            // Truncar seria a casa ficando com dinheiro que não é dela
            var locacao = Fabrica.LocacaoDevolvida();
            locacao.AbrirFechamento(1);

            locacao.LancarNoFechamento(TipoLinhaFechamento.Diaria, "1 diária de 24h", 1m, 150m);
            locacao.LancarNoFechamento(TipoLinhaFechamento.PagamentoAbatido, "3 diárias pagas na reserva", 1m, 450m);

            Assert.Equal(-300m, locacao.Fechamento!.Saldo);
        }

        [Fact]
        public void Isencao_abate_e_exige_autor_e_motivo()
        {
            // RN-34: é a fronteira entre cortesia registrada e receita que evaporou
            var locacao = Fabrica.LocacaoDevolvida();
            locacao.AbrirFechamento(1);
            locacao.LancarNoFechamento(TipoLinhaFechamento.LimpezaEspecial, "lavagem de motor", 1m, 120m);

            Assert.Throws<DomainException>(() => locacao.LancarNoFechamento(
                TipoLinhaFechamento.Isencao, "cortesia", 1m, 120m));

            Assert.Throws<DomainException>(() => locacao.LancarNoFechamento(
                TipoLinhaFechamento.Isencao, "cortesia", 1m, 120m, idFuncionarioLancamento: 9));

            var isencao = locacao.LancarNoFechamento(
                TipoLinhaFechamento.Isencao, "cortesia sobre a limpeza", 1m, 120m,
                idFuncionarioLancamento: 9, motivo: "cliente corporativo, alçada do gerente");

            Assert.Equal(9, isencao.IdFuncionarioLancamento);
            Assert.Equal(0m, locacao.Fechamento!.Saldo);
        }

        // ======================= selagem e correção (RN-31) =======================

        [Fact]
        public void Selar_devolve_o_saldo_e_fecha_a_conta_para_lancamento()
        {
            var locacao = Fabrica.LocacaoDevolvida();
            locacao.AbrirFechamento(1);
            locacao.LancarNoFechamento(TipoLinhaFechamento.Diaria, "3 diárias de 24h", 3m, 150m);

            var saldo = locacao.SelarFechamento();

            Assert.Equal(450m, saldo);
            Assert.True(locacao.Fechamento!.Selado);
            Assert.Throws<DomainException>(
                () => locacao.LancarNoFechamento(TipoLinhaFechamento.Combustivel, "24 litros", 24m, 6.20m));
        }

        [Fact]
        public void Conta_sem_linha_nenhuma_nao_pode_ser_selada()
        {
            // a RN-02 garante o mínimo de uma diária em qualquer contrato: fechamento vazio só
            // pode ser apuração que não rodou, e selá-lo criaria um contrato fechado em branco
            var locacao = Fabrica.LocacaoDevolvida();
            locacao.AbrirFechamento(1);

            Assert.Throws<DomainException>(() => locacao.SelarFechamento());
        }

        [Fact]
        public void Selar_duas_vezes_e_recusado()
        {
            var locacao = ComContaSelada();

            Assert.Throws<DomainException>(() => locacao.SelarFechamento());
        }

        [Fact]
        public void Correcao_so_existe_depois_da_selagem()
        {
            var locacao = Fabrica.LocacaoDevolvida();
            locacao.AbrirFechamento(1);
            locacao.LancarNoFechamento(TipoLinhaFechamento.Diaria, "3 diárias de 24h", 3m, 150m);

            Assert.Throws<DomainException>(() => locacao.CorrigirFechamento(
                TipoLinhaFechamento.Isencao, "estorno", 1m, 50m, 9, "erro de digitação"));
        }

        [Fact]
        public void Correcao_acrescenta_linha_e_nao_altera_as_existentes()
        {
            // RN-31: o extrato passa a mostrar a conta original e o ajuste lado a lado. Quem
            // contesta vê o que foi cobrado e o que foi corrigido; quem audita vê que nada sumiu
            var locacao = ComContaSelada();
            var fechamento = locacao.Fechamento!;
            var original = fechamento.Linhas.Single();

            var correcao = locacao.CorrigirFechamento(
                TipoLinhaFechamento.Isencao,
                "estorno de 1 diária cobrada a mais",
                1m,
                150m,
                idFuncionarioLancamento: 9,
                motivo: "diária lançada em duplicidade na apuração");

            Assert.Equal(2, fechamento.Linhas.Count);
            Assert.True(correcao.EhCorrecao);
            Assert.Equal(9, correcao.IdFuncionarioLancamento);
            Assert.Equal("diária lançada em duplicidade na apuração", correcao.Motivo);

            // a linha original continua exatamente como foi apurada
            Assert.Equal(450m, original.Total);
            Assert.False(original.EhCorrecao);

            // e o saldo anda com a correção
            Assert.Equal(300m, fechamento.Saldo);
        }

        [Fact]
        public void Correcao_sem_autor_ou_sem_motivo_e_recusada()
        {
            var locacao = ComContaSelada();

            Assert.Throws<DomainException>(() => locacao.CorrigirFechamento(
                TipoLinhaFechamento.Avaria, "risco na porta", 1m, 300m, 0, "reavaliação da oficina"));

            Assert.Throws<DomainException>(() => locacao.CorrigirFechamento(
                TipoLinhaFechamento.Avaria, "risco na porta", 1m, 300m, 9, "  "));
        }

        [Fact]
        public void Correcao_de_debito_soma_no_saldo()
        {
            var locacao = ComContaSelada();

            locacao.CorrigirFechamento(
                TipoLinhaFechamento.Avaria,
                "risco na porta traseira, laudo da oficina",
                1m,
                300m,
                idFuncionarioLancamento: 9,
                motivo: "avaria aprovada depois do fechamento");

            Assert.Equal(750m, locacao.Fechamento!.Saldo);
        }

        /// <summary>Contrato devolvido, com uma diária apurada e a conta selada em R$ 450,00.</summary>
        private static Locacao ComContaSelada()
        {
            var locacao = Fabrica.LocacaoDevolvida();
            locacao.AbrirFechamento(1);
            locacao.LancarNoFechamento(TipoLinhaFechamento.Diaria, "3 diárias de 24h", 3m, 150m);
            locacao.SelarFechamento();

            return locacao;
        }
    }
}
