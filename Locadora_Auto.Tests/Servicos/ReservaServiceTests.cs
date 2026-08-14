using Locadora_Auto.Application.Configuration.Ultils.NotificadorServices;
using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Application.Services.ReservaServices;
using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Tests.Fabricas;
using Locadora_Auto.Tests.Fakes;
using Xunit;

namespace Locadora_Auto.Tests.Servicos
{
    /// <summary>
    /// Reserva exercita o caso mais completo da arquitetura: a escrita atravessa a raiz do
    /// agregado (<c>Clientes.ReservarVeiculo</c>) e as regras que a entidade recusaria com
    /// exceção precisam chegar ao usuário como notificação.
    /// </summary>
    public class ReservaServiceTests
    {
        private const int IdCliente = 1;
        private const int IdCategoria = 1;
        private const int IdFilial = 1;

        private sealed class Cenario
        {
            public required ReservaService Service { get; init; }
            public required NotificadorService Notificador { get; init; }
            public required ClienteRepositoryFake Clientes { get; init; }
            public required ReservaRepositoryFake Reservas { get; init; }
            public required Clientes Cliente { get; init; }
        }

        /// <summary>
        /// Cenário mínimo para uma reserva passar: cliente ativo, categoria e filial existentes
        /// e pelo menos um veículo disponível na combinação categoria + filial.
        /// </summary>
        private static Cenario Montar(bool clienteAtivo = true, int veiculosDisponiveis = 1)
        {
            var armazem = new ArmazemFake();

            var cliente = Fabrica.Cliente(ativo: clienteAtivo);
            armazem.Semear(cliente);

            var categoria = Fabrica.Categoria();
            armazem.Semear(categoria);

            var filial = Fabrica.Filial();
            armazem.Semear(filial);

            for (var i = 0; i < veiculosDisponiveis; i++)
                armazem.Semear(Fabrica.Veiculo(categoria.Id, filial.IdFilial, $"ABC1D2{i}"));

            var notificador = new NotificadorService();
            var reservas = new ReservaRepositoryFake(armazem);
            var clientes = new ClienteRepositoryFake(armazem);

            var service = new ReservaService(
                reservas,
                clientes,
                new CategoriaVeiculosRepositoryFake(armazem),
                new FilialRepositoryFake(armazem),
                new VeiculosRepositoryFake(armazem),
                new LocacaoRepositoryFake(armazem),
                notificador);

            return new Cenario
            {
                Service = service,
                Notificador = notificador,
                Clientes = clientes,
                Reservas = reservas,
                Cliente = cliente
            };
        }

        private static CriarReservaDto Dto(int idCliente = IdCliente, int diasAteInicio = 3, int diasAteFim = 6)
            => new()
            {
                IdCliente = idCliente,
                IdCategoriaVeiculo = IdCategoria,
                IdFilial = IdFilial,
                DataInicio = Fabrica.DaquiADias(diasAteInicio),
                DataFim = Fabrica.DaquiADias(diasAteFim)
            };

        [Fact]
        public async Task Criar_valido_registra_a_reserva_no_cliente()
        {
            var cenario = Montar();

            var resultado = await cenario.Service.CriarAsync(Dto());

            Assert.False(cenario.Notificador.TemNotificacao());
            Assert.NotNull(resultado);
            Assert.Equal(nameof(StatusReserva.Reservado), resultado!.Status);
            Assert.Single(cenario.Cliente.Reservas);
            Assert.Equal(1, cenario.Clientes.Salvamentos);
        }

        [Fact]
        public async Task Criar_para_cliente_inexistente_notifica()
        {
            var cenario = Montar();

            var resultado = await cenario.Service.CriarAsync(Dto(idCliente: 999));

            Assert.Null(resultado);
            Assert.Contains(cenario.Notificador.ObterNotificacoes(), n => n.Mensagem.Contains("Cliente"));
            Assert.Equal(0, cenario.Clientes.Salvamentos);
        }

        [Fact]
        public async Task Criar_para_cliente_inativo_notifica()
        {
            var cenario = Montar(clienteAtivo: false);

            var resultado = await cenario.Service.CriarAsync(Dto());

            Assert.Null(resultado);
            Assert.Contains(cenario.Notificador.ObterNotificacoes(), n => n.Mensagem.Contains("inativo"));
            Assert.Empty(cenario.Cliente.Reservas);
        }

        [Fact]
        public async Task Criar_com_data_no_passado_notifica_em_vez_de_lancar()
        {
            var cenario = Montar();

            // a entidade lançaria InvalidOperationException; o serviço barra antes e notifica
            var resultado = await cenario.Service.CriarAsync(Dto(diasAteInicio: -2, diasAteFim: 5));

            Assert.Null(resultado);
            Assert.True(cenario.Notificador.TemNotificacao());
            Assert.Empty(cenario.Cliente.Reservas);
        }

        [Fact]
        public async Task Criar_com_fim_antes_do_inicio_notifica()
        {
            var cenario = Montar();

            var resultado = await cenario.Service.CriarAsync(Dto(diasAteInicio: 8, diasAteFim: 4));

            Assert.Null(resultado);
            Assert.True(cenario.Notificador.TemNotificacao());
        }

        [Fact]
        public async Task Criar_sem_veiculo_na_categoria_e_filial_notifica()
        {
            var cenario = Montar(veiculosDisponiveis: 0);

            var resultado = await cenario.Service.CriarAsync(Dto());

            Assert.Null(resultado);
            Assert.Contains(cenario.Notificador.ObterNotificacoes(), n => n.Mensagem.Contains("disponíveis"));
        }

