using Locadora_Auto.Application.Configuration.Ultils.NotificadorServices;
using Locadora_Auto.Application.Models.Consultas;
using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Application.Models.Mappers;
using Locadora_Auto.Application.Services.VeiculoServices;
using Locadora_Auto.Domain;
using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Domain.IRepositorio;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

public class VeiculoService : IVeiculoService
{
    private readonly IVeiculosRepository _veiculoRepository;
    private readonly ICategoriaVeiculosRepository _categoriaRepository;
    private readonly IFilialRepository _filialRepository;
    private readonly IMovimentoVeiculoRepository _movimentoRepository;
    private readonly IFuncionarioRepository _funcionarioRepository;
    private readonly ILocacaoRepository _locacaoRepository;
    private readonly INotificadorService _notificador;

    public VeiculoService(
        IVeiculosRepository veiculoRepository,
        ICategoriaVeiculosRepository categoriaRepository,
        IFilialRepository filialRepository,
        IMovimentoVeiculoRepository movimentoRepository,
        IFuncionarioRepository funcionarioRepository,
        ILocacaoRepository locacaoRepository,
        INotificadorService notificador)
    {
        _veiculoRepository = veiculoRepository;
        _categoriaRepository = categoriaRepository;
        _filialRepository = filialRepository;
        _movimentoRepository = movimentoRepository;
        _funcionarioRepository = funcionarioRepository;
        _locacaoRepository = locacaoRepository;
        _notificador = notificador;
    }

    #region Consultas

    public async Task<VeiculoDto?> ObterPorIdAsync(int id, CancellationToken ct = default)
    {
        var veiculo = await _veiculoRepository.ObterPrimeiroAsync(
            v => v.IdVeiculo == id,
            incluir: q => q.Include(v => v.Categoria)
                           .Include(v => v.FilialAtual),
            ct: ct);

        return veiculo?.ToDto();
    }

    private async Task<Veiculo?> ObterPorId(int id, CancellationToken ct = default)
    {
        var veiculo = await _veiculoRepository.ObterPrimeiroAsync(
            v => v.IdVeiculo == id,
            rastreado: true,
            incluir: q => q.Include(v => v.Categoria)
                           .Include(v => v.FilialAtual)
                           .Include(v => v.Manutencoes),
            ct: ct);

        return veiculo;
    }

    public async Task<IReadOnlyList<VeiculoDto>> ObterTodosAsync(CancellationToken ct = default)
    {
        var veiculos = await _veiculoRepository.ObterAsync(
            incluir: q => q.Include(v => v.Categoria)
                           .Include(v => v.FilialAtual),
            ct: ct);

        return veiculos.Select(v => v.ToDto()).ToList();
    }

    /// <summary>
    /// Colunas que a listagem aceita ordenar. Coluna desconhecida cai na placa.
    /// </summary>
    private static readonly OrdenacaoDeConsulta<Veiculo> Ordenacoes =
        OrdenacaoDeConsulta<Veiculo>.Padrao(v => v.Placa)
            .Com("placa", v => v.Placa)
            .Com("marca", v => v.Marca)
            .Com("modelo", v => v.Modelo)
            .Com("ano", v => v.Ano)
            .Com("kmatual", v => v.KmAtual)
            .Com("categoria", v => v.Categoria.Nome)
            .Com("filial", v => v.FilialAtual.Nome)
            .Com("status", v => v.Status)
            .Com("ativo", v => v.Ativo);

    public async Task<PaginatedResult<VeiculoDto>> ObterTodosPaginadoAsync(
        ConsultaPaginadaRequest consulta,
        int? idCategoria = null,
        int? idFilial = null,
        int? idStatus = null,
        bool? ativo = null,
        CancellationToken ct = default)
    {
        var busca = consulta.TermoNormalizado;

        // comparar enum com enum evita o cast dentro da árvore de expressão
        StatusVeiculo? status = idStatus.HasValue ? (StatusVeiculo)idStatus.Value : null;

        Expression<Func<Veiculo, bool>> filtro = v =>
            (busca == null
                || v.Placa.ToLower().Contains(busca)
                || v.Marca.ToLower().Contains(busca)
                || v.Modelo.ToLower().Contains(busca)
                || v.Chassi.ToLower().Contains(busca))
            && (idCategoria == null || v.IdCategoria == idCategoria)
            && (idFilial == null || v.FilialAtualId == idFilial)
            && (status == null || v.Status == status)
            && (ativo == null || v.Ativo == ativo);

        var veiculos = await _veiculoRepository.ObterPaginadoComFiltroAsync(
            filtro: filtro,
            ordenarPor: Ordenacoes.Montar(consulta),
            incluir: q => q.Include(v => v.Categoria)
                           .Include(v => v.FilialAtual),
            pagina: consulta.Pagina,
            itensPorPagina: consulta.ItensPorPagina,
            asNoTracking: true,
            ct: ct);

        return veiculos.ParaDto(VeiculoMapper.ToDtoList);
    }

