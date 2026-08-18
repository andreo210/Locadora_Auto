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
    /// O lado da escrita do indicador "tentativas de sobreposição recusadas" (seção 12): a recusa
    /// da RN-40 passou a deixar rastro.
    ///
    /// O que se protege aqui é a ligação, que é fácil de perder: a regra já recusava certo antes
    /// disso, então nenhum teste de comportamento quebraria se o registro sumisse — o indicador
    /// simplesmente voltaria a marcar zero para sempre, e ninguém desconfiaria.
    ///
    /// A recusa pelo <b>banco</b> (RN-41, <c>23P01</c>) não tem teste aqui: o
    /// <c>RepositorioFake</c> não tem constraint, e reproduzi-la exigiria integração com Postgres.
    /// O caminho está no <c>catch</c> de <c>CriarAsync</c>, junto do <c>throw</c> que preserva o 409.
    /// </summary>
    public class RecusaDeSobreposicaoTests
    {
        private const int KmDoVeiculo = 15_000;

        private sealed class Cenario
        {
            public required LocacaoService Service { get; init; }
            public required NotificadorService Notificador { get; init; }
            public required RecusaSobreposicaoRepositoryFake Recusas { get; init; }
            public required ArmazemFake Armazem { get; init; }
            public required Clientes Cliente { get; init; }
            public required Veiculo Veiculo { get; init; }
            public required Funcionario Funcionario { get; init; }
            public required Filial Filial { get; init; }
        }

        private static Cenario Montar()
        {
            var armazem = new ArmazemFake();

            var cliente = Fabrica.Cliente();
            armazem.Semear(cliente);

            var categoria = Fabrica.Categoria();
            armazem.Semear(categoria);

            var filial = Fabrica.Filial();
            armazem.Semear(filial);

            var funcionario = Fabrica.Funcionario();
            armazem.Semear(funcionario);

            var veiculo = Fabrica.Veiculo(categoria.Id, filial.IdFilial);
            armazem.Semear(veiculo);

            var notificador = new NotificadorService();
            var recusas = new RecusaSobreposicaoRepositoryFake(armazem);

            return new Cenario
            {
                Service = Fabrica.LocacaoService(armazem, notificador, recusas: recusas),
                Notificador = notificador,
                Recusas = recusas,
                Armazem = armazem,
                Cliente = cliente,
                Veiculo = veiculo,
                Funcionario = funcionario,
                Filial = filial
            };
        }

        private static CriarLocacaoDto Dto(Cenario cenario, DateTime inicio, DateTime fim) => new()
        {
            IdCliente = cenario.Cliente.IdCliente,
            IdVeiculo = cenario.Veiculo.IdVeiculo,
            IdFuncionario = cenario.Funcionario.IdFuncionario,
            IdFilialRetirada = cenario.Filial.IdFilial,
            DataInicio = inicio,
            DataFimPrevista = fim,
            KmInicial = KmDoVeiculo,
            ValorPrevisto = 450m
        };

        /// <summary>Contrato futuro já vendido para o mesmo veículo.</summary>
        private static void SemearContratoNoPeriodo(Cenario cenario, DateTime inicio, DateTime fim)
        {
            var existente = Fabrica.Locacao(
                veiculo: Fabrica.Veiculo(placa: "OUT0R01"),
                dataInicio: inicio,
                dataFimPrevista: fim);

            typeof(Locacao).GetProperty(nameof(Locacao.IdVeiculo))!
                .SetValue(existente, cenario.Veiculo.IdVeiculo);

            cenario.Armazem.Semear(existente);
        }

        [Fact]
        public async Task Abertura_recusada_por_sobreposicao_registra_a_tentativa()
        {
            var cenario = Montar();

            var inicio = DateTime.UtcNow.AddDays(10);
            var fim = DateTime.UtcNow.AddDays(14);
            SemearContratoNoPeriodo(cenario, inicio, fim);

            var criada = await cenario.Service.CriarAsync(
                Dto(cenario, inicio.AddDays(1), fim.AddDays(-1)));

            Assert.Null(criada);
            Assert.True(cenario.Notificador.TemNotificacao());

            var recusa = Assert.Single(cenario.Armazem.Tabela<RecusaSobreposicao>());
            Assert.Equal(cenario.Veiculo.IdVeiculo, recusa.IdVeiculo);
            Assert.Equal(cenario.Filial.IdFilial, recusa.IdFilialRetirada);
            Assert.Equal(OrigemRecusa.Consulta, recusa.Origem);

            // abertura, não extensão
            Assert.Null(recusa.IdLocacaoEmExtensao);
        }

        [Fact]
        public async Task Abertura_aceita_nao_registra_recusa()
        {
            var cenario = Montar();

            var criada = await cenario.Service.CriarAsync(
                Dto(cenario, DateTime.UtcNow, Fabrica.DaquiADias(3)));

            Assert.NotNull(criada);
            Assert.False(cenario.Notificador.TemNotificacao());
            Assert.Empty(cenario.Armazem.Tabela<RecusaSobreposicao>());
        }

        [Fact]
        public async Task Recusa_por_outro_motivo_nao_conta_como_sobreposicao()
        {
            // o indicador é de sobreposição, não de "abertura recusada": misturar os dois faria o
            // número subir por cliente sem CNH e apontar problema de agenda que não existe
            var cenario = Montar();
            cenario.Veiculo.Desativar();

            var criada = await cenario.Service.CriarAsync(
                Dto(cenario, DateTime.UtcNow, Fabrica.DaquiADias(3)));

            Assert.Null(criada);
            Assert.True(cenario.Notificador.TemNotificacao());
            Assert.Empty(cenario.Armazem.Tabela<RecusaSobreposicao>());
        }

        [Fact]
        public async Task Extensao_recusada_registra_a_tentativa_marcada_como_extensao()
        {
            var cenario = Montar();

            var criada = await cenario.Service.CriarAsync(
                Dto(cenario, DateTime.UtcNow, Fabrica.DaquiADias(3)));

            Assert.NotNull(criada);

            // um segundo contrato ocupa o período para o qual se quer esticar o primeiro
            SemearContratoNoPeriodo(cenario, Fabrica.DaquiADias(4), Fabrica.DaquiADias(8));

            var estendida = await cenario.Service.AtualizarAsync(
                criada!.IdLocacao,
                new AtualizarLocacaoDto
                {
                    DataFimPrevista = Fabrica.DaquiADias(6),
                    KmInicial = KmDoVeiculo,
                    ValorPrevisto = 900m
                });

            Assert.Null(estendida);

            var recusa = Assert.Single(cenario.Armazem.Tabela<RecusaSobreposicao>());
            Assert.Equal(OrigemRecusa.Consulta, recusa.Origem);
            Assert.Equal(criada.IdLocacao, recusa.IdLocacaoEmExtensao);
        }
    }
}
