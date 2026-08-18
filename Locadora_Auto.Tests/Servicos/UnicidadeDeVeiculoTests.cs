using Locadora_Auto.Application.Configuration.Ultils.NotificadorServices;
using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Tests.Fabricas;
using Locadora_Auto.Tests.Fakes;
using Xunit;

namespace Locadora_Auto.Tests.Servicos
{
    /// <summary>
    /// RN-55: placa e chassi são únicos <b>entre os veículos ativos</b>. O índice parcial de
    /// <c>VeiculoConfig</c> é a garantia; estas checagens são a recusa amigável, e as duas têm que
    /// dizer a mesma coisa — se divergirem, a regra vira 500 no lugar de 4xx.
    ///
    /// O que se testa aqui é justamente o recorte: o mesmo dado que é recusado contra um ativo
    /// precisa ser aceito contra um inativo, senão o carro que saiu da frota continua segurando
    /// uma placa que o Detran já reemitiu.
    /// </summary>
    public class UnicidadeDeVeiculoTests
    {
        private sealed class Cenario
        {
            public required VeiculoService Service { get; init; }
            public required NotificadorService Notificador { get; init; }
            public required ArmazemFake Armazem { get; init; }
        }

        private const string Placa = "ABC1D23";
        private const string Chassi = "9BWZZZ377VT004251";

        private static Cenario Montar(Veiculo? existente = null)
        {
            var armazem = new ArmazemFake();

            var categoria = Fabrica.Categoria();
            armazem.Semear(categoria);

            var filial = Fabrica.Filial();
            armazem.Semear(filial);

            if (existente != null) armazem.Semear(existente);

            var notificador = new NotificadorService();

            var service = Fabrica.VeiculoService(armazem, notificador);

            return new Cenario { Service = service, Notificador = notificador, Armazem = armazem };
        }

        private static CriarVeiculoDto Dto(string placa = Placa, string chassi = Chassi) => new()
        {
            Placa = placa,
            Marca = "Fiat",
            Modelo = "Argo",
            Ano = 2022,
            Chassi = chassi,
            KmInicial = 0,
            IdCategoria = 1,
            IdFilialAtual = 1
        };

        [Fact]
        public async Task Placa_repetida_entre_ativos_e_recusada()
        {
            var cenario = Montar(Fabrica.Veiculo(placa: Placa));

            var criado = await cenario.Service.CriarAsync(Dto(chassi: "9BWZZZ377VT000000"));

            Assert.Null(criado);
            Assert.True(cenario.Notificador.TemNotificacao());
        }

        [Fact]
        public async Task Chassi_repetido_entre_ativos_e_recusado()
        {
            var cenario = Montar(Fabrica.Veiculo(placa: "XYZ9K88"));

            var criado = await cenario.Service.CriarAsync(Dto(placa: "QRS4T56"));

            Assert.Null(criado);
            Assert.True(cenario.Notificador.TemNotificacao());
        }

        /// <summary>
        /// O ponto inteiro da RN-55: o índice global de antes travava este cadastro.
        /// </summary>
        [Fact]
        public async Task Placa_de_veiculo_inativo_pode_ser_recadastrada()
        {
            var antigo = Fabrica.Veiculo(placa: Placa);
            antigo.Desativar();

            var cenario = Montar(antigo);

            var criado = await cenario.Service.CriarAsync(Dto());

            Assert.False(cenario.Notificador.TemNotificacao());
            Assert.NotNull(criado);
        }

        /// <summary>
        /// A comparação usa a forma gravada (trim + maiúscula) e não a digitada. Sem isso a
        /// minúscula passava pela checagem, <c>Veiculo.Criar</c> gravava em maiúscula e a recusa
        /// da regra saía do banco como 500.
        /// </summary>
        [Fact]
        public async Task Placa_em_minuscula_nao_escapa_da_checagem()
        {
            var cenario = Montar(Fabrica.Veiculo(placa: Placa));

            var criado = await cenario.Service.CriarAsync(Dto(placa: "  abc1d23 ", chassi: "9BWZZZ377VT000000"));

            Assert.Null(criado);
            Assert.True(cenario.Notificador.TemNotificacao());
        }

        /// <summary>
        /// Reativar é a única operação que colide no índice parcial: enquanto o veículo estava
        /// inativo, nada impedia recadastrar a placa dele.
        /// </summary>
        [Fact]
        public async Task Reativar_veiculo_cuja_placa_foi_recadastrada_e_recusado()
        {
            var antigo = Fabrica.Veiculo(placa: Placa);
            antigo.Desativar();

            var cenario = Montar(antigo);
            var novo = Fabrica.Veiculo(placa: Placa);
            cenario.Armazem.Semear(novo);

            var reativado = await cenario.Service.AtivarAsync(antigo.IdVeiculo);

            Assert.False(reativado);
            Assert.True(cenario.Notificador.TemNotificacao());
            Assert.False(antigo.Ativo);
        }

        [Fact]
        public async Task Reativar_veiculo_sem_conflito_e_aceito()
        {
            var antigo = Fabrica.Veiculo(placa: Placa);
            antigo.Desativar();

            var cenario = Montar(antigo);

            var reativado = await cenario.Service.AtivarAsync(antigo.IdVeiculo);

            Assert.True(reativado);
            Assert.False(cenario.Notificador.TemNotificacao());
            Assert.True(antigo.Ativo);
        }
    }
}