        [Fact]
        public async Task Criar_acima_da_frota_no_mesmo_periodo_notifica()
        {
            var cenario = Montar(veiculosDisponiveis: 1);

            var primeira = await cenario.Service.CriarAsync(Dto());
            var segunda = await cenario.Service.CriarAsync(Dto());

            Assert.NotNull(primeira);
            Assert.Null(segunda);
            Assert.Contains(cenario.Notificador.ObterNotificacoes(), n => n.Mensagem.Contains("período"));
        }

        [Fact]
        public async Task Cancelar_reserva_ativa_encerra_e_grava()
        {
            var cenario = Montar();
            var criada = await cenario.Service.CriarAsync(Dto());

            var sucesso = await cenario.Service.CancelarAsync(criada!.IdReserva);

            Assert.True(sucesso);
            Assert.False(cenario.Notificador.TemNotificacao());
            Assert.Equal(StatusReserva.Cancelado, cenario.Cliente.Reservas.Single().Status);
        }

        [Fact]
        public async Task Cancelar_duas_vezes_notifica_em_vez_de_estourar_a_excecao_do_dominio()
        {
            var cenario = Montar();
            var criada = await cenario.Service.CriarAsync(Dto());
            await cenario.Service.CancelarAsync(criada!.IdReserva);

            // Reserva.Cancelar lança DomainException nesse caso; o serviço confere o status antes
            // e devolve notificação, que a Api traduz em ProblemDetails 400 em vez de 500
            var segunda = await cenario.Service.CancelarAsync(criada.IdReserva);

            Assert.False(segunda);
            Assert.Contains(cenario.Notificador.ObterNotificacoes(), n => n.Mensagem.Contains("canceladas"));
        }

        [Fact]
        public async Task Cancelar_reserva_inexistente_notifica()
        {
            var cenario = Montar();

            var sucesso = await cenario.Service.CancelarAsync(999);

            Assert.False(sucesso);
            Assert.True(cenario.Notificador.TemNotificacao());
        }

        [Fact]
        public async Task Finalizar_encerra_a_reserva()
        {
            var cenario = Montar();
            var criada = await cenario.Service.CriarAsync(Dto());

            var sucesso = await cenario.Service.FinalizarAsync(criada!.IdReserva);

            Assert.True(sucesso);
            Assert.Equal(StatusReserva.Finalizado, cenario.Cliente.Reservas.Single().Status);
        }

        [Fact]
        public async Task Finalizar_reserva_ja_finalizada_notifica()
        {
            var cenario = Montar();
            var criada = await cenario.Service.CriarAsync(Dto());
            await cenario.Service.FinalizarAsync(criada!.IdReserva);

            var segunda = await cenario.Service.FinalizarAsync(criada.IdReserva);

            Assert.False(segunda);
            Assert.True(cenario.Notificador.TemNotificacao());
        }

        [Fact]
        public async Task Listagem_paginada_devolve_dto_com_os_metadados_da_pagina()
        {
            var cenario = Montar(veiculosDisponiveis: 5);
            for (var i = 0; i < 3; i++)
                await cenario.Service.CriarAsync(Dto(diasAteInicio: 3 + i, diasAteFim: 10 + i));

            var pagina = await cenario.Service.ObterTodosPaginadoAsync(
                new ConsultaPaginadaRequest { Pagina = 1, ItensPorPagina = 2 });

            Assert.Equal(3, pagina.Total);
            Assert.Equal(2, pagina.Items.Count);
            Assert.Equal(2, pagina.TotalPaginas);
            Assert.True(pagina.TemProximaPagina);
            Assert.All(pagina.Items, item => Assert.IsType<ReservaDto>(item));
        }

        [Fact]
        public async Task Listagem_paginada_ordena_pela_coluna_pedida()
        {
            var cenario = Montar(veiculosDisponiveis: 5);
            for (var i = 0; i < 3; i++)
                await cenario.Service.CriarAsync(Dto(diasAteInicio: 3 + i, diasAteFim: 20 - i));

            var pagina = await cenario.Service.ObterTodosPaginadoAsync(
                new ConsultaPaginadaRequest { OrdenarPor = "datafim", Direcao = "asc" });

            var datas = pagina.Items.Select(r => r.DataFim).ToList();
            Assert.Equal(datas.OrderBy(d => d), datas);
        }

        [Fact]
        public async Task Listagem_paginada_limita_itens_por_pagina()
        {
            var cenario = Montar(veiculosDisponiveis: 5);
            await cenario.Service.CriarAsync(Dto());

            // pedido absurdo vindo da query string é aparado antes de virar consulta
            var pagina = await cenario.Service.ObterTodosPaginadoAsync(
                new ConsultaPaginadaRequest { ItensPorPagina = 500_000 });

            Assert.Equal(ConsultaPaginadaRequest.MaximoItensPorPagina, pagina.ItensPorPagina);
        }

        [Fact]
        public async Task Expirar_vencidas_nao_mexe_em_reserva_futura()
        {
            var cenario = Montar();
            await cenario.Service.CriarAsync(Dto());

            var expiradas = await cenario.Service.ExpirarVencidasAsync();

            Assert.Equal(0, expiradas);
            Assert.Equal(StatusReserva.Reservado, cenario.Cliente.Reservas.Single().Status);
        }
    }
}
