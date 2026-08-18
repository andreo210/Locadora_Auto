using Locadora_Auto.Application.Configuration.Ultils.NotificadorServices;
using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Tests.Fabricas;
using Locadora_Auto.Tests.Fakes;
using Xunit;

namespace Locadora_Auto.Tests.Servicos
{
    /// <summary>
    /// A porta da RN-49. O domínio já garante o que é do veículo (estar ativo, estar disponível,
    /// não ter viagem em curso); o que o serviço acrescenta é a checagem das <b>duas filiais</b>,
    /// que são outro agregado e o <c>Veiculo</c> não enxerga.
    ///
    /// É aqui que a regra "mandar carro para filial que não recebe" é barrada — sem ela nasceria
    /// uma viagem que ninguém do outro lado vai confirmar, e o carro ficaria fora da oferta das
    /// duas filiais para sempre.
    /// </summary>
    public class TransferenciaVeiculoServiceTests
    {
        private sealed class Cenario
        {
            public required VeiculoService Service { get; init; }
            public required NotificadorService Notificador { get; init; }
            public required Veiculo Veiculo { get; init; }
            public required Filial Origem { get; init; }
            public required Filial Destino { get; init; }
            public required Funcionario Funcionario { get; init; }
        }

        private static DateTime Amanha => DateTime.UtcNow.AddDays(1);

        private static Cenario Montar(
            bool origemPermite = true,
            bool destinoPermite = true,
            bool destinoAtivo = true)
        {
            var armazem = new ArmazemFake();

            var categoria = Fabrica.Categoria();
            armazem.Semear(categoria);

            var origem = Fabrica.Filial("Origem");
            var destino = Fabrica.Filial("Destino");
            armazem.Semear(origem, destino);

            origem.DefinirPermiteTransferencia(origemPermite);
            destino.DefinirPermiteTransferencia(destinoPermite);
            if (!destinoAtivo) destino.Desativar();

            var funcionario = Fabrica.Funcionario();
            armazem.Semear(funcionario);

            var veiculo = Fabrica.Veiculo(categoria.Id, origem.IdFilial);
            armazem.Semear(veiculo);

            var notificador = new NotificadorService();

            var service = Fabrica.VeiculoService(armazem, notificador);

            return new Cenario
            {
                Service = service,
                Notificador = notificador,
                Veiculo = veiculo,
                Origem = origem,
                Destino = destino,
                Funcionario = funcionario
            };
        }

        private static EnviarTransferenciaDto Dto(Cenario cenario, int? idDestino = null) => new()
        {
            IdFilialDestino = idDestino ?? cenario.Destino.IdFilial,
            DataPrevistaChegada = DateTime.UtcNow.AddDays(1),
            IdFuncionarioResponsavel = cenario.Funcionario.IdFuncionario,
            Observacao = "remanejamento de pico"
        };

        [Fact]
        public async Task Envio_valido_tira_o_carro_da_oferta_da_origem()
        {
            var cenario = Montar();

            var transferencia = await cenario.Service.EnviarParaTransferenciaAsync(
                cenario.Veiculo.IdVeiculo, Dto(cenario));

            Assert.False(cenario.Notificador.TemNotificacao());
            Assert.NotNull(transferencia);
            Assert.Equal(StatusVeiculo.EmTransferencia, cenario.Veiculo.Status);
            Assert.Equal(cenario.Origem.IdFilial, cenario.Veiculo.FilialAtualId);
        }

        [Fact]
        public async Task Destino_que_nao_aceita_transferencia_notifica()
        {
            var cenario = Montar(destinoPermite: false);

            var transferencia = await cenario.Service.EnviarParaTransferenciaAsync(
                cenario.Veiculo.IdVeiculo, Dto(cenario));

            Assert.Null(transferencia);
            Assert.True(cenario.Notificador.TemNotificacao());
            Assert.Equal(StatusVeiculo.Disponivel, cenario.Veiculo.Status);
        }

        [Fact]
        public async Task Origem_que_nao_participa_do_remanejamento_notifica()
        {
            var cenario = Montar(origemPermite: false);

            var transferencia = await cenario.Service.EnviarParaTransferenciaAsync(
                cenario.Veiculo.IdVeiculo, Dto(cenario));

            Assert.Null(transferencia);
            Assert.True(cenario.Notificador.TemNotificacao());
        }

