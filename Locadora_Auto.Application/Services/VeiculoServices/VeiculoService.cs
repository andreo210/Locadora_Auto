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
    private readonly INotificadorService _notificador;

    public VeiculoService(
        IVeiculosRepository veiculoRepository,
        ICategoriaVeiculosRepository categoriaRepository,
        IFilialRepository filialRepository,
        IMovimentoVeiculoRepository movimentoRepository,
        INotificadorService notificador)
    {
        _veiculoRepository = veiculoRepository;
        _categoriaRepository = categoriaRepository;
        _filialRepository = filialRepository;
        _movimentoRepository = movimentoRepository;
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
        if (await _veiculoRepository.ExisteAsync(v => v.Placa == dto.Placa, ct))
        {
            _notificador.Add("Placa já cadastrada");
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
        // porque o SetValues do AtualizarSalvarAsync copia escalares e ignora navegação
        var veiculo = await _veiculoRepository.ObterPorIdAsync(id, true, ct);
        if (veiculo == null)
        {
            _notificador.Add("Veículo não encontrado");
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
    /// Veículo inativo volta para <see cref="StatusVeiculo.Indisponivel"/>, não para a oferta.
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

            // veículo inativo não volta para a oferta: SairParaOferta o manda para Indisponivel
            // (RN-53). A transição é a mesma; o que muda é o destino
            veiculo.LiberarDaPreparacaoPorPrazo();
            resultado.Liberados++;
        }

        if (resultado.Liberados > 0)
            await _veiculoRepository.SalvarAsync(ct);

        return resultado;
    }

    #endregion

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
