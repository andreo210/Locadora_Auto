using Locadora_Auto.Application.Configuration.Ultils.NotificadorServices;
using Locadora_Auto.Application.Configuration.Ultils.UploadArquivoServices;
using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Application.Models.Mappers;
using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Domain.IRepositorio;
using Microsoft.EntityFrameworkCore;
using Locadora_Auto.Application.Extensions;

namespace Locadora_Auto.Application.Services.LocacaoServices
{
    public class LocacaoService : ILocacaoService
    {
        private readonly ILocacaoRepository _locacaoRepository;
        private readonly IUploadDownloadFileService _uploadDownloadFileService;
        private readonly IReservaRepository _reservaRepository;
        private readonly ILocacaoSeguroRepository _locacaoSeguroRepository;
        private readonly IClienteRepository _clienteRepository;
        private readonly IVeiculosRepository _veiculoRepository;
        private readonly IVistoriaRepository _vistoriaRepository;
        private readonly ISeguroRepository _seguroRepository;
        private readonly IFilialRepository _filialRepository;
        private readonly IFuncionarioRepository _funcionarioRepository;
        private readonly IAdicionalRepository _adicionalRepository;
        private readonly IRecusaSobreposicaoRepository _recusaRepository;
        private readonly ICategoriaVeiculosRepository _categoriaRepository;
        private readonly INotificadorService _notificador;

        public LocacaoService(
            ILocacaoRepository locacaoRepository,
            IClienteRepository clienteRepository,
            IVeiculosRepository veiculoRepository,
            IReservaRepository reservaRepository,
            IVistoriaRepository vistoriaRepository,
            IFilialRepository filialRepository,
            ISeguroRepository seguroRepository,
            IAdicionalRepository adicionalRepository,
            ILocacaoSeguroRepository locacaoSeguroRepository,
            IFuncionarioRepository funcionarioRepository,
            IUploadDownloadFileService uploadDownloadFileService,
            IRecusaSobreposicaoRepository recusaRepository,
            ICategoriaVeiculosRepository categoriaRepository,
            INotificadorService notificador)
        {
            _locacaoRepository = locacaoRepository;
            _clienteRepository = clienteRepository;
            _veiculoRepository = veiculoRepository;
            _filialRepository = filialRepository;
            _funcionarioRepository = funcionarioRepository;
            _notificador = notificador;
            _seguroRepository = seguroRepository;
            _reservaRepository = reservaRepository;
            _locacaoSeguroRepository = locacaoSeguroRepository;
            _uploadDownloadFileService = uploadDownloadFileService;
            _recusaRepository = recusaRepository;
            _vistoriaRepository = vistoriaRepository;
            _adicionalRepository = adicionalRepository;
            _categoriaRepository = categoriaRepository;
        }