    public async Task<IReadOnlyList<VeiculoDto>> ObterDisponiveisAsync(int? idFilial = null, CancellationToken ct = default)
    {
        // inativo, locado ou em manutenção nunca conta como disponível
        var veiculos = await _veiculoRepository.ObterAsync(
            filtro: v => v.Ativo
                         && v.Disponivel
                         && v.Status == StatusVeiculo.Disponivel
                         && (idFilial == null || v.FilialAtualId == idFilial),
            incluir: q => q.Include(v => v.Categoria)
                           .Include(v => v.FilialAtual),
            ct: ct);

        return veiculos.Select(v => v.ToDto()).ToList();
    }

    #endregion

    #region CRUD

    public async Task<VeiculoDto?> CriarAsync(CriarVeiculoDto dto, CancellationToken ct = default)
    {
        var validacao = await ValidadorCriacaoVeiculo(dto, ct);
        if(!validacao) return null;

        var veiculo = Veiculo.Criar(dto.Placa,dto.Marca,dto.Modelo,dto.Ano,dto.Chassi,dto.KmInicial,dto.IdCategoria,dto.IdFilialAtual);

        await _veiculoRepository.InserirSalvarAsync(veiculo, ct);

        return await ObterPorIdAsync(veiculo.IdVeiculo, ct);
    }
    private async Task<bool> ValidadorCriacaoVeiculo(CriarVeiculoDto dto, CancellationToken ct = default)
    {
        // RN-55: a comparação tem que usar a mesma forma que Veiculo.Criar grava (trim + maiúscula).
        // Comparar o texto cru deixava "abc1d23" passar pela checagem e estourar no índice do banco
        // logo depois — recusa de regra saindo como 500.
        var placa = Normalizar(dto.Placa);
        var chassi = Normalizar(dto.Chassi);

        // RN-55: a unicidade é entre os **ativos**, igual ao índice parcial de VeiculoConfig. Os
        // dois têm que dizer a mesma coisa: aqui sai a recusa amigável, lá está a garantia.
        if (await _veiculoRepository.ExisteAsync(v => v.Ativo && v.Placa == placa, ct))
        {
            _notificador.Add("Placa já cadastrada em veículo ativo");
        }

        if (await _veiculoRepository.ExisteAsync(v => v.Ativo && v.Chassi == chassi, ct))
        {
            _notificador.Add("Chassi já cadastrado em veículo ativo");
        }

        if (dto.KmInicial < 0)
        {
            _notificador.Add("Km inicial inválido");
        }

        if (!await _categoriaRepository.ExisteAsync(c => c.Id == dto.IdCategoria, ct))
        {
            _notificador.Add("Categoria não encontrada");
        }

        if (!await _filialRepository.ExisteAsync(f => f.IdFilial == dto.IdFilialAtual, ct))
        {
            _notificador.Add("Filial não encontrada");
        }
        if(_notificador.TemNotificacao()) return false;
        return true;
    }
    public async Task<bool> AtualizarAsync(int id, AtualizarVeiculoDto dto, CancellationToken ct = default)
    {
        var veiculo = await _veiculoRepository.ObterPrimeiroAsync(v => v.IdVeiculo == id, rastreado: true,ct: ct);
        if (veiculo == null)
        {
            _notificador.Add("Veículo não encontrado");
            return false;
        }

        // RN-56: terminal também para o cadastro — dado de carro vendido não se altera
        if (veiculo.Status == StatusVeiculo.Desmobilizado)
        {
            _notificador.Add("Veículo desmobilizado não pode ser alterado");
            return false;
        }

        if (dto.KmAtual.HasValue && dto.KmAtual.Value < veiculo.KmAtual)
        {
            _notificador.Add("Km não pode ser menor que o atual");
            return false;
        }

        if (dto.IdFilialAtual.HasValue &&
            !await _filialRepository.ExisteAsync(f => f.IdFilial == dto.IdFilialAtual.Value, ct))
        {
            _notificador.Add("Filial não encontrada");
            return false;
        }

        // campos não informados mantêm o valor atual
        veiculo.Atualizar(
            dto.KmAtual ?? veiculo.KmAtual,
            dto.IdFilialAtual ?? veiculo.FilialAtualId,
            dto.Marca,
            dto.Modelo,
            dto.Ano);

        await _veiculoRepository.SalvarAsync(ct);
        return true;
    }
    public async Task<bool> ExcluirAsync(int id, CancellationToken ct = default)
    {
        var veiculo = await _veiculoRepository.ObterPrimeiroAsync(
            v => v.IdVeiculo == id,
            incluir: q => q.Include(v => v.Locacoes),
            rastreado: true,
            ct: ct);

        if (veiculo == null)
        {
            _notificador.Add($"Veículo com ID {id} não encontrado.");
            return false;
        }

        // a FK de locação é Restrict: excluir um veículo com histórico estouraria no banco
        if (veiculo.Locacoes.Any())
        {
            _notificador.Add("Veículo possui locações registradas e não pode ser excluído. Desative-o em vez de excluir.");
            return false;
        }

        // as manutenções saem junto (FK em cascata)
        await _veiculoRepository.ExcluirSalvarAsync(veiculo, ct);
        return true;
    }

