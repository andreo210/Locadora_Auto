using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Tests.Fabricas;
using Xunit;

namespace Locadora_Auto.Tests.Dominio
{
    /// <summary>
    /// RN-08 a RN-11: o que a rodagem custa. A franquia é km <b>por diária cobrada</b>, não um teto
    /// fixo do contrato — então devolver antes encolhe a franquia junto com a conta, e é por isso
    /// que a quilometragem não se apura antes do período.
    /// </summary>
    public class ApuracaoDeQuilometragemTests
    {
        // ======================= doc 07 §10, critérios de aceite =======================

        [Fact]
        public void Km_livre_nao_cobra_excedente()
        {
            // RN-08: LimiteKm nulo é o plano mais vendido no varejo. Rodou 1.800 km e não deve nada
            var apuracao = ApuracaoDeQuilometragem.Calcular(
                kmInicial: 15_000, kmFinal: 16_800, limiteKm: null, valorKmExcedente: null, diariasCobradas: 3);

            Assert.True(apuracao.KmLivre);
            Assert.Equal(1_800, apuracao.KmRodados);
            Assert.Equal(0, apuracao.KmExcedentes);
            Assert.Equal(0m, apuracao.Total);
        }

        [Fact]
        public void Km_controlado_cobra_o_que_passou_da_franquia()
        {
            var apuracao = ApuracaoDeQuilometragem.Calcular(
                kmInicial: 15_000, kmFinal: 15_750, limiteKm: 200, valorKmExcedente: 1.20m, diariasCobradas: 3);

            Assert.Equal(600, apuracao.FranquiaKm);
            Assert.Equal(750, apuracao.KmRodados);
            Assert.Equal(150, apuracao.KmExcedentes);
            Assert.Equal(180m, apuracao.Total);
        }

        // ======================= bordas do cálculo =======================

        [Fact]
        public void Dentro_da_franquia_nao_cobra_nada()
        {
            var apuracao = ApuracaoDeQuilometragem.Calcular(15_000, 15_500, 200, 1.20m, 3);

            Assert.Equal(0, apuracao.KmExcedentes);
            Assert.Equal(0m, apuracao.Total);
        }

        [Fact]
        public void Rodar_menos_que_a_franquia_nao_gera_credito()
        {
            // o excedente nunca é negativo: km não usado não vira dinheiro de volta
            var apuracao = ApuracaoDeQuilometragem.Calcular(15_000, 15_010, 200, 1.20m, 3);

            Assert.Equal(0, apuracao.KmExcedentes);
        }

        [Fact]
        public void Devolucao_antecipada_encolhe_a_franquia()
        {
            // mesma rodagem, uma diária a menos cobrada: a franquia cai de 600 para 400 km, e o
            // que era 150 km de excedente vira 350
            var tresDiarias = ApuracaoDeQuilometragem.Calcular(15_000, 15_750, 200, 1.20m, 3);
            var duasDiarias = ApuracaoDeQuilometragem.Calcular(15_000, 15_750, 200, 1.20m, 2);

            Assert.Equal(600, tresDiarias.FranquiaKm);
            Assert.Equal(400, duasDiarias.FranquiaKm);
            Assert.Equal(350, duasDiarias.KmExcedentes);
            Assert.Equal(420m, duasDiarias.Total);
        }

        [Fact]
        public void Hodometro_menor_na_devolucao_bloqueia()
        {
            // doc 07 §4: adulteração ou erro de digitação. Não há resposta segura — cobrar zero
            // esconderia a adulteração e cobrar o módulo inventaria rodagem
            Assert.Throws<DomainException>(
                () => ApuracaoDeQuilometragem.Calcular(15_000, 14_900, 200, 1.20m, 3));
        }

        [Fact]
        public void Categoria_com_limite_e_sem_valor_de_km_bloqueia()
        {
            Assert.Throws<DomainException>(
                () => ApuracaoDeQuilometragem.Calcular(15_000, 15_750, 200, null, 3));
        }

        [Fact]
        public void Categoria_com_limite_zerado_bloqueia()
        {
            // `CategoriaVeiculo.Criar` recusa limite não positivo, então um zero aqui só pode ser
            // dado velho — e tratá-lo como franquia zero cobraria o contrato inteiro por km
            Assert.Throws<DomainException>(
                () => ApuracaoDeQuilometragem.Calcular(15_000, 15_750, 0, 1.20m, 3));
        }

        [Fact]
        public void Sem_periodo_apurado_nao_ha_franquia_a_calcular()
        {
            Assert.Throws<DomainException>(
                () => ApuracaoDeQuilometragem.Calcular(15_000, 15_750, 200, 1.20m, diariasCobradas: 0));
        }

        // ======================= a linha que a apuração escreve =======================

