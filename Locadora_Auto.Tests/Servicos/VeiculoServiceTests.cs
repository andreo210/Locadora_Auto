using Locadora_Auto.Application.Configuration.Ultils.NotificadorServices;
using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Tests.Fabricas;
using Locadora_Auto.Tests.Fakes;
using Xunit;

namespace Locadora_Auto.Tests.Servicos
{
    /// <summary>
    /// A fila do pátio (RN-44/RN-45): o carro devolvido não volta à oferta sozinho, e sem uma porta
    /// de liberação ele fica preso em <see cref="StatusVeiculo.EmPreparacao"/> — frota parada que
    /// nenhuma consulta de disponibilidade enxerga.
    /// </summary>
    public class VeiculoServiceTests
    {
        private sealed class Cenario
        {
            public required VeiculoService Service { get; init; }
            public required NotificadorService Notificador { get; init; }
            public required VeiculosRepositoryFake Veiculos { get; init; }
            public required Veiculo Veiculo { get; init; }
        }

        /// <summary>
        /// Monta um veículo no armazém no estágio pedido do ciclo do ativo. O caminho é sempre o
        /// real — locar, devolver — porque <c>AplicarStatus</c> é privado de propósito.
        /// </summary>
        private static Cenario Montar(StatusVeiculo status = StatusVeiculo.EmPreparacao, bool ativo = true)
        {
            var armazem = new ArmazemFake();

            var categoria = Fabrica.Categoria();
            armazem.Semear(categoria);

            var filial = Fabrica.Filial();
            armazem.Semear(filial);

            var veiculo = Fabrica.Veiculo(categoria.Id, filial.IdFilial);

            switch (status)
            {
                case StatusVeiculo.Locado:
                    veiculo.Locar();
                    break;
                case StatusVeiculo.EmPreparacao:
                    veiculo.Locar();
                    veiculo.RegistrarDevolucao(16_000, filial.IdFilial);
                    break;
                case StatusVeiculo.EmManutencao:
                    veiculo.IniciarManutencao(TipoManutencao.Preventiva, "revisão");
                    break;
            }

            // desativar depois da devolução: o carro sai da frota enquanto está na fila do pátio
            if (!ativo) veiculo.Desativar();

            armazem.Semear(veiculo);

            var notificador = new NotificadorService();
            var veiculos = new VeiculosRepositoryFake(armazem);

            var service = new VeiculoService(
                veiculos,
                new CategoriaVeiculosRepositoryFake(armazem),
                new FilialRepositoryFake(armazem),
                notificador);

            return new Cenario
            {
                Service = service,
                Notificador = notificador,
                Veiculos = veiculos,
                Veiculo = veiculo
            };
        }

        [Fact]
        public async Task Devolucao_tira_o_veiculo_da_oferta_e_poe_em_preparacao()
        {
            var cenario = Montar();

            Assert.Equal(StatusVeiculo.EmPreparacao, cenario.Veiculo.Status);
            Assert.False(cenario.Veiculo.Disponivel);
        }

        [Fact]
        public async Task Veiculo_em_preparacao_nao_entra_na_lista_de_disponiveis()
        {
            var cenario = Montar();

            var disponiveis = await cenario.Service.ObterDisponiveisAsync();

            Assert.Empty(disponiveis);
        }

        [Fact]
        public async Task Liberar_da_preparacao_devolve_o_veiculo_a_oferta_e_grava()
        {
            var cenario = Montar();

            var sucesso = await cenario.Service.LiberarDaPreparacaoAsync(cenario.Veiculo.IdVeiculo);

            Assert.True(sucesso);
            Assert.False(cenario.Notificador.TemNotificacao());
            Assert.Equal(StatusVeiculo.Disponivel, cenario.Veiculo.Status);
            Assert.True(cenario.Veiculo.Disponivel);
            Assert.Equal(1, cenario.Veiculos.Salvamentos);
        }

        [Fact]
        public async Task Liberado_da_preparacao_volta_a_aparecer_como_disponivel()
        {
            var cenario = Montar();

            await cenario.Service.LiberarDaPreparacaoAsync(cenario.Veiculo.IdVeiculo);
            var disponiveis = await cenario.Service.ObterDisponiveisAsync();

            Assert.Single(disponiveis);
        }

        [Fact]
        public async Task Liberar_veiculo_inativo_para_em_indisponivel_e_nao_na_oferta()
        {
            // RN-53: toda saída de indisponibilidade só devolve à oferta se o veículo estiver ativo
            var cenario = Montar(ativo: false);

            var sucesso = await cenario.Service.LiberarDaPreparacaoAsync(cenario.Veiculo.IdVeiculo);

            Assert.True(sucesso);
            Assert.Equal(StatusVeiculo.Indisponivel, cenario.Veiculo.Status);
            Assert.False(cenario.Veiculo.Disponivel);
        }

        [Theory]
        [InlineData(StatusVeiculo.Disponivel)]
        [InlineData(StatusVeiculo.Locado)]
        [InlineData(StatusVeiculo.EmManutencao)]
        public async Task Liberar_quem_nao_esta_em_preparacao_notifica_em_vez_de_lancar(StatusVeiculo status)
        {
            // Veiculo.LiberarDaPreparacao lança DomainException nesses casos; o serviço confere
            // antes e devolve notificação, que a Api traduz em ProblemDetails 4xx em vez de 500
            var cenario = Montar(status);

            var sucesso = await cenario.Service.LiberarDaPreparacaoAsync(cenario.Veiculo.IdVeiculo);

            Assert.False(sucesso);
            Assert.Contains(cenario.Notificador.ObterNotificacoes(), n => n.Mensagem.Contains("preparação"));
            Assert.Equal(0, cenario.Veiculos.Salvamentos);
        }

        [Fact]
        public async Task Liberar_veiculo_inexistente_notifica()
        {
            var cenario = Montar();

            var sucesso = await cenario.Service.LiberarDaPreparacaoAsync(999);

            Assert.False(sucesso);
            Assert.Contains(cenario.Notificador.ObterNotificacoes(), n => n.Mensagem.Contains("não encontrado"));
            Assert.Equal(0, cenario.Veiculos.Salvamentos);
        }
    }
}