    public async Task<bool> AtivarAsync(int id, CancellationToken ct = default)
    {
        // rastreado: Ativar() é transição de status e, desde a RN-37, também acrescenta um
        // MovimentoVeiculo à coleção — filho novo só chega ao banco pela instância rastreada,
        // porque o SetValues do AtualizarSalvarAsync copia escalares e ignora navegação.
        //
        // E com os bloqueios: Ativar() consulta TemBloqueioEmAberto() para não devolver à oferta um
        // carro que a RN-52 tirou dela. Não há lazy loading no contexto, então sem o Include a
        // coleção viria vazia e a guarda passaria batido — o furo é silencioso, que é o pior tipo
        var veiculo = await _veiculoRepository.ObterPrimeiroAsync(
            v => v.IdVeiculo == id,
            rastreado: true,
            incluir: q => q.Include(v => v.Bloqueios),
            ct: ct);

        if (veiculo == null)
        {
            _notificador.Add("Veículo não encontrado");
            return false;
        }

        // RN-55: reativar é a única operação que pode colidir no índice parcial — enquanto o
        // veículo estava inativo, nada impedia recadastrar a placa dele. A checagem é aqui e não
        // em Veiculo.Ativar() porque o domínio não enxerga os outros veículos.
        if (await _veiculoRepository.ExisteAsync(
                v => v.Ativo && v.IdVeiculo != id && (v.Placa == veiculo.Placa || v.Chassi == veiculo.Chassi), ct))
        {
            _notificador.Add("Já existe veículo ativo com esta placa ou chassi");
            return false;
        }

        veiculo.Ativar();
        return await _veiculoRepository.AtualizarSalvarAsync(veiculo, ct);
    }
    public async Task<bool> DesativarAsync(int id, CancellationToken ct = default)
    {
        // rastreado: mesmo motivo do AtivarAsync — a transição gera movimento
        var veiculo = await _veiculoRepository.ObterPorIdAsync(id, true, ct);
        if (veiculo == null)
        {
            _notificador.Add("Veículo não encontrado");
            return false;
        }

        veiculo.Desativar();
        return await _veiculoRepository.AtualizarSalvarAsync(veiculo, ct);
    }

    /// <summary>
    /// RN-45: o pátio declara o carro pronto e ele volta à oferta. Sem esta porta o veículo
    /// devolvido fica preso em <see cref="StatusVeiculo.EmPreparacao"/>, fora da disponibilidade.
    /// Veículo inativo volta para <see cref="StatusVeiculo.Bloqueado"/>, não para a oferta.
    /// </summary>
    public async Task<bool> LiberarDaPreparacaoAsync(int id, CancellationToken ct = default)
    {
        var veiculo = await _veiculoRepository.ObterPrimeiroAsync(
            v => v.IdVeiculo == id,
            rastreado: true,
            ct: ct);

        if (veiculo == null)
        {
            _notificador.Add("Veículo não encontrado");
            return false;
        }

        // repete a guarda de Veiculo.LiberarDaPreparacao para a recusa sair como ProblemDetails 4xx
        if (veiculo.Status != StatusVeiculo.EmPreparacao)
        {
            _notificador.Add($"Veículo não está em preparação (situação atual: {veiculo.Status})");
            return false;
        }

        veiculo.LiberarDaPreparacao();
        await _veiculoRepository.SalvarAsync(ct);
        return true;
    }

