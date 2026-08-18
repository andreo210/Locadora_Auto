using Locadora_Auto.Application.Configuration.Ultils.NotificadorServices;
using Locadora_Auto.Application.Models.Dto;
// os DTOs de filial ficam num namespace aninhado que repete o nome do de fora — typo consolidado
using Locadora_Auto.Application.Models.Dto.Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Application.Services.FilialServices;
using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Tests.Fabricas;
using Locadora_Auto.Tests.Fakes;
using Xunit;

namespace Locadora_Auto.Tests.Servicos
{
    /// <summary>
    /// Doc 07 §9: os parâmetros de fechamento moram na filial. Nenhum é lido pela apuração ainda —
    /// o que se garante aqui é o caminho do dado até o banco e, sobretudo, a forma da recusa:
    /// <c>Filial.DefinirParametrosFinanceiros</c> lança <c>InvalidOperationException</c>, que o
    /// <c>ExceptionProblemFactory</c> não mapeia e sairia como 500, então o serviço precisa
    /// notificar antes de chamá-la.
    /// </summary>
    public class ParametrosDeFechamentoTests
    {
        private sealed class Cenario
        {
            public required FilialService Service { get; init; }
            public required NotificadorService Notificador { get; init; }
            public required ArmazemFake Armazem { get; init; }
        }

        private static Cenario Montar(Filial? existente = null)
        {
            var armazem = new ArmazemFake();

            if (existente != null) armazem.Semear(existente);

            var notificador = new NotificadorService();

            return new Cenario
            {
                Service = Fabrica.FilialService(armazem, notificador),
                Notificador = notificador,
                Armazem = armazem
            };
        }

        private static EnderecoDto Endereco() => new()
        {
            Logradouro = "Rua das Palmeiras",
            Numero = "100",
            Bairro = "Centro",
            Cidade = "São Paulo",
            Estado = "SP",
            Cep = "01001000"
        };

        [Fact]
        public async Task Filial_nova_grava_os_parametros_informados()
        {
            var cenario = Montar();

            var criada = await cenario.Service.CriarFilialAsync(new CriarFilialDto
            {
                Nome = "Filial Aeroporto",
                Cidade = "São Paulo",
                Endereco = Endereco(),
                HabilitadaOneWay = false,
                TaxaRetornoOneWay = 250m,
                ToleranciaMinutos = 45,
                PercentualHoraExcedente = 0.25m,
                PrecoLitroCombustivel = 6.19m,
                TaxaServicoAbastecimento = 35m,
                ValorLimpezaEspecial = 120m
            });

            Assert.False(cenario.Notificador.TemNotificacao());
            Assert.NotNull(criada);
            Assert.False(criada!.HabilitadaOneWay);
            Assert.Equal(250m, criada.TaxaRetornoOneWay);
            Assert.Equal(45, criada.ToleranciaMinutos);
            Assert.Equal(0.25m, criada.PercentualHoraExcedente);
            Assert.Equal(6.19m, criada.PrecoLitroCombustivel);
            Assert.Equal(35m, criada.TaxaServicoAbastecimento);
            Assert.Equal(120m, criada.ValorLimpezaEspecial);
        }

        [Fact]
        public async Task Filial_nova_sem_os_campos_assume_os_padroes_da_casa()
        {
            var cenario = Montar();

            var criada = await cenario.Service.CriarFilialAsync(new CriarFilialDto
            {
                Nome = "Filial Centro",
                Cidade = "São Paulo",
                Endereco = Endereco()
            });

            Assert.False(cenario.Notificador.TemNotificacao());
            Assert.Equal(Filial.ToleranciaPadraoMinutos, criada!.ToleranciaMinutos);
            Assert.Equal(Filial.PercentualHoraExcedentePadrao, criada.PercentualHoraExcedente);
            Assert.True(criada.HabilitadaOneWay);
        }

        [Fact]
        public async Task Parametro_invalido_no_cadastro_notifica_em_vez_de_lancar()
        {
            var cenario = Montar();

            var criada = await cenario.Service.CriarFilialAsync(new CriarFilialDto
            {
                Nome = "Filial Centro",
                Cidade = "São Paulo",
                Endereco = Endereco(),
                PrecoLitroCombustivel = -1m
            });

            Assert.Null(criada);
            Assert.True(cenario.Notificador.TemNotificacao());
            Assert.Empty(cenario.Armazem.Tabela<Filial>());
        }

        [Fact]
        public async Task Edicao_sem_os_campos_mantem_os_parametros_da_filial()
        {
            // é a garantia que protege a praça do cliente que não conhece os campos — o Front de
            // hoje, entre eles: editar o nome não pode zerar o preço do litro
            var filial = Fabrica.Filial();
            filial.DefinirParametrosFinanceiros(precoLitroCombustivel: 6.19m, toleranciaMinutos: 45);
            var cenario = Montar(filial);

            var sucesso = await cenario.Service.AtualizarFilialAsync(filial.IdFilial, new AtualizarFilialDto
            {
                Nome = "Filial Centro Renomeada",
                Cidade = "São Paulo",
                Endereco = Endereco()
            });

            Assert.True(sucesso);
            Assert.False(cenario.Notificador.TemNotificacao());
            Assert.Equal(6.19m, filial.PrecoLitroCombustivel);
            Assert.Equal(45, filial.ToleranciaMinutos);
        }

        [Fact]
        public async Task Parametro_invalido_na_edicao_notifica_e_nao_altera_nada()
        {
            var filial = Fabrica.Filial();
            filial.DefinirParametrosFinanceiros(precoLitroCombustivel: 6.19m);
            var cenario = Montar(filial);

            var sucesso = await cenario.Service.AtualizarFilialAsync(filial.IdFilial, new AtualizarFilialDto
            {
                Nome = "Filial Centro Renomeada",
                Cidade = "São Paulo",
                Endereco = Endereco(),
                ToleranciaMinutos = Filial.ToleranciaMaximaMinutos + 1
            });

            Assert.False(sucesso);
            Assert.True(cenario.Notificador.TemNotificacao());
            Assert.Equal(6.19m, filial.PrecoLitroCombustivel);
            Assert.Equal("Filial Centro", filial.Nome);
        }
    }
}
