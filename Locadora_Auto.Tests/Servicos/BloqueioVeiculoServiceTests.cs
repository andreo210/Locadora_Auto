using Locadora_Auto.Application.Configuration.Ultils.NotificadorServices;
using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Tests.Fabricas;
using Locadora_Auto.Tests.Fakes;
using Xunit;

namespace Locadora_Auto.Tests.Servicos
{
    /// <summary>
    /// A porta da RN-52. O domínio já recusa bloqueio sem prazo ou sem responsável — o que se fixa
    /// aqui é que a recusa sai pelo <c>INotificadorService</c>, e não como <c>DomainException</c>:
    /// ela é <c>internal</c>, não está no <c>ExceptionProblemFactory</c> e escaparia como 500.
    /// </summary>
    public class BloqueioVeiculoServiceTests
    {
        private sealed class Cenario
        {
            public required VeiculoService Service { get; init; }
            public required NotificadorService Notificador { get; init; }
            public required Veiculo Veiculo { get; init; }
            public required Funcionario Funcionario { get; init; }
        }

        private static DateTime Amanha => DateTime.UtcNow.AddDays(1);

        private static Cenario Montar(StatusVeiculo status = StatusVeiculo.Disponivel)
        {
            var armazem = new ArmazemFake();

            var categoria = Fabrica.Categoria();
            armazem.Semear(categoria);

            var filial = Fabrica.Filial();
            armazem.Semear(filial);

            var funcionario = Fabrica.Funcionario();
            armazem.Semear(funcionario);

            var veiculo = Fabrica.Veiculo(categoria.Id, filial.IdFilial);

            // o caminho é sempre o real: AplicarStatus é privado de propósito
            switch (status)
            {
                case StatusVeiculo.Locado:
                    veiculo.Locar(Fabrica.Contrato());
                    break;
                case StatusVeiculo.EmPreparacao:
                    var contrato = Fabrica.Contrato();
                    veiculo.Locar(contrato);
                    veiculo.RegistrarDevolucao(16_000, filial.IdFilial, contrato);
                    break;
                case StatusVeiculo.EmManutencao:
                    veiculo.IniciarManutencao(TipoManutencao.Preventiva, "revisão");
                    break;
            }

            armazem.Semear(veiculo);

            var notificador = new NotificadorService();

            var service = Fabrica.VeiculoService(armazem, notificador);

            return new Cenario
            {
                Service = service,
                Notificador = notificador,
                Veiculo = veiculo,
                Funcionario = funcionario
            };
        }

        private static BloquearVeiculoDto Dto(
            Funcionario responsavel,
            MotivoBloqueio motivo = MotivoBloqueio.Documental,
            DateTime? prazo = null) => new()
            {
                IdMotivo = (int)motivo,
                DataPrevistaLiberacao = prazo ?? DateTime.UtcNow.AddDays(1),
                IdFuncionarioResponsavel = responsavel.IdFuncionario,
                Observacao = "licenciamento vencido"
            };

        [Fact]
        public async Task Bloqueio_valido_tira_o_carro_da_oferta()
        {
            var cenario = Montar();

            var bloqueio = await cenario.Service.BloquearAsync(cenario.Veiculo.IdVeiculo, Dto(cenario.Funcionario));

            Assert.False(cenario.Notificador.TemNotificacao());
            Assert.NotNull(bloqueio);
            Assert.True(bloqueio!.EmAberto);
            Assert.Equal(StatusVeiculo.Bloqueado, cenario.Veiculo.Status);
            Assert.False(cenario.Veiculo.Disponivel);
        }

        [Fact]
        public async Task Prazo_no_passado_notifica_em_vez_de_estourar()
        {
            var cenario = Montar();

            var bloqueio = await cenario.Service.BloquearAsync(
                cenario.Veiculo.IdVeiculo,
                Dto(cenario.Funcionario, prazo: DateTime.UtcNow.AddHours(-1)));

            Assert.Null(bloqueio);
            Assert.True(cenario.Notificador.TemNotificacao());
            Assert.Equal(StatusVeiculo.Disponivel, cenario.Veiculo.Status);
        }

        [Fact]
        public async Task Responsavel_inexistente_notifica()
        {
            var cenario = Montar();
            var dto = Dto(cenario.Funcionario);
            dto.IdFuncionarioResponsavel = 999;

            var bloqueio = await cenario.Service.BloquearAsync(cenario.Veiculo.IdVeiculo, dto);

            Assert.Null(bloqueio);
            Assert.True(cenario.Notificador.TemNotificacao());
        }

        [Fact]
        public async Task Motivo_fora_do_enum_notifica()
        {
            var cenario = Montar();
            var dto = Dto(cenario.Funcionario);
            dto.IdMotivo = 99;

            var bloqueio = await cenario.Service.BloquearAsync(cenario.Veiculo.IdVeiculo, dto);

            Assert.Null(bloqueio);
            Assert.True(cenario.Notificador.TemNotificacao());
        }