    /// <summary>
    /// RN-45, parte automática: devolve à oferta todo veículo cujo prazo de preparação venceu sem
    /// o pátio ter declarado nada. Sem esta varredura o carro esquecido pelo pátio some da oferta
    /// para sempre, e a frota encolhe sem ninguém perceber.
    ///
    /// Não notifica: é varredura de lote, disparada por agendador e não por usuário, e recusa
    /// individual aqui não tem para quem ser respondida. O que ela devolve é a contagem — é assim
    /// que a liberação sem conferência fica visível.
    /// </summary>
    public async Task<LiberacaoPreparacaoDto> LiberarPreparacoesVencidasAsync(CancellationToken ct = default)
    {
        var agora = DateTime.UtcNow;

        // rastreado: LiberarDaPreparacaoPorPrazo é transição e acrescenta MovimentoVeiculo à
        // coleção — filho novo só chega ao banco pela instância rastreada
        var noPatio = await _veiculoRepository.ObterAsync(
            filtro: v => v.Status == StatusVeiculo.EmPreparacao,
            rastreado: true,
            ct: ct);

        var resultado = new LiberacaoPreparacaoDto { Analisados = noPatio.Count };
        if (noPatio.Count == 0) return resultado;

        // o prazo vem da filial atual do veículo, que na devolução one-way já é a de destino
        // (RN-47): quem prepara o carro é o pátio de onde ele está. Consulta à parte em vez de
        // Include porque só o inteiro interessa
        var idsFilial = noPatio.Select(v => v.FilialAtualId).Distinct().ToList();
        var filiais = (await _filialRepository.ObterAsync(
                filtro: f => idsFilial.Contains(f.IdFilial),
                ct: ct))
            .ToDictionary(f => f.IdFilial);

        var idsVeiculo = noPatio.Select(v => v.IdVeiculo).ToList();

        // quando cada carro entrou no pátio: o DataMovimento do movimento que o levou a
        // EmPreparacao. O mais recente deles, porque o mesmo carro entra e sai a cada ciclo de
        // locação — e o desempate é pelo id, não pela data, porque duas transições do mesmo
        // SaveChanges caem no mesmo instante e o id é atribuído na ordem do insert.
        //
        // Traz todas as entradas históricas dos carros do lote, e não só a última de cada um:
        // "o maior por grupo" não sai do repositório genérico. O lote é o pátio de agora, então o
        // custo é pequeno — mesmo ponto de virada dos indicadores da seção 12, quando a trilha
        // crescer isto vira agregação no banco
        var entradas = (await _movimentoRepository.ObterAsync(
                filtro: m => idsVeiculo.Contains(m.IdVeiculo)
                             && m.StatusDestino == StatusVeiculo.EmPreparacao,
                ct: ct))
            .GroupBy(m => m.IdVeiculo)
            .ToDictionary(g => g.Key, g => g.MaxBy(m => m.IdMovimentoVeiculo)!.DataMovimento);

        foreach (var veiculo in noPatio)
        {
            // a FK de filial é obrigatória, então a busca não falha; se algum dia falhar, deixar o
            // carro no pátio é o lado seguro — a porta manual continua aberta
            if (!filiais.TryGetValue(veiculo.FilialAtualId, out var filial))
                continue;

            if (entradas.TryGetValue(veiculo.IdVeiculo, out var inicioPreparacao))
            {
                if (!filial.PreparacaoVencida(inicioPreparacao, agora))
                {
                    resultado.AindaNoPrazo++;
                    continue;
                }
            }
            else
            {
                // sem carimbo: o carro entrou no pátio antes da trilha da RN-37 existir, logo está
                // parado há mais tempo que qualquer TempoPreparacaoMinutos e o prazo venceu por
                // construção. Inventar um início "agora" reiniciaria o relógio de um carro parado
                // há dias — esconderia exatamente o que se quer enxergar
                resultado.SemCarimbo++;
            }

            // veículo inativo não volta para a oferta: SairParaOferta o manda para Bloqueado
            // (RN-53). A transição é a mesma; o que muda é o destino
            veiculo.LiberarDaPreparacaoPorPrazo();
            resultado.Liberados++;
        }

        if (resultado.Liberados > 0)
            await _veiculoRepository.SalvarAsync(ct);

        return resultado;
    }

    #endregion

    #region Bloqueio

    /// <summary>
    /// RN-52: tira o veículo da oferta com motivo, prazo e responsável.
    ///
    /// As guardas de <c>Veiculo.Bloquear</c> e de <c>BloqueioVeiculo.Criar</c> são repetidas aqui
    /// de propósito, como no resto do serviço: <c>DomainException</c> é <c>internal</c> e não está
    /// no <c>ExceptionProblemFactory</c> — se escapar, a recusa de regra vira 500 em vez de 4xx.
    /// </summary>
    public async Task<BloqueioVeiculoDto?> BloquearAsync(int id, BloquearVeiculoDto dto, CancellationToken ct = default)
    {
        // rastreado e com os bloqueios: Bloquear() acrescenta filho novo à coleção, e filho novo só
        // chega ao banco pela instância rastreada. Sem o Include, TemBloqueioEmAberto() olharia uma
        // coleção vazia e deixaria abrir o segundo bloqueio
        var veiculo = await _veiculoRepository.ObterPrimeiroAsync(
            v => v.IdVeiculo == id,
            rastreado: true,
            incluir: q => q.Include(v => v.Bloqueios),
            ct: ct);

        if (veiculo == null)
        {
            _notificador.Add("Veículo não encontrado");
            return null;
        }

        var motivo = (MotivoBloqueio)dto.IdMotivo;
        if (!Enum.IsDefined(motivo))
            _notificador.Add("Motivo de bloqueio inválido");

        var prazo = NormalizarUtc(dto.DataPrevistaLiberacao) ?? DateTime.UtcNow;
        if (prazo <= DateTime.UtcNow)
            _notificador.Add("A data prevista de liberação tem que ser futura");

        // mesma razão de BloqueioVeiculo.Criar: int.IsPositive(0) é true
        if (dto.IdFuncionarioResponsavel <= 0)
            _notificador.Add("Bloqueio exige um funcionário responsável");
        else if (!await _funcionarioRepository.ExisteAsync(f => f.IdFuncionario == dto.IdFuncionarioResponsavel, ct))
            _notificador.Add("Funcionário responsável não encontrado");

        if (veiculo.TemBloqueioEmAberto())
            _notificador.Add("Veículo já possui bloqueio em aberto");
        else if (!Veiculo.PodeSerBloqueado(veiculo.Status))
            _notificador.Add($"Veículo não pode ser bloqueado na situação atual ({veiculo.Status})");

        if (_notificador.TemNotificacao()) return null;

        var bloqueio = veiculo.Bloquear(motivo, prazo, dto.IdFuncionarioResponsavel, dto.Observacao);

        await _veiculoRepository.SalvarAsync(ct);

        return bloqueio.ToDto(DateTime.UtcNow);
    }

