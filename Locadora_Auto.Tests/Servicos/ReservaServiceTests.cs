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
        private static Cenario Montar(bool clienteAtivo = true, int veiculosDisponiveis = 1, int? tempoPreparacaoMinutos = null)
        {
            var armazem = new ArmazemFake();

            var cliente = Fabrica.Cliente(ativo: clienteAtivo);
            armazem.Semear(cliente);

            var categoria = Fabrica.Categoria();
            armazem.Semear(categoria);

            var filial = Fabrica.Filial(tempoPreparacaoMinutos: tempoPreparacaoMinutos);
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

        // ======================= disponibilidade (RN-46 / seção 9) =======================

        /// <summary>
        /// Contrato aberto no veículo indicado. Passa por <c>Locacao.Criar</c>, então o carro sai da
        /// oferta de verdade — é justamente essa a armadilha que a fórmula antiga não via: o veículo
        /// já não conta como disponível, e ainda assim a locação era subtraída de novo.
        /// </summary>
        private static Locacao SemearContrato(Cenario cenario, Veiculo veiculo, int diasAteInicio, int diasAteFim)
            => SemearContrato(cenario, veiculo, Fabrica.DaquiADias(diasAteInicio), Fabrica.DaquiADias(diasAteFim));

        private static Locacao SemearContrato(Cenario cenario, Veiculo veiculo, DateTime inicio, DateTime fim)
        {
            var contrato = Fabrica.Locacao(
                cliente: cenario.Cliente,
                veiculo: veiculo,
                dataInicio: inicio,
                dataFimPrevista: fim,
                idFilialRetirada: IdFilial);

            cenario.Reservas.Armazem.Semear(contrato);
            return contrato;
        }

        private static CriarReservaDto DtoEntre(DateTime inicio, DateTime fim)
            => new()
            {
                IdCliente = IdCliente,
                IdCategoriaVeiculo = IdCategoria,
                IdFilial = IdFilial,
                DataInicio = inicio,
                DataFim = fim
            };

        private static List<Veiculo> Frota(Cenario cenario) => cenario.Reservas.Armazem.Tabela<Veiculo>();

        [Fact]
        public async Task Disponibilidade_nao_desconta_o_mesmo_carro_duas_vezes()
        {
            // critério de aceite da seção 11: 5 veículos na categoria/filial, 2 em contrato aberto
            // que atravessa o período, nenhuma reserva — o resultado tem de ser 3, não 1
            var cenario = Montar(veiculosDisponiveis: 5);

            SemearContrato(cenario, Frota(cenario)[0], diasAteInicio: 2, diasAteFim: 8);
            SemearContrato(cenario, Frota(cenario)[1], diasAteInicio: 2, diasAteFim: 8);

            for (var i = 0; i < 3; i++)
                Assert.NotNull(await cenario.Service.CriarAsync(Dto()));

            var quarta = await cenario.Service.CriarAsync(Dto());

            Assert.Null(quarta);
            Assert.Contains(cenario.Notificador.ObterNotificacoes(), n => n.Mensagem.Contains("período"));
        }

        [Fact]
        public async Task Contrato_que_termina_antes_do_periodo_nao_bloqueia_a_venda()
        {
            // critério de aceite da seção 11: 1 veículo, contrato aberto que já passou, consulta
            // para daqui a 3 dias — resultado 1. É o contrato atrasado que ninguém fechou no
            // sistema e que, na fórmula antiga, tirava o carro da oferta para sempre.
            var cenario = Montar(veiculosDisponiveis: 1);

            SemearContrato(cenario, Frota(cenario).Single(), diasAteInicio: -9, diasAteFim: -5);

            var resultado = await cenario.Service.CriarAsync(Dto());

            Assert.NotNull(resultado);
            Assert.False(cenario.Notificador.TemNotificacao());
        }

        [Fact]
        public async Task Contrato_encerrado_no_periodo_nao_bloqueia_a_venda()
        {
            var cenario = Montar(veiculosDisponiveis: 1);
            var contrato = SemearContrato(cenario, Frota(cenario).Single(), diasAteInicio: 2, diasAteFim: 8);

            Fabrica.Devolver(
                Fabrica.Retirar(contrato),
                dataFimReal: Fabrica.DaquiADias(4),
                kmFinal: 16_000,
                filialDevolucao: IdFilial);

            // a venda é para depois da devolução real. O contrato devolvido continua ocupando o
            // período que de fato rodou (RN-61: só Finalizada e Cancelada soltam a placa), e é isso
            // que se quer: o que a devolução libera é o futuro, não o passado dela
            var resultado = await cenario.Service.CriarAsync(Dto(diasAteInicio: 5, diasAteFim: 8));

            Assert.NotNull(resultado);
            Assert.False(cenario.Notificador.TemNotificacao());
        }

        [Fact]
        public async Task Veiculo_em_preparacao_continua_contando_para_periodo_futuro()
        {
            // a fila do pátio se resolve em horas e a reserva é sempre futura (início no passado já
            // foi recusado antes daqui), então o carro devolvido não sai da oferta do período
            var cenario = Montar(veiculosDisponiveis: 1);
            var veiculo = Frota(cenario).Single();

            var contrato = Fabrica.Contrato();
            veiculo.Locar(contrato);
            veiculo.RegistrarDevolucao(16_000, IdFilial, contrato);
            Assert.Equal(StatusVeiculo.EmPreparacao, veiculo.Status);

            var resultado = await cenario.Service.CriarAsync(Dto());

            Assert.NotNull(resultado);
            Assert.False(cenario.Notificador.TemNotificacao());
        }

        [Fact]
        public async Task Veiculo_em_manutencao_sai_da_frota_ofertavel()
        {
            // impedimento estrutural: não tem data para voltar, então não é vendável no período
            var cenario = Montar(veiculosDisponiveis: 1);

            Frota(cenario).Single().IniciarManutencao(TipoManutencao.Preventiva, "revisão de 10.000 km");

            var resultado = await cenario.Service.CriarAsync(Dto());

            Assert.Null(resultado);
            Assert.Contains(cenario.Notificador.ObterNotificacoes(), n => n.Mensagem.Contains("disponíveis"));
        }

        [Fact]
        public async Task Veiculo_inativo_sai_da_frota_ofertavel()
        {
            var cenario = Montar(veiculosDisponiveis: 1);

            Frota(cenario).Single().Desativar();

            var resultado = await cenario.Service.CriarAsync(Dto());

            Assert.Null(resultado);
            Assert.Contains(cenario.Notificador.ObterNotificacoes(), n => n.Mensagem.Contains("disponíveis"));
        }

        [Fact]
        public async Task Contrato_de_outra_filial_nao_consome_a_oferta_desta()
        {
            var cenario = Montar(veiculosDisponiveis: 1);

            var outraFilial = Fabrica.Filial("Filial Aeroporto");
            cenario.Reservas.Armazem.Semear(outraFilial);

            var veiculoDeFora = Fabrica.Veiculo(IdCategoria, outraFilial.IdFilial, "XYZ9K88");
            cenario.Reservas.Armazem.Semear(veiculoDeFora);
            SemearContrato(cenario, veiculoDeFora, diasAteInicio: 2, diasAteFim: 8);

            var resultado = await cenario.Service.CriarAsync(Dto());

            Assert.NotNull(resultado);
            Assert.False(cenario.Notificador.TemNotificacao());
        }

        // ======================= tempo de preparação (RN-45/RN-46) =======================

        [Fact]
        public async Task Contrato_que_termina_dentro_do_preparo_ainda_bloqueia_a_venda()
        {
            // devolvido às 09:00 com preparo de 2h, o carro só entrega às 11:00 — uma reserva que
            // começa às 10:00 não pode contar com ele
            var cenario = Montar(tempoPreparacaoMinutos: 120);
            var inicio = Fabrica.DaquiADias(3);

            SemearContrato(cenario, Frota(cenario).Single(), inicio.AddDays(-2), inicio.AddHours(-1));

            var resultado = await cenario.Service.CriarAsync(DtoEntre(inicio, inicio.AddDays(2)));

            Assert.Null(resultado);
            Assert.Contains(cenario.Notificador.ObterNotificacoes(), n => n.Mensagem.Contains("período"));
        }

        [Fact]
        public async Task Contrato_que_termina_antes_do_preparo_nao_bloqueia_a_venda()
        {
            // devolvido 3h antes, com preparo de 2h: dá tempo, o carro está na oferta
            var cenario = Montar(tempoPreparacaoMinutos: 120);
            var inicio = Fabrica.DaquiADias(3);

            SemearContrato(cenario, Frota(cenario).Single(), inicio.AddDays(-2), inicio.AddHours(-3));

            var resultado = await cenario.Service.CriarAsync(DtoEntre(inicio, inicio.AddDays(2)));

            Assert.NotNull(resultado);
            Assert.False(cenario.Notificador.TemNotificacao());
        }

        [Fact]
        public async Task Filial_sem_preparacao_entrega_no_instante_da_devolucao()
        {
            // preparo zero é escolha declarada da filial: aí vale só o intervalo meio-aberto, e o
            // contrato que termina exatamente no início da reserva não bloqueia
            var cenario = Montar(tempoPreparacaoMinutos: 0);
            var inicio = Fabrica.DaquiADias(3);

            SemearContrato(cenario, Frota(cenario).Single(), inicio.AddDays(-2), inicio);

            var resultado = await cenario.Service.CriarAsync(DtoEntre(inicio, inicio.AddDays(2)));

            Assert.NotNull(resultado);
            Assert.False(cenario.Notificador.TemNotificacao());
        }

        [Fact]
        public async Task Preparo_maior_tira_da_oferta_o_que_o_preparo_menor_deixaria_passar()
        {
            // mesma frota, mesmo contrato, mesma janela: só o parâmetro da filial muda o resultado
            var inicioDaReserva = Fabrica.DaquiADias(3);
            var fimDoContrato = inicioDaReserva.AddHours(-2);

            var comPreparoCurto = Montar(tempoPreparacaoMinutos: 60);
            SemearContrato(comPreparoCurto, Frota(comPreparoCurto).Single(), inicioDaReserva.AddDays(-2), fimDoContrato);

            var comPreparoLongo = Montar(tempoPreparacaoMinutos: 240);
            SemearContrato(comPreparoLongo, Frota(comPreparoLongo).Single(), inicioDaReserva.AddDays(-2), fimDoContrato);

            var aceita = await comPreparoCurto.Service.CriarAsync(DtoEntre(inicioDaReserva, inicioDaReserva.AddDays(2)));
            var recusada = await comPreparoLongo.Service.CriarAsync(DtoEntre(inicioDaReserva, inicioDaReserva.AddDays(2)));

            Assert.NotNull(aceita);
            Assert.Null(recusada);
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