        [Fact]
        public async Task Veiculo_em_manutencao_notifica_em_vez_de_estourar()
        {
            var cenario = Montar(StatusVeiculo.EmManutencao);

            var bloqueio = await cenario.Service.BloquearAsync(cenario.Veiculo.IdVeiculo, Dto(cenario.Funcionario));

            Assert.Null(bloqueio);
            Assert.True(cenario.Notificador.TemNotificacao());
            Assert.Equal(StatusVeiculo.EmManutencao, cenario.Veiculo.Status);
        }

        [Fact]
        public async Task Segundo_bloqueio_em_aberto_notifica()
        {
            var cenario = Montar();
            await cenario.Service.BloquearAsync(cenario.Veiculo.IdVeiculo, Dto(cenario.Funcionario));

            var segundo = await cenario.Service.BloquearAsync(
                cenario.Veiculo.IdVeiculo, Dto(cenario.Funcionario, MotivoBloqueio.Comercial));

            Assert.Null(segundo);
            Assert.True(cenario.Notificador.TemNotificacao());
            Assert.Single(cenario.Veiculo.Bloqueios);
        }

        [Fact]
        public async Task Liberar_devolve_a_oferta_e_encerra_o_bloqueio()
        {
            var cenario = Montar();
            var bloqueio = await cenario.Service.BloquearAsync(cenario.Veiculo.IdVeiculo, Dto(cenario.Funcionario));

            var liberado = await cenario.Service.LiberarBloqueioAsync(
                cenario.Veiculo.IdVeiculo, bloqueio!.IdBloqueioVeiculo);

            Assert.True(liberado);
            Assert.False(cenario.Notificador.TemNotificacao());
            Assert.Equal(StatusVeiculo.Disponivel, cenario.Veiculo.Status);
            Assert.False(cenario.Veiculo.TemBloqueioEmAberto());
        }

        [Fact]
        public async Task Liberar_veiculo_que_nao_esta_bloqueado_notifica()
        {
            var cenario = Montar();

            var liberado = await cenario.Service.LiberarBloqueioAsync(cenario.Veiculo.IdVeiculo, 1);

            Assert.False(liberado);
            Assert.True(cenario.Notificador.TemNotificacao());
        }

        [Fact]
        public async Task Veiculo_bloqueado_sai_da_lista_de_disponiveis()
        {
            var cenario = Montar();
            await cenario.Service.BloquearAsync(cenario.Veiculo.IdVeiculo, Dto(cenario.Funcionario));

            var disponiveis = await cenario.Service.ObterDisponiveisAsync();

            Assert.Empty(disponiveis);
        }

        /// <summary>
        /// A guarda de <c>Ativar()</c> só funciona se o serviço carregar a coleção de bloqueios:
        /// não há lazy loading no contexto, então sem o Include ela viria vazia e a reativação
        /// devolveria à venda um carro bloqueado — em silêncio, que é o pior tipo de furo.
        /// </summary>
        [Fact]
        public async Task Reativar_veiculo_bloqueado_nao_o_devolve_a_oferta()
        {
            var cenario = Montar();
            await cenario.Service.BloquearAsync(cenario.Veiculo.IdVeiculo, Dto(cenario.Funcionario));
            await cenario.Service.DesativarAsync(cenario.Veiculo.IdVeiculo);

            var reativado = await cenario.Service.AtivarAsync(cenario.Veiculo.IdVeiculo);

            Assert.True(reativado);
            Assert.True(cenario.Veiculo.Ativo);
            Assert.Equal(StatusVeiculo.Bloqueado, cenario.Veiculo.Status);
            Assert.True(cenario.Veiculo.TemBloqueioEmAberto());

            var disponiveis = await cenario.Service.ObterDisponiveisAsync();
            Assert.Empty(disponiveis);
        }

        [Fact]
        public async Task Leitura_traz_abertos_e_encerrados_com_o_vencimento_calculado()
        {
            var cenario = Montar();
            var bloqueio = await cenario.Service.BloquearAsync(cenario.Veiculo.IdVeiculo, Dto(cenario.Funcionario));

            var lista = await cenario.Service.ObterBloqueiosAsync(cenario.Veiculo.IdVeiculo);

            var unico = Assert.Single(lista);
            Assert.Equal(bloqueio!.IdBloqueioVeiculo, unico.IdBloqueioVeiculo);
            Assert.True(unico.EmAberto);
            Assert.False(unico.Vencido);
            Assert.Equal(nameof(StatusVeiculo.Disponivel), unico.StatusAnterior);
        }
    }
}
