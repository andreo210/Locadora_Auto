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
    /// Os 15 cenários gherkin do doc 07 §10, um a um, <b>pelo serviço</b>.
    ///
    /// A aritmética de cada regra já está fixada nos testes de domínio, que são onde ela se depura.
    /// O que estes aqui pegam é outra classe de defeito: a que só aparece quando as dez apurações
    /// rodam juntas, sobre um grafo carregado do repositório e traduzido para DTO — `Include` que
    /// falta e faz a conta sair menor, ordem trocada entre período e franquia, mapper que perde
    /// linha. Nenhum teste de unidade do cálculo enxerga isso.
    ///
    /// Os números são os do documento, literais. Quando um deles mudar, é a especificação que está
    /// mudando.
    /// </summary>
    public class CriteriosDeAceiteDoFechamentoTests
    {
        private static readonly DateTime NoveDaManha = new(2026, 3, 10, 9, 0, 0, DateTimeKind.Utc);

        /// <summary>Devolução que fecha 3 ciclos de 24h cravados — R$ 450,00 de diárias.</summary>
        private static readonly DateTime TresDiarias = NoveDaManha.AddDays(3);

        // ======================= período (RN-01 a RN-07) =======================

        [Fact]
        public async Task Diaria_e_ciclo_de_24h_nao_calendario()
        {
            // retirada 10/03 às 22:00, devolução 11/03 às 20:00 — dois dias no calendário, 22h de
            // contrato
            var cenario = Montar(
                retirada: new DateTime(2026, 3, 10, 22, 0, 0, DateTimeKind.Utc),
                devolucao: new DateTime(2026, 3, 11, 20, 0, 0, DateTimeKind.Utc));

            var extrato = await cenario.Apurar();

            Assert.Equal(1m, Quantidade(extrato, TipoLinhaFechamento.Diaria));
            Assert.Equal(150m, Total(extrato, TipoLinhaFechamento.Diaria));
        }

        [Fact]
        public async Task Tolerancia_de_30_minutos_nao_gera_cobranca()
        {
            var cenario = Montar(devolucao: new DateTime(2026, 3, 12, 9, 25, 0, DateTimeKind.Utc));

            var extrato = await cenario.Apurar();

            Assert.Equal(2m, Quantidade(extrato, TipoLinhaFechamento.Diaria));
            Assert.Equal(0m, Total(extrato, TipoLinhaFechamento.HoraExcedente));
        }

        [Fact]
        public async Task Hora_excedente_por_hora_iniciada_apos_a_tolerancia()
        {
            var cenario = Montar(devolucao: new DateTime(2026, 3, 12, 11, 30, 0, DateTimeKind.Utc));

            var extrato = await cenario.Apurar();

            Assert.Equal(2m, Quantidade(extrato, TipoLinhaFechamento.Diaria));
            Assert.Equal(2m, Quantidade(extrato, TipoLinhaFechamento.HoraExcedente));
            Assert.Equal(100m, Total(extrato, TipoLinhaFechamento.HoraExcedente));

            // "o total de período deve ser R$ 400,00"
            Assert.Equal(400m,
                Total(extrato, TipoLinhaFechamento.Diaria) + Total(extrato, TipoLinhaFechamento.HoraExcedente));
        }

        [Fact]
        public async Task Teto_de_uma_diaria_substitui_as_horas_excedentes()
        {
            var cenario = Montar(devolucao: new DateTime(2026, 3, 12, 13, 0, 0, DateTimeKind.Utc));

            var extrato = await cenario.Apurar();

            Assert.Equal(0m, Total(extrato, TipoLinhaFechamento.HoraExcedente));
            Assert.Equal(1m, Quantidade(extrato, TipoLinhaFechamento.DiariaPorTetoDeHoras));

            // "devem ser cobradas 3 diárias no total, R$ 450,00"
            Assert.Equal(450m,
                Total(extrato, TipoLinhaFechamento.Diaria) + Total(extrato, TipoLinhaFechamento.DiariaPorTetoDeHoras));
        }

        // ======================= quilometragem (RN-08 a RN-12) =======================

        [Fact]
        public async Task Km_livre_nao_cobra_excedente()
        {
            var cenario = Montar(
                limiteKm: null, valorKmExcedente: null,
                kmRetirada: 15_000, kmDevolucao: 16_800);

            var extrato = await cenario.Apurar();

            Assert.Equal(0m, Total(extrato, TipoLinhaFechamento.KmExcedente));
        }

        [Fact]
        public async Task Km_controlado_cobra_o_que_passou_da_franquia()
        {
            // o cenário fala em "um contrato de 3 diárias cobradas"
            var cenario = Montar(devolucao: TresDiarias, kmRetirada: 15_000, kmDevolucao: 15_750);

            var extrato = await cenario.Apurar();
            var linha = Linha(extrato, TipoLinhaFechamento.KmExcedente);

            Assert.Contains("franquia de 600 km", linha.BaseCalculo);
            Assert.Equal(150m, linha.Quantidade);
            Assert.Equal(180m, linha.Total);

            // "e o Veiculo.KmAtual deve passar a 15.750"
            Assert.Equal(15_750, cenario.Veiculo.KmAtual);
        }

        // ======================= combustível (RN-13 a RN-16) =======================

        [Fact]
        public async Task Combustivel_cobrado_pela_diferenca_de_nivel()
        {
            var cenario = Montar(
                nivelDevolucao: NivelCombustivel.Meio,
                precoLitro: 6.20m, taxaAbastecimento: 40m);

            var extrato = await cenario.Apurar();

            Assert.Equal(24m, Quantidade(extrato, TipoLinhaFechamento.Combustivel));
            Assert.Equal(148.80m, Total(extrato, TipoLinhaFechamento.Combustivel));
            Assert.Equal(40m, Total(extrato, TipoLinhaFechamento.TaxaServicoAbastecimento));
        }

        [Fact]
        public async Task Devolver_com_mais_combustivel_nao_gera_credito()
        {
            var cenario = Montar(
                nivelRetirada: NivelCombustivel.Meio,
                nivelDevolucao: NivelCombustivel.Cheio,
                precoLitro: 6.20m, taxaAbastecimento: 40m);

            var extrato = await cenario.Apurar();

            Assert.Equal(0m, Total(extrato, TipoLinhaFechamento.Combustivel));

            // "e nenhum crédito deve ser lançado"
            Assert.DoesNotContain(extrato.Linhas,
                l => l.Natureza == nameof(NaturezaLinhaFechamento.Credito));
        }

        // ======================= taxas (RN-21 a RN-23) =======================

        [Fact]
        public async Task Devolucao_em_outra_filial_cobra_taxa_de_retorno()
        {
            var cenario = Montar(idFilialDevolucao: 3, taxaOneWay: 250m);

            var extrato = await cenario.Apurar();

            Assert.Equal(250m, Total(extrato, TipoLinhaFechamento.TaxaRetornoOneWay));
        }

        // ======================= avarias (RN-24 a RN-26) =======================

        [Fact]
        public async Task Avaria_com_protecao_e_limitada_a_franquia()
        {
            var cenario = Montar(franquia: 2_000m, avariasAprovadas: new[] { 1_500m, 1_800m });

            var extrato = await cenario.Apurar();

            // "a cobrança de avarias ao cliente deve ser R$ 2.000,00"
            var cobrado = Total(extrato, TipoLinhaFechamento.Avaria)
                          - Total(extrato, TipoLinhaFechamento.AbatimentoPorProtecao);

            Assert.Equal(3_300m, Total(extrato, TipoLinhaFechamento.Avaria));
            Assert.Equal(2_000m, cobrado);
        }

        [Fact]
        public async Task Avaria_em_analise_nao_entra_no_fechamento()
        {
            var cenario = Montar(avariasEmAnalise: new[] { 900m });

            var resultado = await cenario.ApurarComResultado();

            Assert.Equal(0m, Total(resultado.Fechamento, TipoLinhaFechamento.Avaria));

            // o doc pede o evento `AvariaEnviadaParaAnalise`. Não há barramento de eventos no
            // sistema (doc 07 §7 é futuro), e o que cumpre o papel hoje é o aviso da apuração —
            // com o prazo do pós-contrato, que o evento sozinho não carregaria
            Assert.Contains(resultado.Avisos, a => a.Contains("em análise") && a.Contains("Prazo"));
        }

        // ======================= caução e composição (RN-27 a RN-32) =======================

        [Fact]
        public async Task Caucao_cobre_o_saldo_e_o_restante_volta()
        {
            // 3 diárias de R$ 150,00 mais R$ 490,00 de multa dão os R$ 940,00 do cenário
            var cenario = Montar(devolucao: TresDiarias, caucao: 1_500m, multaDeAjuste: 490m);

            var resultado = await cenario.ApurarComResultado();
            var caucao = cenario.Locacao.Caucoes.Single();

            Assert.Equal(940m, resultado.Fechamento.Saldo);
            Assert.Equal(940m, resultado.CaucaoConsumida);
            Assert.Equal(560m, caucao.ValorDisponivel);
            Assert.Equal(Caucao.StatusCaucao.Utilizada, caucao.Status);
        }

        [Fact]
        public async Task Saldo_maior_que_a_caucao_gera_cobranca_residual()
        {
            // 3 diárias mais R$ 1.850,00 de multa dão os R$ 2.300,00 do cenário
            var cenario = Montar(devolucao: TresDiarias, caucao: 1_500m, multaDeAjuste: 1_850m);

            var resultado = await cenario.ApurarComResultado();

            Assert.Equal(1_500m, resultado.CaucaoConsumida);
            Assert.Equal(0m, cenario.Locacao.Caucoes.Single().ValorDisponivel);
            Assert.Equal(800m, resultado.SaldoResidual);
            Assert.Equal(StatusLocacao.ComSaldoResidual, cenario.Locacao.Status);
        }

        [Fact]
        public async Task Fechamento_e_idempotente()
        {
            // 3 diárias mais R$ 790,00 de multa dão os R$ 1.240,00 do cenário
            var cenario = Montar(devolucao: TresDiarias, caucao: 1_500m, multaDeAjuste: 790m);

            var primeira = await cenario.ApurarComResultado();
            var segunda = await cenario.ApurarComResultado();

            Assert.Equal(1_240m, primeira.Fechamento.Saldo);

            Assert.Equal(primeira.Fechamento.Linhas.Count, segunda.Fechamento.Linhas.Count);
            Assert.Equal(1_240m, segunda.Fechamento.Saldo);
            Assert.Equal(1_240m, cenario.Locacao.Caucoes.Single().ValorConsumido);
            Assert.True(segunda.JaEstavaApurado);
        }

        [Fact]
        public async Task Nao_fecha_sem_vistoria_de_retirada()
        {
            var cenario = Montar(comVistoriaDeRetirada: false);

            var resultado = await cenario.Service.ApurarFechamentoAsync(
                cenario.Locacao.IdLocacao, new ApurarFechamentoDto { IdFuncionarioApuracao = 1 });

            Assert.Null(resultado);
            Assert.True(cenario.Notificador.TemNotificacao());

            // "e o contrato deve permanecer em status Criada" — sem a vistoria de retirada ele
            // nunca chegou a `EmAndamento`, então nem a devolução aconteceu
            Assert.Equal(StatusLocacao.Criada, cenario.Locacao.Status);
            Assert.Null(cenario.Locacao.Fechamento);
        }

        // ======================= leitura do extrato =======================

        private static LinhaFechamentoDto Linha(FechamentoLocacaoDto extrato, TipoLinhaFechamento tipo)
            => extrato.Linhas.Single(l => l.Tipo == tipo.ToString());

        private static decimal Total(FechamentoLocacaoDto extrato, TipoLinhaFechamento tipo)
            => extrato.Linhas.Where(l => l.Tipo == tipo.ToString()).Sum(l => l.Total);

        private static decimal Quantidade(FechamentoLocacaoDto extrato, TipoLinhaFechamento tipo)
            => extrato.Linhas.Where(l => l.Tipo == tipo.ToString()).Sum(l => l.Quantidade);

        // ======================= montagem =======================

        private sealed class Cenario
        {
            public required LocacaoService Service { get; init; }
            public required NotificadorService Notificador { get; init; }
            public required Locacao Locacao { get; init; }
            public required Veiculo Veiculo { get; init; }

            public async Task<ResultadoDaApuracaoDto> ApurarComResultado()
            {
                var resultado = await Service.ApurarFechamentoAsync(
                    Locacao.IdLocacao, new ApurarFechamentoDto { IdFuncionarioApuracao = 1 });

                Assert.False(Notificador.TemNotificacao());
                Assert.NotNull(resultado);

                return resultado!;
            }

            public async Task<FechamentoLocacaoDto> Apurar() => (await ApurarComResultado()).Fechamento;
        }

        /// <summary>
        /// Contrato do doc 07 §10: retirada 10/03 às 09:00, devolução 12/03 às 09:00, diária de
        /// R$ 150,00, categoria com 200 km/diária a R$ 1,20 e tolerância de 30 minutos. Cada
        /// cenário muda só o que o seu texto muda.
        /// </summary>
        private static Cenario Montar(
            DateTime? retirada = null,
            DateTime? devolucao = null,
            decimal diaria = 150m,
            int? limiteKm = 200,
            decimal? valorKmExcedente = 1.20m,
            int kmRetirada = 15_000,
            int kmDevolucao = 15_000,
            decimal? tanque = 48m,
            NivelCombustivel nivelRetirada = NivelCombustivel.Cheio,
            NivelCombustivel nivelDevolucao = NivelCombustivel.Cheio,
            decimal precoLitro = 0m,
            decimal taxaAbastecimento = 0m,
            int idFilialDevolucao = 1,
            decimal taxaOneWay = 0m,
            decimal? franquia = null,
            decimal[]? avariasAprovadas = null,
            decimal[]? avariasEmAnalise = null,
            decimal? caucao = null,
            decimal? multaDeAjuste = null,
            bool comVistoriaDeRetirada = true)
        {
            var inicio = retirada ?? NoveDaManha;
            var fim = devolucao ?? inicio.AddDays(2);

            var armazem = new ArmazemFake();

            var categoria = Fabrica.Categoria(
                valorDiaria: diaria, limiteKm: limiteKm, valorKmExcedente: valorKmExcedente);
            armazem.Semear(categoria);

            var filialRetirada = Fabrica.Filial();
            armazem.Semear(filialRetirada);

            var filialDevolucao = filialRetirada;

            if (idFilialDevolucao != filialRetirada.IdFilial)
            {
                filialDevolucao = Fabrica.Filial("Filial Aeroporto");
                armazem.Semear(filialDevolucao);
                Fabrica.DefinirId(filialDevolucao, idFilialDevolucao);
            }

            filialDevolucao.DefinirParametrosFinanceiros(
                taxaRetornoOneWay: taxaOneWay,
                precoLitroCombustivel: precoLitro,
                taxaServicoAbastecimento: taxaAbastecimento);

            var veiculo = Fabrica.Veiculo(categoria.Id, filialRetirada.IdFilial);
            veiculo.DefinirCapacidadeTanque(tanque);
            armazem.Semear(veiculo);

            var locacao = Fabrica.Locacao(
                veiculo: veiculo,
                dataInicio: inicio,
                dataFimPrevista: fim > inicio ? fim : inicio.AddDays(1),
                kmInicial: kmRetirada,
                idFilialRetirada: filialRetirada.IdFilial,
                valorDiariaContratada: diaria);

            if (franquia is { } valorFranquia)
                Fabrica.ContratarSeguro(locacao, idSeguro: 3, valorDiaria: 0.01m, franquia: valorFranquia);

            if (caucao is { } valorCaucao)
            {
                locacao.RegistrarCaucao(valorCaucao);
                Fabrica.DefinirId(locacao.Caucoes.Single(), 4);
                locacao.BloquearCaucao(4);
            }

            if (comVistoriaDeRetirada)
            {
                locacao.RegistrarVistoria(1, TipoVistoria.Retirada, nivelRetirada, kmRetirada, null);
                locacao.RegistrarVistoria(1, TipoVistoria.Devolucao, nivelDevolucao, kmDevolucao, null);

                var vistoria = locacao.Vistorias.Single(v => v.Tipo == TipoVistoria.Devolucao);
                Fabrica.DefinirId(vistoria, 5);

                RegistrarAvarias(vistoria, avariasAprovadas, d => d.Aprovar());
                RegistrarAvarias(vistoria, avariasEmAnalise, d => d.ColocarEmAnalise());

                locacao.RegistrarDevolucao(fim, filialDevolucao.IdFilial);

                if (multaDeAjuste is { } valorMulta)
                    locacao.AdicionarMulta(TipoMulta.MultaTransito, valorMulta);
            }

            armazem.Semear(locacao);

            // o `RepositorioFake` ignora `incluir`: em produção quem materializa o grafo é o EF
            Fabrica.LigarNavegacoesDoFechamento(locacao, veiculo, categoria, filialRetirada, filialDevolucao);

            var notificador = new NotificadorService();

            return new Cenario
            {
                Service = Fabrica.LocacaoService(armazem, notificador),
                Notificador = notificador,
                Locacao = locacao,
                Veiculo = veiculo
            };
        }

        private static void RegistrarAvarias(Vistoria vistoria, decimal[]? valores, Action<Dano> decisao)
        {
            foreach (var valor in valores ?? Array.Empty<decimal>())
            {
                vistoria.RegistrarDano($"avaria de {valor:0}", TipoDano.Risco, valor);
                decisao(vistoria.Danos.Last());
            }
        }
    }
}