    /// <summary>
    /// Encerra o bloqueio. O veículo volta para a situação em que estava quando foi bloqueado, não
    /// direto para a oferta — quem decide isso é o domínio, pelo <c>StatusAnterior</c> gravado.
    /// </summary>
    public async Task<bool> LiberarBloqueioAsync(int id, int idBloqueio, CancellationToken ct = default)
    {
        var veiculo = await _veiculoRepository.ObterPrimeiroAsync(
            v => v.IdVeiculo == id,
            rastreado: true,
            incluir: q => q.Include(v => v.Bloqueios),
            ct: ct);

        if (veiculo == null)
        {
            _notificador.Add("Veículo não encontrado");
            return false;
        }

        if (veiculo.Status != StatusVeiculo.Bloqueado)
        {
            _notificador.Add($"Veículo não está bloqueado (situação atual: {veiculo.Status})");
            return false;
        }

        if (!veiculo.Bloqueios.Any(b => b.IdBloqueioVeiculo == idBloqueio && b.EmAberto))
        {
            _notificador.Add($"Bloqueio {idBloqueio} não encontrado em aberto para este veículo");
            return false;
        }

        veiculo.LiberarBloqueio(idBloqueio);
        await _veiculoRepository.SalvarAsync(ct);
        return true;
    }

    public async Task<IReadOnlyList<BloqueioVeiculoDto>> ObterBloqueiosAsync(int id, CancellationToken ct = default)
    {
        var veiculo = await _veiculoRepository.ObterPrimeiroAsync(
            v => v.IdVeiculo == id,
            incluir: q => q.Include(v => v.Bloqueios)
                           .ThenInclude(b => b.Responsavel),
            ct: ct);

        if (veiculo == null)
        {
            _notificador.Add("Veículo não encontrado");
            return new List<BloqueioVeiculoDto>();
        }

        // um relógio só para a lista inteira: duas linhas da mesma resposta não podem discordar
        // sobre o que está vencido
        var agora = DateTime.UtcNow;

        return veiculo.Bloqueios
            .OrderByDescending(b => b.DataBloqueio)
            .ToDtoList(agora);
    }

    #endregion Bloqueio

    #region Transferencia entre filiais

    /// <summary>
    /// RN-49: tira o veículo da oferta da origem e o coloca na estrada.
    ///
    /// A checagem das duas filiais mora aqui, e não no domínio: <c>Filial</c> é outro agregado e o
    /// <c>Veiculo</c> não a enxerga. O que ele garante sozinho é o que é dele — estar ativo, estar
    /// disponível e não ter viagem em curso.
    /// </summary>
    public async Task<TransferenciaVeiculoDto?> EnviarParaTransferenciaAsync(
        int id, EnviarTransferenciaDto dto, CancellationToken ct = default)
    {
        var veiculo = await _veiculoRepository.ObterPrimeiroAsync(
            v => v.IdVeiculo == id,
            rastreado: true,
            incluir: q => q.Include(v => v.Transferencias),
            ct: ct);

        if (veiculo == null)
        {
            _notificador.Add("Veículo não encontrado");
            return null;
        }

        var origem = await _filialRepository.ObterPrimeiroAsync(f => f.IdFilial == veiculo.FilialAtualId, ct: ct);
        var destino = await _filialRepository.ObterPrimeiroAsync(f => f.IdFilial == dto.IdFilialDestino, ct: ct);

        if (destino == null)
            _notificador.Add("Filial de destino não encontrada");
        else if (destino.IdFilial == veiculo.FilialAtualId)
            _notificador.Add("A filial de destino tem que ser diferente da de origem");
        else
        {
            // as duas pontas precisam participar do remanejamento: mandar carro para uma filial que
            // não o recebe é criar viagem que ninguém vai confirmar
            if (!destino.Ativo)
                _notificador.Add("Filial de destino está inativa");
            if (!destino.PermiteTransferencia)
                _notificador.Add("Filial de destino não aceita transferência de veículo");
            if (origem is { PermiteTransferencia: false })
                _notificador.Add("Filial de origem não participa de transferência de veículo");
        }

        var chegada = NormalizarUtc(dto.DataPrevistaChegada) ?? DateTime.UtcNow;
        if (chegada <= DateTime.UtcNow)
            _notificador.Add("A data prevista de chegada tem que ser futura");

        // mesma razão de BloqueioVeiculo.Criar: int.IsPositive(0) é true
        if (dto.IdFuncionarioResponsavel <= 0)
            _notificador.Add("Transferência exige um funcionário responsável");
        else if (!await _funcionarioRepository.ExisteAsync(f => f.IdFuncionario == dto.IdFuncionarioResponsavel, ct))
            _notificador.Add("Funcionário responsável não encontrado");

        // guardas do domínio repetidas para a recusa sair como ProblemDetails 4xx
        if (!veiculo.Ativo)
            _notificador.Add("Veículo inativo não pode ser transferido");
        else if (veiculo.TemTransferenciaEmTransito())
            _notificador.Add("Veículo já está em transferência");
        else if (veiculo.Status != StatusVeiculo.Disponivel)
            _notificador.Add($"Só veículo disponível pode ser transferido (situação atual: {veiculo.Status})");

        if (_notificador.TemNotificacao()) return null;

        var transferencia = veiculo.EnviarParaTransferencia(
            dto.IdFilialDestino, chegada, dto.IdFuncionarioResponsavel, dto.Observacao);

        await _veiculoRepository.SalvarAsync(ct);

        return transferencia.ToDto(DateTime.UtcNow);
    }

