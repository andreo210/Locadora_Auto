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
    /// A locação é quem move o ciclo do ativo: abrir contrato consome a placa, fechar devolve o
    /// carro para a fila do pátio e cancelar o devolve direto à oferta. O que estes testes fixam é
    /// que essas transições acontecem pelo serviço e que a recusa chega como notificação — as
    /// guardas equivalentes dentro de <c>Veiculo</c> são <c>DomainException</c>, que viraria 500.
    /// </summary>
    public class LocacaoServiceTests
    {
        private const int KmDoVeiculo = 15_000;

        private sealed class Cenario
        {
            public required LocacaoService Service { get; init; }
            public required NotificadorService Notificador { get; init; }
            public required LocacaoRepositoryFake Locacoes { get; init; }
            public required Clientes Cliente { get; init; }
            public required Veiculo Veiculo { get; init; }
            public required Funcionario Funcionario { get; init; }
            public required Filial Filial { get; init; }
        }

        private static Cenario Montar(bool veiculoAtivo = true, StatusVeiculo status = StatusVeiculo.Disponivel)
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

            switch (status)
            {
                case StatusVeiculo.Locado:
                    veiculo.Locar(Fabrica.Contrato());
                    break;
                case StatusVeiculo.EmPreparacao:
                    var contrato = Fabrica.Contrato();
                    veiculo.Locar(contrato);
                    veiculo.RegistrarDevolucao(KmDoVeiculo, filial.IdFilial, contrato);
                    break;
                case StatusVeiculo.EmManutencao:
                    veiculo.IniciarManutencao(TipoManutencao.Preventiva, "revisão");
                    break;
            }

            if (!veiculoAtivo) veiculo.Desativar();

            armazem.Semear(veiculo);

            var notificador = new NotificadorService();
            var locacoes = new LocacaoRepositoryFake(armazem);

            var service = new LocacaoService(
                locacoes,
                new ClienteRepositoryFake(armazem),
                new VeiculosRepositoryFake(armazem),
                new ReservaRepositoryFake(armazem),
                new VistoriaRepositoryFake(armazem),
                new FilialRepositoryFake(armazem),
                new SeguroRepositoryFake(armazem),
                new AdicionalRepositoryFake(armazem),
                new LocacaoSeguroRepositoryFake(armazem),
                new FuncionarioRepositoryFake(armazem),
                new UploadDownloadFileServiceFake(),
                notificador);

            return new Cenario
            {
                Service = service,
                Notificador = notificador,
                Locacoes = locacoes,
                Cliente = cliente,
                Veiculo = veiculo,
                Funcionario = funcionario,
                Filial = filial
            };
        }

        private static CriarLocacaoDto Dto(Cenario cenario)
            => new()
            {
                IdCliente = cenario.Cliente.IdCliente,
                IdVeiculo = cenario.Veiculo.IdVeiculo,
                IdFuncionario = cenario.Funcionario.IdFuncionario,
                IdFilialRetirada = cenario.Filial.IdFilial,
                DataInicio = DateTime.UtcNow,
                DataFimPrevista = Fabrica.DaquiADias(3),
                KmInicial = KmDoVeiculo,
                ValorPrevisto = 450m
            };

        // ======================= abertura =======================

        [Fact]
        public async Task Criar_locacao_consome_a_placa_e_tira_o_veiculo_da_oferta()
        {
            var cenario = Montar();

            var resultado = await cenario.Service.CriarAsync(Dto(cenario));

            Assert.False(cenario.Notificador.TemNotificacao());
            Assert.NotNull(resultado);
            Assert.Equal(nameof(StatusLocacao.Criada), resultado!.Status);
            Assert.Equal(StatusVeiculo.Locado, cenario.Veiculo.Status);
            Assert.False(cenario.Veiculo.Disponivel);
            Assert.Equal(1, cenario.Locacoes.Salvamentos);
        }

        [Theory]
        [InlineData(StatusVeiculo.Locado)]
        [InlineData(StatusVeiculo.EmPreparacao)]
        [InlineData(StatusVeiculo.EmManutencao)]
        public async Task Criar_com_veiculo_fora_da_oferta_notifica_em_vez_de_lancar(StatusVeiculo status)
        {
            // Veiculo.Locar lança DomainException nesses três casos
            var cenario = Montar(status: status);

            var resultado = await cenario.Service.CriarAsync(Dto(cenario));

            Assert.Null(resultado);
            Assert.Contains(cenario.Notificador.ObterNotificacoes(), n => n.Mensagem.Contains("não está disponível"));
            Assert.Equal(0, cenario.Locacoes.Salvamentos);
        }

        [Fact]
        public async Task Criar_com_veiculo_inativo_notifica()
        {
            var cenario = Montar(veiculoAtivo: false);

            var resultado = await cenario.Service.CriarAsync(Dto(cenario));

            Assert.Null(resultado);
            Assert.Contains(cenario.Notificador.ObterNotificacoes(), n => n.Mensagem.Contains("inativo"));
        }

        [Fact]
        public async Task Criar_com_veiculo_inexistente_notifica_e_nao_estoura()
        {
            var cenario = Montar();
            var dto = Dto(cenario);
            dto.IdVeiculo = 999;

            var resultado = await cenario.Service.CriarAsync(dto);

            Assert.Null(resultado);
            Assert.Contains(cenario.Notificador.ObterNotificacoes(), n => n.Mensagem.Contains("Veículo não encontrado"));
        }

        [Fact]
        public async Task Criar_sem_reserva_e_locacao_de_balcao_valida()
        {
            // idReserva ausente é o caminho do walk-in: não pode virar busca por reserva nenhuma
            var cenario = Montar();
            var dto = Dto(cenario);
            dto.idReserva = null;

            var resultado = await cenario.Service.CriarAsync(dto);

            Assert.NotNull(resultado);
            Assert.False(cenario.Notificador.TemNotificacao());
        }

        [Fact]
        public async Task Criar_com_reserva_inexistente_notifica()
        {
            var cenario = Montar();
            var dto = Dto(cenario);
            dto.idReserva = 999;

            var resultado = await cenario.Service.CriarAsync(dto);

            Assert.Null(resultado);
            Assert.Contains(cenario.Notificador.ObterNotificacoes(), n => n.Mensagem.Contains("Reserva não encontrada"));
        }

        // ======================= sobreposição de período =======================

        /// <summary>
        /// Põe no armazém um contrato do mesmo veículo e devolve a placa à oferta. O estado
        /// resultante — carro disponível com contrato não encerrado em aberto — é exatamente o que
        /// a guarda de status <b>não</b> vê: ele aparece por linha anterior à regra, por correção
        /// de status à mão e pelas duas requisições simultâneas que passam juntas pela checagem.
        ///
        /// Com <paramref name="encerrado"/>, o contrato é cancelado pelo caminho do domínio, que
        /// grava status terminal e devolve a placa na mesma chamada.
        /// </summary>
        private static Locacao SemearContrato(Cenario cenario, DateTime inicio, DateTime fim, bool encerrado = false)
        {
            var contrato = Fabrica.Locacao(
                cliente: cenario.Cliente,
                veiculo: cenario.Veiculo,
                funcionario: cenario.Funcionario,
                dataInicio: inicio,
                dataFimPrevista: fim,
                idFilialRetirada: cenario.Filial.IdFilial);

            cenario.Locacoes.Armazem.Semear(contrato);

            // Fabrica.Locacao passa por Locacao.Criar, que consome a placa: sem devolvê-la, quem
            // recusaria a abertura seguinte seria o status, e não a regra em teste.
            if (encerrado) contrato.Cancelar();
            else cenario.Veiculo.ReverterLocacao(contrato);

            return contrato;
        }

        [Fact]
        public async Task Criar_com_contrato_sobreposto_no_mesmo_veiculo_e_recusado()
        {
            // RN-40: é o defeito que gera cliente no balcão sem carro
            var cenario = Montar();
            var inicio = DateTime.UtcNow;
            SemearContrato(cenario, inicio, inicio.AddDays(4));

            var dto = Dto(cenario);
            dto.DataInicio = inicio.AddDays(2);
            dto.DataFimPrevista = inicio.AddDays(3);

            var resultado = await cenario.Service.CriarAsync(dto);

            Assert.Null(resultado);
            Assert.Contains(cenario.Notificador.ObterNotificacoes(), n => n.Mensagem.Contains("já possui contrato no período"));
            Assert.Equal(0, cenario.Locacoes.Salvamentos);

            // a tentativa recusada não pode ter consumido a placa
            Assert.Equal(StatusVeiculo.Disponivel, cenario.Veiculo.Status);
        }

        [Theory]
        // começa dentro do contrato existente e termina depois
        [InlineData(2, 6)]
        // engole o contrato existente por inteiro
        [InlineData(-1, 9)]
        // cabe inteiro dentro do contrato existente
        [InlineData(1, 2)]
        public async Task Criar_recusa_qualquer_forma_de_cruzamento(int diasParaInicio, int diasParaFim)
        {
            var cenario = Montar();
            var inicio = DateTime.UtcNow;
            SemearContrato(cenario, inicio, inicio.AddDays(4));

            var dto = Dto(cenario);
            dto.DataInicio = inicio.AddDays(diasParaInicio);
            dto.DataFimPrevista = inicio.AddDays(diasParaFim);

            var resultado = await cenario.Service.CriarAsync(dto);

            Assert.Null(resultado);
            Assert.Contains(cenario.Notificador.ObterNotificacoes(), n => n.Mensagem.Contains("já possui contrato no período"));
        }

        [Fact]
        public async Task Criar_contrato_encostado_no_anterior_e_aceito()
        {
            // intervalo meio-aberto: devolver e retirar no mesmo instante é operação normal de balcão
            var cenario = Montar();
            var inicio = DateTime.UtcNow;
            SemearContrato(cenario, inicio, inicio.AddDays(4));

            var dto = Dto(cenario);
            dto.DataInicio = inicio.AddDays(4);
            dto.DataFimPrevista = inicio.AddDays(6);

            var resultado = await cenario.Service.CriarAsync(dto);

            Assert.NotNull(resultado);
            Assert.False(cenario.Notificador.TemNotificacao());
            Assert.Equal(StatusVeiculo.Locado, cenario.Veiculo.Status);
        }

        [Fact]
        public async Task Criar_ignora_contrato_ja_encerrado_no_mesmo_periodo()
        {
            // contrato encerrado soltou a placa: não ocupa período nenhum
            var cenario = Montar();
            var inicio = DateTime.UtcNow;
            SemearContrato(cenario, inicio, inicio.AddDays(4), encerrado: true);

            var dto = Dto(cenario);
            dto.DataInicio = inicio.AddDays(1);
            dto.DataFimPrevista = inicio.AddDays(3);

            var resultado = await cenario.Service.CriarAsync(dto);

            Assert.NotNull(resultado);
            Assert.False(cenario.Notificador.TemNotificacao());
        }

        [Fact]
        public async Task Estender_por_cima_do_contrato_seguinte_e_recusado()
        {
            // RN-42: o contrato seguinte do mesmo carro já foi vendido; esticar este invade aquele
            var cenario = Montar();
            var inicio = DateTime.UtcNow;
            SemearContrato(cenario, inicio.AddDays(5), inicio.AddDays(8));

            var dto = Dto(cenario);
            dto.DataInicio = inicio;
            dto.DataFimPrevista = inicio.AddDays(3);
            var criada = await cenario.Service.CriarAsync(dto);

            var resultado = await cenario.Service.AtualizarAsync(criada!.IdLocacao, new AtualizarLocacaoDto
            {
                DataFimPrevista = inicio.AddDays(6),
                KmInicial = KmDoVeiculo,
                ValorPrevisto = 900m
            });

            Assert.Null(resultado);
            Assert.Contains(cenario.Notificador.ObterNotificacoes(), n => n.Mensagem.Contains("já possui contrato no período"));

            // só a abertura gravou: a extensão recusada não pode ter chegado ao banco
            Assert.Equal(1, cenario.Locacoes.Salvamentos);
        }

        [Fact]
        public async Task Estender_sem_invadir_o_contrato_seguinte_e_aceito()
        {
            var cenario = Montar();
            var inicio = DateTime.UtcNow;
            SemearContrato(cenario, inicio.AddDays(5), inicio.AddDays(8));

            var dto = Dto(cenario);
            dto.DataInicio = inicio;
            dto.DataFimPrevista = inicio.AddDays(3);
            var criada = await cenario.Service.CriarAsync(dto);

            var resultado = await cenario.Service.AtualizarAsync(criada!.IdLocacao, new AtualizarLocacaoDto
            {
                DataFimPrevista = inicio.AddDays(4),
                KmInicial = KmDoVeiculo,
                ValorPrevisto = 600m
            });

            Assert.NotNull(resultado);
            Assert.False(cenario.Notificador.TemNotificacao());
            Assert.Equal(2, cenario.Locacoes.Salvamentos);
        }

        [Fact]
        public async Task Estender_nao_colide_com_o_proprio_contrato()
        {
            // sem ignorar a própria locação, toda extensão se recusaria sozinha
            var cenario = Montar();
            var criada = await cenario.Service.CriarAsync(Dto(cenario));

            var resultado = await cenario.Service.AtualizarAsync(criada!.IdLocacao, new AtualizarLocacaoDto
            {
                DataFimPrevista = Fabrica.DaquiADias(5),
                KmInicial = KmDoVeiculo,
                ValorPrevisto = 750m
            });

            Assert.NotNull(resultado);
            Assert.False(cenario.Notificador.TemNotificacao());
        }

        // ======================= fechamento =======================

        /// <summary>
        /// RN-57: a devolução exige o par de vistorias. O contrato semeado pelo serviço nasce em
        /// <c>Criada</c> e sem vistoria nenhuma, então todo teste de devolução precisa passar por
        /// aqui — que é exatamente a ordem do balcão: retirar, receber, vistoriar, devolver.
        /// </summary>
        private static Locacao PrepararParaDevolucao(Cenario cenario, int idLocacao, int kmFinal = 16_500)
        {
            var locacao = cenario.Locacoes.Armazem.Tabela<Locacao>().Single(l => l.IdLocacao == idLocacao);

            Fabrica.Retirar(locacao);
            locacao.RegistrarVistoria(1, TipoVistoria.Devolucao, NivelCombustivel.Meio, kmFinal, null);

            return locacao;
        }

        [Fact]
        public async Task Finalizar_sem_o_par_de_vistorias_notifica_em_vez_de_lancar()
        {
            // sem base comparável entre retirada e devolução não há cobrança de avaria, km nem
            // combustível que se sustente — o contrato não pode ser devolvido
            var cenario = Montar();
            var criada = await cenario.Service.CriarAsync(Dto(cenario));

            var sucesso = await cenario.Service.FinalizarAsync(
                criada!.IdLocacao, DateTime.UtcNow.AddDays(2), 16_500, 500m, cenario.Filial.IdFilial);

            Assert.False(sucesso);
            Assert.Contains(cenario.Notificador.ObterNotificacoes(), n => n.Mensagem.Contains("vistoria de retirada"));
            Assert.Equal(StatusVeiculo.Locado, cenario.Veiculo.Status);
        }

        [Fact]
        public async Task Finalizar_devolve_o_carro_e_fecha_a_conta()
        {
            var cenario = Montar();
            var criada = await cenario.Service.CriarAsync(Dto(cenario));
            var locacao = PrepararParaDevolucao(cenario, criada!.IdLocacao);

            var sucesso = await cenario.Service.FinalizarAsync(
                criada.IdLocacao, DateTime.UtcNow.AddDays(2), 16_500, 500m, cenario.Filial.IdFilial);

            Assert.True(sucesso);
            Assert.False(cenario.Notificador.TemNotificacao());

            // RN-58: a porta da Api ainda é uma só (o A11 separa), mas o ciclo já é o definitivo —
            // devolução e fechamento são dois atos, e sem pagamento sobra saldo a cobrar
            Assert.Equal(StatusLocacao.ComSaldoResidual, locacao.Status);
            Assert.Equal(500m, locacao.ValorFinal);
            Assert.Equal(500m, locacao.SaldoEmAberto());
        }

        [Fact]
        public async Task Finalizar_manda_o_veiculo_para_a_preparacao_e_avanca_km_e_filial()
        {
            var cenario = Montar();
            var criada = await cenario.Service.CriarAsync(Dto(cenario));
            PrepararParaDevolucao(cenario, criada!.IdLocacao);

            var outraFilial = Fabrica.Filial("Filial Aeroporto");
            cenario.Locacoes.Armazem.Semear(outraFilial);

            var sucesso = await cenario.Service.FinalizarAsync(
                criada!.IdLocacao, DateTime.UtcNow.AddDays(2), 16_500, 500m, outraFilial.IdFilial);

            Assert.True(sucesso);
            Assert.False(cenario.Notificador.TemNotificacao());

            // RN-44: devolvido não volta à oferta, entra na fila do pátio
            Assert.Equal(StatusVeiculo.EmPreparacao, cenario.Veiculo.Status);
            Assert.False(cenario.Veiculo.Disponivel);

            // RN-12/RN-47: o ativo passa a morar onde foi devolvido, com o odômetro da devolução
            Assert.Equal(16_500, cenario.Veiculo.KmAtual);
            Assert.Equal(outraFilial.IdFilial, cenario.Veiculo.FilialAtualId);
        }

        [Fact]
        public async Task Finalizar_com_km_retrocedendo_notifica_em_vez_de_lancar()
        {
            // RN-54: Veiculo.RegistrarKm lança DomainException aqui
            var cenario = Montar();
            var criada = await cenario.Service.CriarAsync(Dto(cenario));
            PrepararParaDevolucao(cenario, criada!.IdLocacao);

            var sucesso = await cenario.Service.FinalizarAsync(
                criada.IdLocacao, DateTime.UtcNow.AddDays(2), KmDoVeiculo - 500, 500m, cenario.Filial.IdFilial);

            Assert.False(sucesso);
            Assert.Contains(cenario.Notificador.ObterNotificacoes(), n => n.Mensagem.Contains("retroceder"));
            Assert.Equal(StatusVeiculo.Locado, cenario.Veiculo.Status);
            Assert.Equal(KmDoVeiculo, cenario.Veiculo.KmAtual);
        }

        [Fact]
        public async Task Finalizar_com_filial_de_devolucao_inexistente_notifica()
        {
            var cenario = Montar();
            var criada = await cenario.Service.CriarAsync(Dto(cenario));
            PrepararParaDevolucao(cenario, criada!.IdLocacao);

            var sucesso = await cenario.Service.FinalizarAsync(
                criada.IdLocacao, DateTime.UtcNow.AddDays(2), 16_500, 500m, 999);

            Assert.False(sucesso);
            Assert.Contains(cenario.Notificador.ObterNotificacoes(), n => n.Mensagem.Contains("Filial de devolução"));
            Assert.Equal(StatusVeiculo.Locado, cenario.Veiculo.Status);
        }

        [Fact]
        public async Task Finalizar_duas_vezes_notifica_em_vez_de_lancar()
        {
            var cenario = Montar();
            var criada = await cenario.Service.CriarAsync(Dto(cenario));
            PrepararParaDevolucao(cenario, criada!.IdLocacao);
            await cenario.Service.FinalizarAsync(
                criada.IdLocacao, DateTime.UtcNow.AddDays(2), 16_500, 500m, cenario.Filial.IdFilial);

            var segunda = await cenario.Service.FinalizarAsync(
                criada.IdLocacao, DateTime.UtcNow.AddDays(2), 17_000, 500m, cenario.Filial.IdFilial);

            Assert.False(segunda);
            Assert.True(cenario.Notificador.TemNotificacao());
            Assert.Equal(StatusVeiculo.EmPreparacao, cenario.Veiculo.Status);
        }

        // ======================= cancelamento =======================

        [Fact]
        public async Task Cancelar_devolve_o_veiculo_a_oferta_sem_passar_pela_preparacao()
        {
            // o contrato foi anulado e o carro não rodou: não há o que preparar
            var cenario = Montar();
            var criada = await cenario.Service.CriarAsync(Dto(cenario));

            var sucesso = await cenario.Service.CancelarAsync(criada!.IdLocacao);

            Assert.True(sucesso);
            Assert.Equal(StatusVeiculo.Disponivel, cenario.Veiculo.Status);
            Assert.True(cenario.Veiculo.Disponivel);
        }

        [Fact]
        public async Task Cancelar_locacao_ja_devolvida_notifica_em_vez_de_lancar()
        {
            var cenario = Montar();
            var criada = await cenario.Service.CriarAsync(Dto(cenario));
            PrepararParaDevolucao(cenario, criada!.IdLocacao);
            await cenario.Service.FinalizarAsync(
                criada.IdLocacao, DateTime.UtcNow.AddDays(2), 16_500, 500m, cenario.Filial.IdFilial);

            var sucesso = await cenario.Service.CancelarAsync(criada.IdLocacao);

            Assert.False(sucesso);
            Assert.True(cenario.Notificador.TemNotificacao());
            Assert.Equal(StatusVeiculo.EmPreparacao, cenario.Veiculo.Status);
        }
    }
}