        [Fact]
        public void Apuracao_escreve_a_linha_de_km_excedente()
        {
            var cenario = Cenario(kmRetirada: 15_000, kmDevolucao: 15_750);

            var apuracao = cenario.Apurar();

            var linha = cenario.Locacao.Fechamento!.Linhas
                .Single(l => l.Tipo == TipoLinhaFechamento.KmExcedente);

            Assert.Equal(150m, linha.Quantidade);
            Assert.Equal(1.20m, linha.ValorUnitario);
            Assert.Equal(180m, linha.Total);
            Assert.Contains("franquia de 600 km", linha.BaseCalculo);
            Assert.Equal(180m, apuracao.Total);
        }

        [Fact]
        public void A_linha_e_escrita_mesmo_valendo_zero()
        {
            // doc 07 §10: a linha zerada diz ao cliente que a quilometragem foi apurada e não gerou
            // cobrança — o que a ausência dela não diz
            var cenario = Cenario(kmRetirada: 15_000, kmDevolucao: 15_100);

            cenario.Apurar();

            var linha = cenario.Locacao.Fechamento!.Linhas
                .Single(l => l.Tipo == TipoLinhaFechamento.KmExcedente);

            Assert.Equal(0m, linha.Total);
            Assert.Contains("dentro da franquia", linha.BaseCalculo);
        }

        [Fact]
        public void O_hodometro_vem_da_vistoria_e_o_contrato_guarda_o_mesmo_numero()
        {
            // RN-11: a medição que sustenta a cobrança é a da vistoria, feita com o carro à frente
            // de quem assina. Até o A11, `RegistrarDevolucao` recebia o hodômetro por fora e os
            // dois podiam divergir sem nada avisar; hoje há uma fonte só
            var cenario = Cenario(kmRetirada: 15_000, kmDevolucao: 15_750);

            var apuracao = cenario.Apurar();

            var daVistoria = cenario.Locacao.Vistorias
                .Single(v => v.Tipo == TipoVistoria.Devolucao).KmVeiculo;

            Assert.Equal(15_750, daVistoria);
            Assert.Equal(daVistoria, cenario.Locacao.KmFinal);
            Assert.Equal(750, apuracao.KmRodados);
            Assert.Equal(150, apuracao.KmExcedentes);
        }

        [Fact]
        public void Apurar_a_quilometragem_duas_vezes_e_recusado()
        {
            var cenario = Cenario();
            cenario.Apurar();

            Assert.Throws<DomainException>(() => cenario.Apurar());
        }

        [Fact]
        public void Apurar_com_o_veiculo_ou_a_categoria_errados_e_recusado()
        {
            var cenario = Cenario();

            var outroVeiculo = Fabrica.Veiculo(placa: "ZZZ9Z99");
            Fabrica.DefinirId(outroVeiculo, 77);

            var outraCategoria = Fabrica.Categoria("SUV");
            Fabrica.DefinirId(outraCategoria, 88);

            Assert.Throws<InvalidOperationException>(
                () => cenario.Locacao.ApurarQuilometragem(outroVeiculo, cenario.Categoria, cenario.Periodo));
            Assert.Throws<InvalidOperationException>(
                () => cenario.Locacao.ApurarQuilometragem(cenario.Veiculo, outraCategoria, cenario.Periodo));
        }

        // ======================= montagem =======================

        private sealed record CenarioDeRodagem(
            Locacao Locacao, Veiculo Veiculo, CategoriaVeiculo Categoria, ApuracaoDePeriodo Periodo)
        {
            public ApuracaoDeQuilometragem Apurar()
                => Locacao.ApurarQuilometragem(Veiculo, Categoria, Periodo);
        }

        /// <summary>
        /// Contrato de 3 diárias devolvido, com a conta aberta e o período já apurado — que é o
        /// pré-requisito da franquia.
        /// </summary>
        private static CenarioDeRodagem Cenario(
            int kmRetirada = 15_000,
            int kmDevolucao = 15_750)
        {
            var inicio = new DateTime(2026, 3, 10, 9, 0, 0, DateTimeKind.Utc);

            var categoria = Fabrica.Categoria(valorKmExcedente: 1.20m);
            Fabrica.DefinirId(categoria, 1);

            var veiculo = Fabrica.Veiculo(idCategoria: categoria.Id);

            var locacao = Fabrica.Locacao(
                veiculo: veiculo,
                dataInicio: inicio,
                dataFimPrevista: inicio.AddDays(3),
                kmInicial: kmRetirada);

            locacao.RegistrarVistoria(1, TipoVistoria.Retirada, NivelCombustivel.Cheio, kmRetirada, null);
            locacao.RegistrarVistoria(1, TipoVistoria.Devolucao, NivelCombustivel.Cheio, kmDevolucao, null);
            locacao.RegistrarDevolucao(inicio.AddDays(3), 1);

            locacao.AbrirFechamento(1);

            var filial = Fabrica.Filial();
            Fabrica.DefinirId(filial, 1);
            var periodo = locacao.ApurarPeriodo(filial);

            return new CenarioDeRodagem(locacao, veiculo, categoria, periodo);
        }
    }
}
