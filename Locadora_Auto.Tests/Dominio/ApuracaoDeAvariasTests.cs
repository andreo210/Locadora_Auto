using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Tests.Fabricas;
using Xunit;

namespace Locadora_Auto.Tests.Dominio
{
    /// <summary>
    /// RN-24 a RN-26: avarias e multas.
    ///
    /// A RN-25 é a que mais gera conflito no balcão — a franquia é teto da <b>soma</b>, não de cada
    /// avaria — e a RN-24 é a que mais gera caução retida: o que ainda está em análise não segura o
    /// fechamento nem o dinheiro do cliente, vira pendência com prazo declarado.
    /// </summary>
    public class ApuracaoDeAvariasTests
    {
        private static readonly DateTime Retirada = new(2026, 3, 10, 9, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime Devolucao = Retirada.AddDays(3);

        private const decimal Franquia = 2000m;

        // ======================= doc 07 §10, critérios de aceite =======================

        [Fact]
        public void Avaria_com_protecao_e_limitada_a_franquia()
        {
            // duas avarias de R$ 1.500 e R$ 1.800 somam R$ 3.300; com franquia de R$ 2.000, o
            // cliente paga R$ 2.000 — e não duas franquias
            var locacao = Contrato(comProtecao: true, aprovadas: new[] { 1500m, 1800m });

            var apuracao = locacao.ApurarAvarias();

            Assert.Equal(3300m, apuracao.TotalApurado);
            Assert.Equal(1300m, apuracao.AbatimentoPorProtecao);
            Assert.Equal(2000m, apuracao.TotalCobravel);
        }

        [Fact]
        public void Avaria_em_analise_nao_entra_no_fechamento()
        {
            var locacao = Contrato(emAnalise: new[] { 900m });

            var apuracao = locacao.ApurarAvarias();

            Assert.Equal(0m, apuracao.TotalApurado);
            Assert.True(apuracao.TemPendenciaDePosContrato);
            Assert.Equal(1, apuracao.AvariasEmAnalise);
            Assert.Equal(900m, apuracao.ValorEmAnalise);
            Assert.DoesNotContain(locacao.Fechamento!.Linhas, l => l.Tipo == TipoLinhaFechamento.Avaria);
        }

        // ======================= RN-24: o que entra e o que espera =======================

        [Fact]
        public void Pendencia_em_analise_ganha_prazo_de_trinta_dias_a_partir_da_devolucao()
        {
            // avaria "em apuração" por tempo indefinido é caução retida e cliente irritado
            var locacao = Contrato(emAnalise: new[] { 900m });

            var apuracao = locacao.ApurarAvarias();

            Assert.Equal(30, ApuracaoDeAvarias.PrazoPosContratoDias);
            Assert.Equal(Devolucao.AddDays(30), apuracao.PrazoDoPosContrato);
        }

        [Fact]
        public void Avaria_apenas_registrada_conta_como_pendencia()
        {
            // `Registrado` e `EmAnalise` são a mesma coisa para o cliente: avaria sem decisão
            var locacao = Contrato(registradas: new[] { 700m });

            var apuracao = locacao.ApurarAvarias();

            Assert.Equal(0m, apuracao.TotalApurado);
            Assert.Equal(1, apuracao.AvariasEmAnalise);
            Assert.Equal(700m, apuracao.ValorEmAnalise);
        }

        [Fact]
        public void Sem_avaria_pendente_nao_ha_prazo()
        {
            var locacao = Contrato(aprovadas: new[] { 500m });

            var apuracao = locacao.ApurarAvarias();

            Assert.False(apuracao.TemPendenciaDePosContrato);
            Assert.Null(apuracao.PrazoDoPosContrato);
        }

        [Fact]
        public void Avaria_isenta_ou_cancelada_nao_entra_nem_vira_pendencia()
        {
            // as duas já foram decididas, e a decisão foi não cobrar
            var locacao = Contrato(isentas: new[] { 800m }, canceladas: new[] { 600m });

            var apuracao = locacao.ApurarAvarias();

            Assert.Equal(0m, apuracao.TotalApurado);
            Assert.Equal(0, apuracao.AvariasEmAnalise);
            Assert.Null(apuracao.PrazoDoPosContrato);
        }

        [Fact]
        public void Avaria_cancelada_deixou_de_se_confundir_com_a_em_analise()
        {
            // `StatusDano.Cancelado` valia 6, o mesmo de `EmAnalise`: avaria descartada apareceria
            // como pendência do pós-contrato para sempre
            Assert.NotEqual((int)StatusDano.Cancelado, (int)StatusDano.EmAnalise);
        }

        // ======================= RN-25: a franquia é teto da soma =======================

        [Fact]
        public void Sem_protecao_a_avaria_e_cobrada_integralmente()
        {
            var locacao = Contrato(comProtecao: false, aprovadas: new[] { 1500m, 1800m });

            var apuracao = locacao.ApurarAvarias();

            Assert.False(apuracao.TemProtecao);
            Assert.Equal(0m, apuracao.AbatimentoPorProtecao);
            Assert.Equal(3300m, apuracao.TotalCobravel);
        }

        [Fact]
        public void Avaria_dentro_da_franquia_nao_gera_abatimento()
        {
            var locacao = Contrato(comProtecao: true, aprovadas: new[] { 500m });

            var apuracao = locacao.ApurarAvarias();

            Assert.Equal(0m, apuracao.AbatimentoPorProtecao);
            Assert.Equal(500m, apuracao.TotalCobravel);
            Assert.DoesNotContain(locacao.Fechamento!.Linhas,
                l => l.Tipo == TipoLinhaFechamento.AbatimentoPorProtecao);
        }

        [Fact]
        public void Protecao_cancelada_antes_da_devolucao_nao_da_franquia()
        {
            // quem cancelou a proteção deixou de ter o produto; a avaria é integral.
            // Datas vigentes porque `Cancelar()` carimba o instante atual: num contrato retroagido
            // o cancelamento cairia **depois** da devolução, que é o caso oposto
            var locacao = Contrato(
                comProtecao: true, aprovadas: new[] { 1500m, 1800m },
                cancelarProtecao: true, datasVigentes: true);

            var apuracao = locacao.ApurarAvarias();

            Assert.False(apuracao.TemProtecao);
            Assert.Equal(3300m, apuracao.TotalCobravel);
        }

        [Fact]
        public void Protecao_cancelada_depois_da_devolucao_ainda_da_a_franquia()
        {
            // o outro lado: o cliente devolveu coberto e só depois pediu para cancelar. Usar
            // `Ativo` no lugar da janela do A7 tiraria a franquia de quem tinha direito a ela
            var locacao = Contrato(comProtecao: true, aprovadas: new[] { 1500m, 1800m }, cancelarProtecao: true);

            var apuracao = locacao.ApurarAvarias();

            Assert.True(apuracao.TemProtecao);
            Assert.Equal(2000m, apuracao.TotalCobravel);
        }

        // ======================= as linhas que a apuração escreve =======================

        [Fact]
        public void Cada_avaria_sai_em_uma_linha_e_o_abatimento_em_outra()
        {
            // é assim que a cobrança se defende, e é onde a proteção mostra que se pagou
            var locacao = Contrato(comProtecao: true, aprovadas: new[] { 1500m, 1800m });

            locacao.ApurarAvarias();

            var linhas = locacao.Fechamento!.Linhas;
            Assert.Equal(2, linhas.Count(l => l.Tipo == TipoLinhaFechamento.Avaria));

            var abatimento = linhas.Single(l => l.Tipo == TipoLinhaFechamento.AbatimentoPorProtecao);
            Assert.Equal(NaturezaLinhaFechamento.Credito, abatimento.Natureza);
            Assert.Equal(1300m, abatimento.Total);

            // o saldo do fechamento passa a ser o que se cobra do cliente
            Assert.Equal(2000m, linhas.Where(l => l.Tipo == TipoLinhaFechamento.Avaria).Sum(l => l.Total)
                                - abatimento.Total);
        }

        [Fact]
        public void A_linha_da_avaria_descreve_o_dano()
        {
            var locacao = Contrato(aprovadas: new[] { 1500m });

            locacao.ApurarAvarias();

            var linha = locacao.Fechamento!.Linhas.Single(l => l.Tipo == TipoLinhaFechamento.Avaria);
            Assert.Contains("Risco", linha.BaseCalculo);
            Assert.Contains("avaria de 1500", linha.BaseCalculo);
            Assert.Equal(1500m, linha.Total);
        }

        [Fact]
        public void Apurar_as_avarias_duas_vezes_e_recusado()
        {
            var locacao = Contrato(aprovadas: new[] { 1500m });
            locacao.ApurarAvarias();

            Assert.Throws<DomainException>(() => locacao.ApurarAvarias());
        }

        // ======================= RN-26: multas =======================

        [Fact]
        public void Multa_pendente_de_transito_entra_na_conta()
        {
            var locacao = Contrato();
            locacao.AdicionarMulta(TipoMulta.MultaTransito, 293.47m);

            var (total, redundantes) = locacao.ApurarMultas();

            var linha = locacao.Fechamento!.Linhas.Single(l => l.Tipo == TipoLinhaFechamento.MultaTransito);
            Assert.Equal(293.47m, linha.Total);
            Assert.Equal(293.47m, total);
            Assert.Empty(redundantes);
        }

        [Fact]
        public void Multa_ja_paga_nao_entra()
        {
            // saiu do caixa do cliente por outro caminho
            var locacao = Contrato();
            locacao.AdicionarMulta(TipoMulta.MultaTransito, 293.47m);
            var multa = locacao.Multas.Single();
            Fabrica.DefinirId(multa, 4);
            locacao.PagarMulta(4);

            var (total, _) = locacao.ApurarMultas();

            Assert.Equal(0m, total);
            Assert.DoesNotContain(locacao.Fechamento!.Linhas, l => l.Tipo == TipoLinhaFechamento.MultaTransito);
        }

        [Fact]
        public void Multa_redundante_com_a_apuracao_nao_e_cobrada_mas_e_devolvida()
        {
            // `TipoMulta` é anterior ao fechamento: era o jeito manual de cobrar o que a apuração
            // agora calcula. Cobrar de novo seria faturar duas vezes o mesmo fato — e sumir com ela
            // em silêncio seria pior ainda
            var locacao = Contrato();
            locacao.AdicionarMulta(TipoMulta.Atraso, 150m);
            locacao.AdicionarMulta(TipoMulta.Limpeza, 120m);
            locacao.AdicionarMulta(TipoMulta.DanoVeiculo, 800m);
            locacao.AdicionarMulta(TipoMulta.MultaTransito, 293.47m);

            var (total, redundantes) = locacao.ApurarMultas();

            Assert.Equal(293.47m, total);
            Assert.Equal(3, redundantes.Count);
            Assert.Single(locacao.Fechamento!.Linhas.Where(l => l.Tipo == TipoLinhaFechamento.MultaTransito));
        }

        [Fact]
        public void Multa_de_outros_entra_por_nao_ter_linha_apurada_equivalente()
        {
            var locacao = Contrato();
            locacao.AdicionarMulta(TipoMulta.Outros, 90m);

            var (total, redundantes) = locacao.ApurarMultas();

            Assert.Equal(90m, total);
            Assert.Empty(redundantes);
        }

        [Fact]
        public void Apurar_as_multas_duas_vezes_e_recusado()
        {
            var locacao = Contrato();
            locacao.AdicionarMulta(TipoMulta.MultaTransito, 293.47m);
            locacao.ApurarMultas();

            Assert.Throws<DomainException>(() => locacao.ApurarMultas());
        }

        /// <summary>
        /// Contrato de 3 diárias devolvido, com a conta aberta e o período apurado. Cada lista de
        /// valores vira uma avaria na vistoria de devolução, no status correspondente.
        /// </summary>
        private static Locacao Contrato(
            bool comProtecao = false,
            decimal[]? aprovadas = null,
            decimal[]? emAnalise = null,
            decimal[]? registradas = null,
            decimal[]? isentas = null,
            decimal[]? canceladas = null,
            bool cancelarProtecao = false,
            bool datasVigentes = false)
        {
            // datas retroagidas por padrão, para o prazo do pós-contrato ser conferível; vigentes
            // quando o teste precisa que `Cancelar()` caia **dentro** do contrato
            var retirada = datasVigentes ? DateTime.UtcNow.AddDays(-1) : Retirada;
            var devolucao = datasVigentes ? DateTime.UtcNow.AddDays(2) : Devolucao;

            var locacao = Fabrica.Locacao(dataInicio: retirada, dataFimPrevista: devolucao);

            if (comProtecao)
            {
                // ainda em `Criada`: a proteção cobre desde a retirada
                Fabrica.ContratarSeguro(locacao, idSeguro: 3, valorDiaria: 40m, franquia: Franquia);
                Fabrica.DefinirId(locacao.Seguros.Single(), 7);
            }

            Fabrica.Retirar(locacao);

            if (cancelarProtecao)
                locacao.CancelarSeguro(7);

            locacao.RegistrarVistoria(1, TipoVistoria.Devolucao, NivelCombustivel.Meio, 15_400, null);
            var vistoria = locacao.Vistorias.Single(v => v.Tipo == TipoVistoria.Devolucao);
            Fabrica.DefinirId(vistoria, 5);

            Registrar(vistoria, aprovadas, d => d.Aprovar());
            Registrar(vistoria, emAnalise, d => d.ColocarEmAnalise());
            Registrar(vistoria, registradas, _ => { });
            Registrar(vistoria, isentas, d => d.Isentar());
            Registrar(vistoria, canceladas, d => d.Cancelar());

            locacao.RegistrarDevolucao(devolucao, 1);
            locacao.AbrirFechamento(1);

            var filial = Fabrica.Filial();
            Fabrica.DefinirId(filial, 1);
            locacao.ApurarPeriodo(filial);

            return locacao;
        }

        private static void Registrar(Vistoria vistoria, decimal[]? valores, Action<Dano> decisao)
        {
            foreach (var valor in valores ?? Array.Empty<decimal>())
            {
                vistoria.RegistrarDano($"avaria de {valor:0}", TipoDano.Risco, valor);
                decisao(vistoria.Danos.Last());
            }
        }
    }
}
