using Locadora_Auto.Application.Configuration.Ultils.NotificadorServices;
using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Tests.Fabricas;
using Locadora_Auto.Tests.Fakes;
using Xunit;

namespace Locadora_Auto.Tests.Servicos
{
    /// <summary>
    /// RN-14: a capacidade do tanque é o que transforma a fração do <c>NivelCombustivel</c> em
    /// litro, e litro em dinheiro. Enquanto a apuração do A6 não existe, o que se garante aqui é o
    /// dado — e principalmente a <b>forma da recusa</b>: a guarda de <c>Veiculo</c> lança
    /// <c>InvalidOperationException</c>, que o <c>ExceptionProblemFactory</c> não mapeia e sairia
    /// como 500, então o serviço tem que notificar antes de chegar lá.
    /// </summary>
    public class CapacidadeDeTanqueTests
    {
        private sealed class Cenario
        {
            public required VeiculoService Service { get; init; }
            public required NotificadorService Notificador { get; init; }
            public required ArmazemFake Armazem { get; init; }
        }

        private static Cenario Montar(Veiculo? existente = null)
        {
            var armazem = new ArmazemFake();

            armazem.Semear(Fabrica.Categoria());
            armazem.Semear(Fabrica.Filial());

            if (existente != null) armazem.Semear(existente);

            var notificador = new NotificadorService();

            return new Cenario
            {
                Service = Fabrica.VeiculoService(armazem, notificador),
                Notificador = notificador,
                Armazem = armazem
            };
        }

        private static CriarVeiculoDto Dto(decimal? capacidade = null) => new()
        {
            Placa = "ABC1D23",
            Marca = "Fiat",
            Modelo = "Argo",
            Ano = 2022,
            Chassi = "9BWZZZ377VT004251",
            KmInicial = 0,
            IdCategoria = 1,
            IdFilialAtual = 1,
            CapacidadeTanqueLitros = capacidade
        };

        [Fact]
        public async Task Cadastro_com_tanque_informado_guarda_a_capacidade()
        {
            var cenario = Montar();

            var criado = await cenario.Service.CriarAsync(Dto(48m));

            Assert.False(cenario.Notificador.TemNotificacao());
            Assert.Equal(48m, criado!.CapacidadeTanqueLitros);
        }

        [Fact]
        public async Task Cadastro_sem_tanque_e_aceito()
        {
            // exigir o dado travaria a entrada de frota por um número que a RN-14 já sabe dispensar:
            // tanque nulo apenas não gera cobrança de combustível
            var cenario = Montar();

            var criado = await cenario.Service.CriarAsync(Dto());

            Assert.False(cenario.Notificador.TemNotificacao());
            Assert.Null(criado!.CapacidadeTanqueLitros);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(1001)]
        public async Task Cadastro_com_tanque_fora_da_faixa_notifica_em_vez_de_lancar(int litros)
        {
            var cenario = Montar();

            var criado = await cenario.Service.CriarAsync(Dto(litros));

            Assert.Null(criado);
            Assert.True(cenario.Notificador.TemNotificacao());
            Assert.Empty(cenario.Armazem.Tabela<Veiculo>());
        }

        [Fact]
        public async Task Edicao_sem_o_campo_mantem_o_tanque_cadastrado()
        {
            var veiculo = Fabrica.Veiculo();
            veiculo.DefinirCapacidadeTanque(48m);
            var cenario = Montar(veiculo);

            var sucesso = await cenario.Service.AtualizarAsync(
                veiculo.IdVeiculo, new AtualizarVeiculoDto { Marca = "FIAT" });

            Assert.True(sucesso);
            Assert.Equal(48m, veiculo.CapacidadeTanqueLitros);
        }

        [Fact]
        public async Task Edicao_com_tanque_invalido_notifica_e_nao_altera_nada()
        {
            var veiculo = Fabrica.Veiculo();
            veiculo.DefinirCapacidadeTanque(48m);
            var cenario = Montar(veiculo);

            var sucesso = await cenario.Service.AtualizarAsync(
                veiculo.IdVeiculo,
                new AtualizarVeiculoDto { Marca = "CHEVROLET", CapacidadeTanqueLitros = 0m });

            Assert.False(sucesso);
            Assert.True(cenario.Notificador.TemNotificacao());
            Assert.Equal(48m, veiculo.CapacidadeTanqueLitros);
            Assert.Equal("FIAT", veiculo.Marca);
        }
    }
}