        [Fact]
        public async Task Destino_inativo_notifica()
        {
            var cenario = Montar(destinoAtivo: false);

            var transferencia = await cenario.Service.EnviarParaTransferenciaAsync(
                cenario.Veiculo.IdVeiculo, Dto(cenario));

            Assert.Null(transferencia);
            Assert.True(cenario.Notificador.TemNotificacao());
        }

        [Fact]
        public async Task Destino_inexistente_notifica()
        {
            var cenario = Montar();

            var transferencia = await cenario.Service.EnviarParaTransferenciaAsync(
                cenario.Veiculo.IdVeiculo, Dto(cenario, idDestino: 999));

            Assert.Null(transferencia);
            Assert.True(cenario.Notificador.TemNotificacao());
        }

        [Fact]
        public async Task Responsavel_inexistente_notifica()
        {
            var cenario = Montar();
            var dto = Dto(cenario);
            dto.IdFuncionarioResponsavel = 999;

            var transferencia = await cenario.Service.EnviarParaTransferenciaAsync(
                cenario.Veiculo.IdVeiculo, dto);

            Assert.Null(transferencia);
            Assert.True(cenario.Notificador.TemNotificacao());
        }

        [Fact]
        public async Task Veiculo_em_transito_sai_da_lista_de_disponiveis_das_duas_filiais()
        {
            // o coração da RN-49: contá-lo em qualquer das duas é overbooking involuntário
            var cenario = Montar();
            await cenario.Service.EnviarParaTransferenciaAsync(cenario.Veiculo.IdVeiculo, Dto(cenario));

            var naOrigem = await cenario.Service.ObterDisponiveisAsync(cenario.Origem.IdFilial);
            var noDestino = await cenario.Service.ObterDisponiveisAsync(cenario.Destino.IdFilial);

            Assert.Empty(naOrigem);
            Assert.Empty(noDestino);
        }

        [Fact]
        public async Task Chegada_confirmada_poe_o_carro_na_oferta_do_destino()
        {
            var cenario = Montar();
            var transferencia = await cenario.Service.EnviarParaTransferenciaAsync(
                cenario.Veiculo.IdVeiculo, Dto(cenario));

            var chegou = await cenario.Service.ConfirmarChegadaTransferenciaAsync(
                cenario.Veiculo.IdVeiculo,
                transferencia!.IdTransferenciaVeiculo,
                new ChegadaTransferenciaDto { KmChegada = 15_400 });

            Assert.True(chegou);
            Assert.False(cenario.Notificador.TemNotificacao());

            var noDestino = await cenario.Service.ObterDisponiveisAsync(cenario.Destino.IdFilial);
            Assert.Single(noDestino);
        }

        [Fact]
        public async Task Chegada_com_km_retrocedido_notifica_em_vez_de_estourar()
        {
            var cenario = Montar();
            var transferencia = await cenario.Service.EnviarParaTransferenciaAsync(
                cenario.Veiculo.IdVeiculo, Dto(cenario));

            var chegou = await cenario.Service.ConfirmarChegadaTransferenciaAsync(
                cenario.Veiculo.IdVeiculo,
                transferencia!.IdTransferenciaVeiculo,
                new ChegadaTransferenciaDto { KmChegada = 1 });

            Assert.False(chegou);
            Assert.True(cenario.Notificador.TemNotificacao());
            Assert.Equal(StatusVeiculo.EmTransferencia, cenario.Veiculo.Status);
        }

        [Fact]
        public async Task Cancelar_devolve_o_carro_a_oferta_da_origem()
        {
            var cenario = Montar();
            var transferencia = await cenario.Service.EnviarParaTransferenciaAsync(
                cenario.Veiculo.IdVeiculo, Dto(cenario));

            var cancelou = await cenario.Service.CancelarTransferenciaAsync(
                cenario.Veiculo.IdVeiculo, transferencia!.IdTransferenciaVeiculo);

            Assert.True(cancelou);

            var naOrigem = await cenario.Service.ObterDisponiveisAsync(cenario.Origem.IdFilial);
            Assert.Single(naOrigem);
        }

        [Fact]
        public async Task Confirmar_chegada_de_veiculo_que_nao_esta_em_transito_notifica()
        {
            var cenario = Montar();

            var chegou = await cenario.Service.ConfirmarChegadaTransferenciaAsync(
                cenario.Veiculo.IdVeiculo, 1, new ChegadaTransferenciaDto { KmChegada = 15_400 });

            Assert.False(chegou);
            Assert.True(cenario.Notificador.TemNotificacao());
        }
    }
}