    public async Task<bool> ConfirmarChegadaTransferenciaAsync(
        int id, int idTransferencia, ChegadaTransferenciaDto dto, CancellationToken ct = default)
    {
        var veiculo = await _veiculoRepository.ObterPrimeiroAsync(
            v => v.IdVeiculo == id,
            rastreado: true,
            incluir: q => q.Include(v => v.Transferencias),
            ct: ct);

        if (veiculo == null)
        {
            _notificador.Add("Veículo não encontrado");
            return false;
        }

        if (veiculo.Status != StatusVeiculo.EmTransferencia)
        {
            _notificador.Add($"Veículo não está em transferência (situação atual: {veiculo.Status})");
            return false;
        }

        if (!veiculo.Transferencias.Any(t => t.IdTransferenciaVeiculo == idTransferencia && t.EmTransito))
        {
            _notificador.Add($"Transferência {idTransferencia} não encontrada em trânsito para este veículo");
            return false;
        }

        // RN-54: o hodômetro não retrocede. A recusa sai por notificação porque digitar km errado na
        // chegada é rotina de pátio, não erro de programa
        if (dto.KmChegada < veiculo.KmAtual)
        {
            _notificador.Add($"Km não pode ser menor que o atual ({veiculo.KmAtual})");
            return false;
        }

        veiculo.ConfirmarChegadaTransferencia(idTransferencia, dto.KmChegada);
        await _veiculoRepository.SalvarAsync(ct);
        return true;
    }

    public async Task<bool> CancelarTransferenciaAsync(int id, int idTransferencia, CancellationToken ct = default)
    {
        var veiculo = await _veiculoRepository.ObterPrimeiroAsync(
            v => v.IdVeiculo == id,
            rastreado: true,
            incluir: q => q.Include(v => v.Transferencias),
            ct: ct);

        if (veiculo == null)
        {
            _notificador.Add("Veículo não encontrado");
            return false;
        }

        if (veiculo.Status != StatusVeiculo.EmTransferencia)
        {
            _notificador.Add($"Veículo não está em transferência (situação atual: {veiculo.Status})");
            return false;
        }

        if (!veiculo.Transferencias.Any(t => t.IdTransferenciaVeiculo == idTransferencia && t.EmTransito))
        {
            _notificador.Add($"Transferência {idTransferencia} não encontrada em trânsito para este veículo");
            return false;
        }

        veiculo.CancelarTransferencia(idTransferencia);
        await _veiculoRepository.SalvarAsync(ct);
        return true;
    }

    public async Task<IReadOnlyList<TransferenciaVeiculoDto>> ObterTransferenciasAsync(int id, CancellationToken ct = default)
    {
        var veiculo = await _veiculoRepository.ObterPrimeiroAsync(
            v => v.IdVeiculo == id,
            incluir: q => q.Include(v => v.Transferencias).ThenInclude(t => t.FilialOrigem)
                           .Include(v => v.Transferencias).ThenInclude(t => t.FilialDestino)
                           .Include(v => v.Transferencias).ThenInclude(t => t.Responsavel),
            ct: ct);

        if (veiculo == null)
        {
            _notificador.Add("Veículo não encontrado");
            return new List<TransferenciaVeiculoDto>();
        }

        var agora = DateTime.UtcNow;

        return veiculo.Transferencias
            .OrderByDescending(t => t.DataEnvio)
            .ToDtoList(agora);
    }

    #endregion Transferencia entre filiais

    #region Desmobilizacao