        #region Locacao
        public async Task<LocacaoDto?> CriarAsync(CriarLocacaoDto dto, CancellationToken ct = default)
        {
            if (dto.IdCliente == null) _notificador.Add("Cliente é obrigatório");
            if (dto.IdVeiculo == null) _notificador.Add("Veículo é obrigatório");
            if (dto.IdFilialRetirada == null) _notificador.Add("Filial de retirada é obrigatória");
            if (dto.DataInicio == null) _notificador.Add("Data de início é obrigatória");
            if (dto.DataFimPrevista == null) _notificador.Add("Data fim prevista é obrigatória");
            if (dto.KmInicial == null) _notificador.Add("Km inicial é obrigatório");

            if (_notificador.TemNotificacao())
                return null;

            // locação de balcão não nasce de reserva: só procura quando veio um id de verdade
            var idReserva = dto.idReserva.GetValueOrDefault();
            var reserva = idReserva > 0
                ? await _reservaRepository.ObterPrimeiroAsync(r => r.IdReserva == idReserva, null, true, ct)
                : null;

            // Os três precisam vir rastreados, por dois motivos distintos:
            //
            // o veículo, porque Locacao.Criar chama veiculo.Locar() — a saída do ativo da oferta e
            // o MovimentoVeiculo da RN-37 só chegam ao banco pela instância que o contexto segue;
            //
            // cliente e funcionário, porque Locacao.Criar os guarda como navegação e o Add da
            // locação pinta de Added todo o grafo que não estiver rastreado: o EF tentaria inserir
            // um cliente e um funcionário novos em vez de referenciar os que já existem.
            var veiculo = await _veiculoRepository.ObterPorIdAsync(dto.IdVeiculo.Value, true, ct);
            var cliente = await _clienteRepository.ObterPorIdAsync(dto.IdCliente.Value, true, ct);
            var funcionario = await _funcionarioRepository.ObterPorIdAsync(dto.IdFuncionario, true, ct);

            if (cliente == null) _notificador.Add("Cliente não encontrado");
            if (veiculo == null) _notificador.Add("Veículo não encontrado");
            if (funcionario == null) _notificador.Add("Funcionário não encontrado");
            if (idReserva > 0 && reserva == null) _notificador.Add("Reserva não encontrada");

            if (_notificador.TemNotificacao())
                return null;

            if (reserva != null)
            {
                if (reserva.Ativo == false)
                {
                    _notificador.Add("Essa reserva foi cancelada");
                    return null;
                }
                if (reserva.IdCliente != dto.IdCliente)
                    _notificador.Add("Reserva não pertence ao cliente informado");
                if (reserva.IdCategoria != veiculo!.IdCategoria)
                    _notificador.Add("Veículo não pertence à categoria da reserva");
                if (reserva.DataInicio != dto.DataInicio || reserva.DataFim != dto.DataFimPrevista)
                    _notificador.Add("Datas da locação não coincidem com as datas da reserva");
                if (reserva.IdFilial != dto.IdFilialRetirada)
                    _notificador.Add("Filial de retirada não coincide com a filial da reserva");
                if (reserva.Status != StatusReserva.Reservado)
                    _notificador.Add("Reserva não está em status reservado");
                if (_notificador.TemNotificacao())
                    return null;

                dto.DataInicio = reserva.DataInicio;
                dto.DataFimPrevista = reserva.DataFim;
                dto.IdFilialRetirada = reserva.IdFilial;
                dto.IdCliente = reserva.IdCliente;
            }

            if (!await _filialRepository.ExisteAsync(f => f.IdFilial == dto.IdFilialRetirada.Value, ct))
                _notificador.Add("Filial de retirada não encontrada");

            // RN-38/RN-43: quem decide é o status do ativo, não o booleano. Repetir aqui a guarda de
            // Veiculo.Locar() é o que transforma a invariante em ProblemDetails 4xx em vez de 500
            if (!veiculo!.Ativo)
                _notificador.Add("Veículo inativo não pode ser locado");
            else if (veiculo.Status != StatusVeiculo.Disponivel)
                _notificador.Add($"Veículo não está disponível para locação (situação atual: {veiculo.Status})");

            if (!cliente!.PodeLocar())
                _notificador.Add("Cliente não está habilitado para locar");

            if (dto.DataFimPrevista <= dto.DataInicio)
            {
                _notificador.Add("Data fim prevista deve ser posterior à data início");
            }
            else if (await ExisteContratoSobrepostoAsync(
                         veiculo.IdVeiculo, dto.DataInicio!.Value, dto.DataFimPrevista!.Value, ct: ct))
            {
                // RN-40: a guarda de status acima é um retrato de agora, e contrato é período. Um
                // carro que voltou à oferta (cancelamento, correção de status, linha antiga) pode
                // ter contrato futuro já vendido — é essa colisão que só a consulta enxerga.
                _notificador.Add("Veículo já possui contrato no período");

                await RegistrarRecusaAsync(
                    veiculo.IdVeiculo,
                    dto.IdFilialRetirada.Value,
                    dto.DataInicio.Value,
                    dto.DataFimPrevista.Value,
                    OrigemRecusa.Consulta,
                    ct: ct);
            }

            // RN-06: a diária vai congelada no contrato, e o preço sai da categoria do veículo
            // agora — não da leitura que a apuração faria no fechamento. É por repositório, e não
            // por `Include(v => v.Categoria)`, porque o Include só existe sobre provider do EF: a
            // navegação chegaria nula em qualquer chamador que não o pedisse, e o contrato nasceria
            // com diária zero — defeito que só apareceria semanas depois, na devolução.
            var categoria = await _categoriaRepository.ObterPorIdAsync(veiculo!.IdCategoria, false, ct);

            if (categoria == null)
                _notificador.Add("Categoria do veículo não encontrada");
            else if (categoria.ValorDiaria <= 0)
                _notificador.Add("Categoria do veículo está sem valor de diária cadastrado");

            if (_notificador.TemNotificacao())
                return null;

            var locacao = Locacao.Criar(
                cliente,
                veiculo!,
                funcionario!,
                reserva!,
                dto.IdFilialRetirada.Value,
                dto.DataInicio.Value,
                dto.DataFimPrevista.Value,
                dto.KmInicial.Value,
                dto.ValorPrevisto,
                categoria!.ValorDiaria
            );

            try
            {
                await _locacaoRepository.InserirSalvarAsync(locacao, ct);
            }
            catch (Exception ex) when (ViolacaoDeExclusao.EhSobreposicaoDeIntervalo(ex))
            {
                // RN-41: a consulta lá em cima é a mensagem amigável; esta é a garantia. Chegar
                // aqui significa concorrência real — dois atendentes passaram pela consulta antes
                // de qualquer um gravar, e o banco deixou exatamente um passar.
                await RegistrarRecusaAposFalhaAsync(
                    veiculo.IdVeiculo,
                    dto.IdFilialRetirada.Value,
                    dto.DataInicio.Value,
                    dto.DataFimPrevista.Value,
                    ct: ct);

                // relançada de propósito: quem traduz para 409 é o ExceptionProblemFactory, e
                // engolir aqui transformaria o conflito em 400 do notificador
                throw;
            }

            return locacao.ToDto();
        }
        public async Task<LocacaoDto?> AtualizarAsync(int id, AtualizarLocacaoDto dto, CancellationToken ct = default)
        {
            // rastreado: AtualizarDados altera a locação e o AtualizarSalvarAsync grava logo abaixo
            var locacao = await _locacaoRepository.ObterPorIdAsync(id, true, ct);
            if (locacao == null)
            {
                _notificador.Add("Locação não encontrada");
                return null;
            }

            // RN-42: estender é vender período novo do mesmo carro, então revalida como se fosse
            // abertura. Extensão aceita sem checar disponibilidade é o gerador nº 1 de falta de
            // carro na filial: o contrato seguinte já foi vendido e ninguém avisa o balcão.
            if (await ExisteContratoSobrepostoAsync(
                    locacao.IdVeiculo, locacao.DataInicio, dto.DataFimPrevista, locacao.IdLocacao, ct))
            {
                _notificador.Add("Veículo já possui contrato no período");

                // extensão recusada conta no mesmo indicador da abertura, mas marcada como
                // extensão: as duas dizem coisas diferentes ao gestor — abertura errada é escolha
                // de placa, extensão recusada é frota curta com o cliente já na mão
                await RegistrarRecusaAsync(
                    locacao.IdVeiculo,
                    locacao.IdFilialRetirada,
                    locacao.DataInicio,
                    dto.DataFimPrevista,
                    OrigemRecusa.Consulta,
                    locacao.IdLocacao,
                    ct);

                return null;
            }

            try
            {
                locacao.AtualizarDados(dto.DataFimPrevista, dto.KmInicial, dto.ValorPrevisto);
                await _locacaoRepository.AtualizarSalvarAsync(locacao, ct);
                return locacao.ToDto();
            }
            catch (InvalidOperationException ex)
            {
                _notificador.Add(ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Seção 12: registra a tentativa recusada, para o indicador "tentativas de sobreposição
        /// recusadas por filial" existir.
        ///
        /// A recusa em si funcionou — o balcão não vendeu carro que não existe. O que o número
        /// denuncia é <b>processo</b>: atendente escolhendo placa já comprometida, agenda de pátio
        /// desatualizada, frota curta na filial. Por isso ele é por filial e ao longo do tempo, e
        /// por isso é tabela e não linha de log.
        ///
        /// Falha ao gravar o indicador não pode derrubar a recusa: o cliente já recebeu a resposta
        /// certa, e perder uma linha de estatística é infinitamente melhor que transformar um 400
        /// explicado num 500. Por isso engole a exceção.
        /// </summary>
        private async Task RegistrarRecusaAsync(
            int idVeiculo,
            int idFilialRetirada,
            DateTime inicio,
            DateTime fim,
            OrigemRecusa origem,
            int? idLocacaoEmExtensao = null,
            CancellationToken ct = default)
        {
            try
            {
                var recusa = RecusaSobreposicao.Criar(
                    idVeiculo, idFilialRetirada, inicio, fim, origem, idLocacaoEmExtensao);

                await _recusaRepository.InserirSalvarAsync(recusa, ct);
            }
            catch
            {
                // indicador não derruba operação
            }
        }

        /// <summary>
        /// A mesma coisa, depois de o <c>SaveChanges</c> da abertura ter falhado (RN-41).
        ///
        /// O passo a mais é o <c>LimparRastreamento</c>: a locação recusada continua <c>Added</c> no
        /// contexto, e junto com ela o <c>Locar()</c> que já marcou o veículo. Gravar a recusa sem
        /// limpar mandaria os três de novo — bateria no mesmo <c>23P01</c> e, pior, poderia deixar
        /// o veículo <c>Locado</c> sem contrato. A operação já está perdida (a exceção é relançada
        /// logo em seguida), então descartar o rastreamento é o certo, não um atalho.
        /// </summary>
        private async Task RegistrarRecusaAposFalhaAsync(
            int idVeiculo,
            int idFilialRetirada,
            DateTime inicio,
            DateTime fim,
            CancellationToken ct = default)
        {
            try
            {
                _locacaoRepository.LimparRastreamento();
            }
            catch
            {
                // sem o descarte não dá para gravar nada; sai calado e o 409 segue seu caminho
                return;
            }

            await RegistrarRecusaAsync(idVeiculo, idFilialRetirada, inicio, fim, OrigemRecusa.Banco, ct: ct);
        }

        /// <summary>
        /// RN-60: o contrato passou do fim previsto e o carro continua na rua.
        ///
        /// É varredura porque atraso é fato do <b>relógio</b>, não de um clique: ninguém no balcão
        /// vai marcar como atrasado o contrato de um cliente que sumiu, e é justamente esse que
        /// interessa enxergar. Sem ela o contrato fica <c>EmAndamento</c> para sempre e o carro
        /// some dos indicadores de atraso — o mesmo defeito que a liberação automática da RN-45
        /// corrige do lado do pátio.
        ///
        /// Não notifica: é lote de agendador, e recusa individual aqui não tem para quem ser
        /// respondida. O que ela devolve é a contagem.
        ///
        /// <b>Sem tolerância ainda.</b> O doc 07 §9 recomenda 30 minutos de folga antes de a hora
        /// excedente correr, mas o parâmetro da casa é o backlog A3 e ainda não existe. Enquanto
        /// isso o corte é o instante do fim previsto, que é o lado conservador: marca cedo demais,
        /// nunca tarde demais — e <c>Atrasada</c> hoje não cobra nada, só torna o contrato visível.
        /// </summary>
        public async Task<int> MarcarAtrasadasAsync(CancellationToken ct = default)
        {
            var agora = DateTime.UtcNow;

            // o filtro já é o da regra, e não "tudo que está em andamento": a varredura roda a cada
            // poucos minutos e trazer a carteira inteira para a memória a cada volta seria o mesmo
            // erro que a paginação da trilha evita
            var candidatas = await _locacaoRepository.ObterAsync(
                filtro: l => l.Status == StatusLocacao.EmAndamento && l.DataFimPrevista < agora,
                rastreado: true,
                ct: ct);

            if (candidatas.Count == 0) return 0;

            // MarcarComoAtrasada tem a guarda dela; conferir o status depois da chamada é o que dá
            // a contagem real, mesmo padrão do ExpirarVencidasAsync da reserva
            var marcadas = 0;
            foreach (var locacao in candidatas)
            {
                locacao.MarcarComoAtrasada(agora);
                if (locacao.Status == StatusLocacao.Atrasada) marcadas++;
            }

            if (marcadas > 0)
                await _locacaoRepository.SalvarAsync(ct);

            return marcadas;
        }

        /// <summary>
        /// RN-40/RN-41: isto é a <b>mensagem amigável</b>, não a garantia. Duas requisições
        /// simultâneas passam pelas duas consultas antes de qualquer uma gravar — nenhum <c>if</c>
        /// no serviço resolve isso. Quem garante é a constraint <c>EXCLUDE</c> em
        /// <c>tb_locacao</c>, cuja violação chega como 409 pelo <c>ExceptionProblemFactory</c>.
        /// A consulta existe para o atendente ver a recusa antes de digitar o contrato inteiro.
        /// </summary>
        private Task<bool> ExisteContratoSobrepostoAsync(
            int idVeiculo,
            DateTime inicio,
            DateTime fim,
            int idLocacaoIgnorada = 0,
            CancellationToken ct = default)
            => _locacaoRepository.ExisteAsync(
                Locacao.Sobrepostas(idVeiculo, inicio, fim, idLocacaoIgnorada), ct);

        public async Task<bool> FinalizarAsync(int id, DateTime dataFimReal, int kmFinal, decimal valorFinal, int filialDevolucao, CancellationToken ct = default)
        {
            // as vistorias entram no Include por duas razões: a RN-57 exige o par delas para
            // aceitar a devolução, e AbrirManutencaoPorAvaria varre os danos da vistoria de
            // devolução — sem carregá-las, a avaria nunca virava ordem corretiva
            var locacao = await _locacaoRepository.ObterPrimeiroAsync(
                x => x.IdLocacao == id,
                incluir: q => q.Include(c => c.Veiculo)
                               .Include(c => c.Pagamentos)
                               .Include(c => c.Vistorias)
                                   .ThenInclude(v => v.Danos),
                rastreado: true);

            if (locacao == null)
            {
                _notificador.Add("Locação não encontrada");
                return false;
            }

            // RN-44/RN-54: Locacao.RegistrarDevolucao delega a devolução ao ativo, e as guardas de
            // Veiculo.RegistrarDevolucao são DomainException. Repetidas aqui, viram 4xx e não 500
            if (locacao.Veiculo.Status != StatusVeiculo.Locado)
                _notificador.Add($"Veículo da locação não está locado (situação atual: {locacao.Veiculo.Status})");

            if (kmFinal < locacao.Veiculo.KmAtual)
                _notificador.Add($"Quilometragem não pode retroceder: o veículo está com {locacao.Veiculo.KmAtual} km e foi informado {kmFinal} km");

            if (!await _filialRepository.ExisteAsync(f => f.IdFilial == filialDevolucao, ct))
                _notificador.Add("Filial de devolução não encontrada");

            // RN-57: sem o par de vistorias não há base comparável, e nada do que a apuração
            // cobraria se sustenta. O domínio recusa de qualquer forma; aqui a recusa vira mensagem
            if (!locacao.Vistorias.Any(v => v.Tipo == TipoVistoria.Retirada))
                _notificador.Add("Contrato sem vistoria de retirada não pode ser devolvido");

            if (!locacao.Vistorias.Any(v => v.Tipo == TipoVistoria.Devolucao))
                _notificador.Add("Registre a vistoria de devolução antes de encerrar a posse");

            if (_notificador.TemNotificacao())
                return false;

            try
            {
                // RN-58: são dois atos, não um. A devolução encerra a posse; o fechamento encerra o
                // contrato. Enquanto a apuração real não existir (backlog A5–A10), valorFinal chega
                // pronto de quem chama e o fechamento é provisório — mas o ciclo de vida já é o
                // definitivo, e o A11 só precisa separar esta chamada em duas portas da Api.
                locacao.RegistrarDevolucao(dataFimReal, kmFinal, filialDevolucao);
                locacao.Fechar(valorFinal);
                locacao.LiquidarSaldo();

                await _locacaoRepository.AtualizarSalvarAsync(locacao, ct);
                return true;
            }
            catch (InvalidOperationException ex)
            {
                _notificador.Add(ex.Message);
                return false;
            }
        }
        public async Task<bool> CancelarAsync(int id, CancellationToken ct = default)
        {
            var locacao = await ObterLocacao(id, ct);
            if (locacao == null)
            {
                _notificador.Add("Locação não encontrada");
                return false;
            }

            // Locacao.Cancelar devolve o carro à oferta por Veiculo.ReverterLocacao, que exige o
            // ativo locado — sem esta guarda a inconsistência viraria 500 em vez de mensagem
            if (locacao.Veiculo.Status != StatusVeiculo.Locado)
            {
                _notificador.Add($"Veículo da locação não está locado (situação atual: {locacao.Veiculo.Status})");
                return false;
            }

            try
            {
                locacao.Cancelar();
                await _locacaoRepository.AtualizarSalvarAsync(locacao, ct);
                return true;
            }
            catch (InvalidOperationException ex)
            {
                _notificador.Add(ex.Message);
                return false;
            }
        }
        #endregion Locacao

        #region Pagamento
        public async Task<bool> AdicionarPagamentoAsync(int id,AdicionarPagamentoDto pagamento, CancellationToken ct = default)
        {
            var locacao = await _locacaoRepository.ObterPrimeiroAsync(x => x.IdLocacao == id, incluir: q => q.Include(l => l.Pagamentos), rastreado: true, ct);

            if (locacao == null)
            {
                _notificador.Add("Locação não encontrada");
                return false;
            }

            locacao.AdicionarPagamento(pagamento.Valor, (FormaPagamento)pagamento.IdFormaPagamento);
            await _locacaoRepository.AtualizarSalvarAsync(locacao, ct);
            return true;
           
        }

        public async Task<bool> ConfirmarPagamentoAsync(int id, int idPagamento, CancellationToken ct = default)
        {
            var locacao = await _locacaoRepository.ObterPrimeiroAsync(x => x.IdLocacao == id, incluir: q => q.Include(l => l.Pagamentos), rastreado: true, ct);

            if (locacao == null)
            {
                _notificador.Add("Locação não encontrada");
                return false;
            }

            locacao.ConfirmarPagamento(idPagamento);
            await _locacaoRepository.AtualizarSalvarAsync(locacao, ct);
            return true;

        }

        public async Task<bool> CancelarPagamentoAsync(int id, int idPagamento,string motivo, CancellationToken ct = default)
        {
            var locacao = await _locacaoRepository.ObterPrimeiroAsync(x => x.IdLocacao == id, incluir: q => q.Include(l => l.Pagamentos), rastreado: true, ct);

            if (locacao == null)
            {
                _notificador.Add("Locação não encontrada");
                return false;
            }

            locacao.CancelarPagamento(idPagamento,motivo);
            await _locacaoRepository.AtualizarSalvarAsync(locacao, ct);
            return true;
        }

        public async Task<bool> MarcarComoFalhaAsync(int id, int idPagamento,CancellationToken ct = default)
        {
            var locacao = await _locacaoRepository.ObterPrimeiroAsync(x => x.IdLocacao == id, incluir: q => q.Include(l => l.Pagamentos), rastreado: true, ct);

            if (locacao == null)
            {
                _notificador.Add("Locação não encontrada");
                return false;
            }

            locacao.MarcarComoFalha(idPagamento);
            await _locacaoRepository.AtualizarSalvarAsync(locacao, ct);
            return true;
        }


        #endregion Pagamento

        #region Caucao
        public async Task<bool> AdicionarCalcaoAsync(int idLocacao, decimal valor, CancellationToken ct = default)
        {
            var locacao = await ObterLocacao(idLocacao, ct); ;
            if (locacao == null)
            {
                _notificador.Add("Locação não encontrada");
                return false;
            }

            locacao.RegistrarCaucao(valor);
            await _locacaoRepository.AtualizarSalvarAsync(locacao, ct);
            return true;
        }

        public async Task<bool> DevolverCalcaoAsync(int idLocacao, int idCaucao, CancellationToken ct = default)
        {
            var locacao = await ObterLocacao(idLocacao, ct); ;
            if (locacao == null)
            {
                _notificador.Add("Locação não encontrada");
                return false;
            }

            locacao.DevolverCaucao(idCaucao);
            await _locacaoRepository.AtualizarSalvarAsync(locacao, ct);
            return true;
        }

        public async Task<bool>BloquearCalcaoAsync(int idLocacao, int idCaucao, CancellationToken ct = default)
        {
            var locacao = await ObterLocacao(idLocacao, ct); ;
            if (locacao == null)
            {
                _notificador.Add("Locação não encontrada");
                return false;
            }
            locacao.BloquearCaucao(idCaucao);
            await _locacaoRepository.AtualizarSalvarAsync(locacao, ct);
            return true;
        }

        public async Task<bool> DeduzirCalcaoAsync(int idLocacao, int idCaucao,decimal valor, CancellationToken ct = default)
        {
            var locacao = await ObterLocacao(idLocacao, ct); ;
            if (locacao == null)
            {
                _notificador.Add("Locação não encontrada");
                return false;
            }
            locacao.DeduzirCaucao(idCaucao, valor);
            await _locacaoRepository.AtualizarSalvarAsync(locacao, ct);
            return true;
        }
        #endregion Caucao

        #region multas
        public async Task<bool> AdicionarMultaAsync(int idLocacao, CriarMultaDto dto, CancellationToken ct = default)
        {
            var locacao = await ObterLocacao(idLocacao, ct); ;
            if (locacao == null)
            {
                _notificador.Add("Locação não encontrada");
                return false;
            }
            locacao.AdicionarMulta((TipoMulta)dto.Tipo,dto.Valor);
            await _locacaoRepository.AtualizarSalvarAsync(locacao, ct);
            return true;
          
        }

        public async Task<bool> PagarMultaAsync(int idLocacao, int idMulta, CancellationToken ct = default)
        {
            var locacao = await ObterLocacao(idLocacao, ct); ;
            if (locacao == null)
            {
                _notificador.Add("Locação não encontrada");
                return false;
            }
            var multa = locacao.Multas.Where(m => m.IdMulta == idMulta).FirstOrDefault();
            if (multa == null)
            {
                _notificador.Add("Multa não encontrada");
                return false;
            }
            if (multa.Status != StatusMulta.Pendente)
            {
                _notificador.Add("Somente multas pendentes podem ser pagas");
                return false;
            }
            locacao.PagarMulta(idMulta);
            await _locacaoRepository.AtualizarSalvarAsync(locacao, ct);
            return true;

        }

        public async Task<bool> CancelarMultaAsync(int idLocacao, int idMulta, CancellationToken ct = default)
        {
            var locacao = await ObterLocacao(idLocacao, ct); ;
            if (locacao == null)
            {
                _notificador.Add("Locação não encontrada");
                return false;
            }
            var multa = locacao.Multas.Where(m => m.IdMulta == idMulta).FirstOrDefault();
            if (multa == null)
            {
                _notificador.Add("Multa não encontrada");
                return false;
            }
            if (multa.Status == StatusMulta.Paga)
            {
                _notificador.Add("Multa paga não pode ser cancelada, ja foi paga");
                return false;
            }

            locacao.CancelarMulta(idMulta);
            await _locacaoRepository.AtualizarSalvarAsync(locacao, ct);
            return true;

        }

        public async Task<bool> CompensarMultaAsync(int idLocacao, int idMulta,CancellationToken ct = default)
        {
            // 1. Buscar locação pelo repositório
            var locacao = await ObterLocacao(idLocacao,ct);
            if (locacao == null)
                _notificador.Add("Locação não encontrada");

            // 2. Delegar para o aggregate Locacao
            locacao.CompensarMultaComCaucao(idMulta);

            // 3. Persistir mudanças
            var atualiza =await _locacaoRepository.AtualizarSalvarAsync(locacao,ct);
            if(!atualiza)
            {
                _notificador.Add("Erro ao atualizar locação");
                return false;
            }
            return true;
        }
        #endregion multas

        #region Seguro
        public async Task<bool> AdicionarSeguroAsync(int idLocacao, int idSeguro, CancellationToken ct = default)
        {
            var locacao = await ObterLocacao(idLocacao, ct);
            if (locacao == null)
            {
                _notificador.Add("Locação não encontrada");
                return false;
            }
            var seguro = await _seguroRepository.ObterPorIdAsync(idSeguro, false, ct);
            if(seguro == null)
            {
                _notificador.Add("Seguro não encontrado");
                return false;
            }

            // RN-18/RN-25: o cadastro do seguro já estava carregado e era descartado — agora é dele
            // que saem a diária e a franquia congeladas no contrato. Reajustar a tabela de seguros
            // deixa de reescrever o teto de avaria de quem já assinou.
            locacao.AdicionarSeguro(idSeguro, seguro.ValorDiaria, seguro.Franquia);
            await _locacaoRepository.AtualizarSalvarAsync(locacao, ct);
            return true;
        }

        public async Task<bool> CancelarSeguroAsync(int idLocacao, int idLocacaoSeguro, CancellationToken ct = default)
        {
            var locacao = await ObterLocacao(idLocacao, ct);
            if (locacao == null)
            {
                _notificador.Add("Locação não encontrada");
                return false;
            }
            var locacaoSeguro = await _locacaoSeguroRepository.ObterPorIdAsync(idLocacaoSeguro, false, ct);
            if(locacaoSeguro == null)
            {
                _notificador.Add("Locação Seguro não encontrado");
                return false;
            }
            locacao.CancelarSeguro(idLocacaoSeguro); // substituir por busca real
            await _locacaoRepository.AtualizarSalvarAsync(locacao, ct);
            return true;
        }
        #endregion Seguro

        #region Leitura
        public async Task<LocacaoDto?> ObterPorIdAsync(int id, CancellationToken ct = default)
        {
            var locacao = await ObterLocacao(id, ct);
            if (locacao == null)
            {
                _notificador.Add("Locação não encontrada");
                return null;
            }
            return locacao.ToDto();
        }
        public async Task<IEnumerable<LocacaoDto>> ObterTodasAsync(CancellationToken ct = default)
        {
            var locacao = await _locacaoRepository.ObterAsync(
                incluir: q => q
                .Include(c => c.Veiculo)
                .Include(c => c.Cliente).ThenInclude(u => u.Usuario)
                .Include(c => c.Funcionario).ThenInclude(u => u.Usuario)
                .Include(c => c.Caucoes)
                .Include(m => m.Multas)
                .Include(m => m.Pagamentos)
                .Include(m => m.Seguros),
                rastreado: true);
            return locacao.ToDtoList();
        }
        private async Task<Locacao> ObterLocacao(int id, CancellationToken ct)
        {
            var locacao = await _locacaoRepository.ObterPrimeiroAsync(
                x => x.IdLocacao == id, 
                incluir: q => q
                .Include(c => c.Veiculo)
                .Include(c => c.Cliente).ThenInclude(u=>u.Usuario)
                .Include(c => c.Funcionario).ThenInclude(u => u.Usuario)
                .Include(c => c.Caucoes)
                .Include(m=>m.Multas)
                .Include(m => m.Pagamentos)
                .Include(m => m.Vistorias)
                .Include(m => m.Adicionais)
                .Include(m => m.Seguros),

                rastreado: true);
            return locacao!;
        }

        #endregion Leitura

        #region Vistoria

        public async Task<bool> RegistrarVistoriaAsync(int idLocacao, CriarVistoriaDto dto, CancellationToken ct = default)
        {
            var locacao = await ObterLocacao(idLocacao, ct);

            if (locacao == null)
            {
                _notificador.Add("Locação não encontrada");
                return false;
            }

            locacao.RegistrarVistoria(dto.IdFuncionario, (TipoVistoria)dto.Tipo,(NivelCombustivel)dto.NivelCombustivel,dto.KmVeiculo, dto.Observacoes);

            await _locacaoRepository.AtualizarSalvarAsync(locacao);
            return true;
        }

        public async Task<bool> RegistrarFotoVistoriaAsync(int id, EnviarFotoVistoriaDto dto, CancellationToken ct = default)
        {
            var locacao = await ObterLocacao(id, ct);
            if (locacao == null)
            {
                _notificador.Add("Locação não encontrada");
                return false;
            }
            var vistoria = locacao.Vistorias.Where(x=>x.IdVistoria == dto.IdVistoria).FirstOrDefault();
            if (vistoria == null)
            {
                _notificador.Add("Vistoria não encontrada");
                return false;
            }
            var fotos = await EnviarFoto(dto);
            locacao.RegistrarFoto(fotos,dto.IdVistoria);
            await _locacaoRepository.AtualizarSalvarAsync(locacao);
            return true;
        }

        public async Task<bool> RegistrarDanoVistoriaAsync(int id, CriarDanoDto dto, CancellationToken ct = default)
        {
            var locacao = await ObterLocacao(id, ct);
            if (locacao == null)
            {
                _notificador.Add("Locação não encontrada");
                return false;
            }
            var vistoria = locacao.Vistorias.FirstOrDefault(x => x.IdVistoria == dto.IdVistoria);
            if (vistoria == null)
            {
                _notificador.Add("Vistoria não encontrada");
                return false;
            }
            locacao.RegistrarDanoVistoria(dto.IdVistoria, dto.Descricao,(TipoDano)dto.codigoTipoDano, dto.ValorEstimado);
            return await _locacaoRepository.AtualizarSalvarAsync(locacao);
        }

        public async Task<bool> RemoverDanoVistoriaAsync(int id,RemoverDanoDto dto, CancellationToken ct = default)
        {
            var locacao = await ObterLocacao(id, ct);
            if (locacao == null)
            {
                _notificador.Add("Locação não encontrada");
                return false;
            }
            var vistoria = locacao.Vistorias.FirstOrDefault(x => x.IdVistoria == dto.IdVistoria);
            if (vistoria == null)
            {
                _notificador.Add("Vistoria não encontrada");
                return false;
            }
            var dano = vistoria.Danos.FirstOrDefault(x => x.IdDano == dto.IdDano);
            if (dano == null)
            {
                _notificador.Add("Dano não encontrado");
                return false;
            }

            locacao.RemoverDanoVistoria(dto.IdVistoria, dto.IdDano);
            return await _locacaoRepository.AtualizarSalvarAsync(locacao);
        }

        private async Task<List<FotoVistoria>> EnviarFoto(EnviarFotoVistoriaDto dto)
        {
            var documentosAnexos = new List<FotoVistoria>();
            foreach (var doc in dto.Fotos!)
            {
                var arquivo = await _uploadDownloadFileService.EnviarArquivoSimplesAsync(doc);
                if(arquivo != null)
                {
                    var fotoVistoria = FotoVistoria.Criar(      
                         //dto.IdVistoria,
                         arquivo.NomeArquivo,
                         arquivo.Raiz,
                         arquivo.Diretorio,
                         arquivo.Extensao,
                         arquivo.QuantidadeBytes.Value
                    );
                    documentosAnexos.Add(fotoVistoria);
                }               
            }
            return documentosAnexos;
        }


        #endregion Vistoria

        #region Adicionais
        public async Task<bool> InserirAdicionalAsync(int idLocacao, LocacaoAdicionalDto dto, CancellationToken ct = default)
        {
            var locacao = await ObterLocacao(idLocacao, ct);
            if (locacao == null)
            {
                _notificador.Add("Locação não encontrada");
                return false;
            }

            var adicional = await _adicionalRepository.ObterPorIdAsync(dto.IdAdicional);
            if (adicional == null)
            {
                _notificador.Add("Adicional não encontrada");
                return false;
            }

            locacao.AdicionarAdicional(adicional.IdAdicional,adicional.ValorDiaria, dto.Quantidade);

            return await _locacaoRepository.AtualizarSalvarAsync(locacao,ct);
        }

        public async Task<bool> RemoverAdicionalAsync(int idLocacao, int idAdicional, CancellationToken ct = default)
        {
            var locacao = await ObterLocacao(idLocacao, ct);
            if (locacao == null)
            {
                _notificador.Add("Locação não encontrada");
                return false;
            }

            var adicional = await _adicionalRepository.ObterPorIdAsync(idAdicional);
            if (adicional == null)
            {
                _notificador.Add("Adicional não encontrada");
                return false;
            }

            locacao.RemoverAdicional(adicional.IdAdicional);

            return await _locacaoRepository.AtualizarSalvarAsync(locacao, ct);
        }

        public async Task<decimal?> ObterTotalAdicionalAsync(int idLocacao, CancellationToken ct = default)
        {
            var locacao = await ObterLocacao(idLocacao, ct);
            if (locacao == null)
            {
                _notificador.Add("Locação não encontrada");
                return null;
            }

            var valor = locacao.CalcularTotalAdicionais();

            return valor;
        }
        #endregion Adicionais
    }
}
