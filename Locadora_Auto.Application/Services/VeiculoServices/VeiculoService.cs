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
    private readonly INotificadorService _notificador;

    public VeiculoService(
        IVeiculosRepository veiculoRepository,
        ICategoriaVeiculosRepository categoriaRepository,
        IFilialRepository filialRepository,
        INotificadorService notificador)
    {
        _veiculoRepository = veiculoRepository;
        _categoriaRepository = categoriaRepository;
        _filialRepository = filialRepository;
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

    #endregion
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