    /// <summary>
    /// RN-56: o ativo deixa a frota, em definitivo.
    ///
    /// A guarda que só o serviço consegue fazer é a do <b>contrato</b>. O status do veículo é um
    /// retrato de agora e não enxerga período: um carro <c>Disponivel</c> hoje pode ter contrato
    /// vendido para a semana que vem, e desmobilizá-lo criaria cliente no balcão sem carro — a
    /// mesma falha que a RN-40 fecha do outro lado. Por isso a consulta é por status não terminal,
    /// e não por "contrato em andamento".
    /// </summary>
    public async Task<bool> DesmobilizarAsync(int id, DesmobilizarVeiculoDto dto, CancellationToken ct = default)
    {
        var veiculo = await _veiculoRepository.ObterPrimeiroAsync(
            v => v.IdVeiculo == id,
            rastreado: true,
            incluir: q => q.Include(v => v.Bloqueios),
            ct: ct);

        if (veiculo == null)
        {
            _notificador.Add("Veículo não encontrado");
            return false;
        }

        if (string.IsNullOrWhiteSpace(dto.Motivo))
            _notificador.Add("Desmobilização exige o motivo");

        // mesma razão de BloqueioVeiculo.Criar: int.IsPositive(0) é true
        if (dto.IdFuncionarioResponsavel <= 0)
            _notificador.Add("Desmobilização exige um funcionário responsável");
        else if (!await _funcionarioRepository.ExisteAsync(f => f.IdFuncionario == dto.IdFuncionarioResponsavel, ct))
            _notificador.Add("Funcionário responsável não encontrado");

        if (veiculo.Status == StatusVeiculo.Desmobilizado)
            _notificador.Add("Veículo já está desmobilizado");
        else if (!Veiculo.PodeSerDesmobilizado(veiculo.Status))
            _notificador.Add($"Veículo não pode ser desmobilizado na situação atual ({veiculo.Status})");

        // contrato aberto **ou futuro**: os dois impedem, e é o segundo que o status não revela
        if (await _locacaoRepository.ExisteAsync(
                l => l.IdVeiculo == id && !Locacao.StatusTerminais.Contains(l.Status), ct))
        {
            _notificador.Add("Veículo possui contrato não encerrado e não pode ser desmobilizado");
        }

        if (_notificador.TemNotificacao()) return false;

        veiculo.Desmobilizar(dto.Motivo, dto.IdFuncionarioResponsavel);
        await _veiculoRepository.SalvarAsync(ct);
        return true;
    }

    #endregion Desmobilizacao

    #region Trilha do ativo

    /// <summary>
    /// Colunas que a trilha aceita ordenar.
    ///
    /// O padrão é o id, e não a data, de propósito: <c>DataMovimento</c> vem de um
    /// <c>DateTime.UtcNow</c> por transição, e duas transições do mesmo <c>SaveChanges</c> podem
    /// cair no mesmo instante. Empate no ORDER BY faz o Postgres devolver a página 2 com uma linha
    /// que já apareceu na 1 — ou nenhuma. O id é único e cresce junto com o tempo (a sequência é
    /// atribuída na ordem do insert), então ordena igual e pagina estável.
    /// </summary>
    private static readonly OrdenacaoDeConsulta<MovimentoVeiculo> OrdenacoesMovimento =
        OrdenacaoDeConsulta<MovimentoVeiculo>.Padrao(m => m.IdMovimentoVeiculo, descendente: true)
            .Com("data", m => m.DataMovimento)
            .Com("statusdestino", m => m.StatusDestino)
            .Com("tipoorigem", m => m.TipoOrigem)
            .Com("autor", m => m.IdUsuarioCriacao);

    /// <summary>
    /// RN-37: por onde o carro passou. É a leitura que fecha o ciclo da trilha — sem ela o
    /// movimento é gravado e ninguém consegue conferir, que é o pior estado possível para uma
    /// tabela de auditoria.
    ///
    /// Consulta <c>MovimentoVeiculo</c> direto, e não <c>veiculo.Movimentos</c> por
    /// <c>Include</c>, porque a trilha de um carro antigo tem centenas de linhas e não há como
    /// paginar dentro de um Include: viria tudo para a memória para descartar 90%.
    /// </summary>
    public async Task<PaginatedResult<MovimentoVeiculoDto>> ObterMovimentosAsync(
        int id,
        ConsultaPaginadaRequest consulta,
        DateTime? de = null,
        DateTime? ate = null,
        int? idTipoOrigem = null,
        CancellationToken ct = default)
    {
        if (!await _veiculoRepository.ExisteAsync(v => v.IdVeiculo == id, ct))
        {
            _notificador.Add("Veículo não encontrado");
            return PaginaVazia(consulta);
        }

        // as colunas são timestamptz e o Npgsql recusa DateTime que não seja Utc; data de query
        // string chega Unspecified
        var inicio = NormalizarUtc(de);
        var fim = NormalizarUtc(ate);

        // comparar enum com enum evita o cast dentro da árvore de expressão
        TipoDocumentoOrigem? tipoOrigem = idTipoOrigem.HasValue
            ? (TipoDocumentoOrigem)idTipoOrigem.Value
            : null;

        // o termo procura o autor: a pergunta que a trilha responde é "quem mexeu neste carro", e
        // o resto da linha é enum e id, que já têm filtro próprio
        var busca = consulta.TermoNormalizado;

        Expression<Func<MovimentoVeiculo, bool>> filtro = m =>
            m.IdVeiculo == id
            && (inicio == null || m.DataMovimento >= inicio)
            && (fim == null || m.DataMovimento <= fim)
            && (tipoOrigem == null || m.TipoOrigem == tipoOrigem)
            && (busca == null
                || (m.IdUsuarioCriacao != null && m.IdUsuarioCriacao.ToLower().Contains(busca)));

        var movimentos = await _movimentoRepository.ObterPaginadoComFiltroAsync(
            filtro: filtro,
            ordenarPor: OrdenacoesMovimento.Montar(consulta),
            pagina: consulta.Pagina,
            itensPorPagina: consulta.ItensPorPagina,
            asNoTracking: true,
            ct: ct);

        return movimentos.ParaDto(MovimentoVeiculoMapper.ToDtoList);
    }

    /// <summary>
    /// Página sem itens que preserva o que o cliente pediu. Devolver <c>null</c> obrigaria o
    /// controller a distinguir "não achei" de "achei e está vazio" — o notificador já faz isso.
    /// </summary>
    private static PaginatedResult<MovimentoVeiculoDto> PaginaVazia(ConsultaPaginadaRequest consulta)
        => new()
        {
            Items = new List<MovimentoVeiculoDto>(),
            Total = 0,
            Pagina = consulta.Pagina,
            TotalPaginas = 0,
            ItensPorPagina = consulta.ItensPorPagina
        };

    /// <summary>
    /// Placa e chassi como <c>Veiculo.Criar</c> os grava: sem espaço nas pontas e em maiúscula.
    /// A checagem de unicidade (RN-55) precisa comparar na forma gravada, não na digitada.
    /// </summary>
    private static string Normalizar(string? texto) => (texto ?? string.Empty).Trim().ToUpper();

    /// <summary>
    /// Mesma regra do conversor global do <c>LocadoraDbContext</c> (e do
    /// <c>ReservaService.NormalizarUtc</c>): Local vira UTC, Unspecified é remarcado como UTC.
    /// </summary>
    private static DateTime? NormalizarUtc(DateTime? data) => data == null
        ? null
        : data.Value.Kind switch
        {
            DateTimeKind.Utc => data.Value,
            DateTimeKind.Local => data.Value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(data.Value, DateTimeKind.Utc)
        };

    #endregion Trilha do ativo

    #region Manutecao
    public async Task<IReadOnlyList<ManutencaoDto>> ObterManutencoesAsync(int id, CancellationToken ct = default)
    {
        var veiculo = await _veiculoRepository.ObterPrimeiroAsync(
            v => v.IdVeiculo == id,
            incluir: q => q.Include(v => v.Manutencoes),
            ct: ct);

        if (veiculo == null)
        {
            _notificador.Add("Veículo não encontrado");
            return new List<ManutencaoDto>();
        }

        return veiculo.Manutencoes
            .OrderByDescending(m => m.DataInicio)
            .Select(m => m.ToDto(veiculo.IdVeiculo))
            .ToList();
    }

    public async Task<bool> IniciarManutencao(int id,CriarManutencaoDto dto, CancellationToken ct = default)
    {
        var veiculo = await ObterPorId(id);
        if (veiculo == null)
        {
            _notificador.Add("Veículo não encontrado");
            return false;
        }

        veiculo.IniciarManutencao((TipoManutencao)dto.IdTipoManutencao, dto.Descricao);
        return await _veiculoRepository.AtualizarSalvarAsync(veiculo, ct);
    }

    public async Task<bool> TerminaManutencao(int id, TerminarManutencaoDto dto, CancellationToken ct = default)
    {
        var veiculo = await ObterPorId(id);
        if (veiculo == null)
        {
            _notificador.Add("Veículo não encontrado");
            return false;
        }

        veiculo.TerminaManutencao(dto.custo,dto.IdManutencao);
        return await _veiculoRepository.AtualizarSalvarAsync(veiculo, ct);
    }
    public async Task<bool> CancelarManutencao(int id, int idManutencao, CancellationToken ct = default)
    {
        var veiculo = await ObterPorId(id);
        if (veiculo == null)
        {
            _notificador.Add("Veículo não encontrado");
            return false;
        }

        veiculo.CancelarManutencao(idManutencao);
        return await _veiculoRepository.AtualizarSalvarAsync(veiculo, ct);
    }

    public async Task<bool> AtualizarDescricaoManutencao(int id, AtualizarManutencaoDto dto, CancellationToken ct = default)
    {
        var veiculo = await ObterPorId(id);
        if (veiculo == null)
        {
            _notificador.Add("Veículo não encontrado");
            return false;
        }

        veiculo.AtualizarDescricaoManutencao(dto.IdManutencao,dto.Descricao);
        return await _veiculoRepository.AtualizarSalvarAsync(veiculo, ct);
    }
    #endregion Manutencao

}
