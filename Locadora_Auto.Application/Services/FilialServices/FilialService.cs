using Locadora_Auto.Application.Models;
using Locadora_Auto.Application.Models.Dto.Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Application.Models.Mappers;
using Locadora_Auto.Application.Services.Notificador;
using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Domain.IRepositorio;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace Locadora_Auto.Application.Services.FilialServices;

public class FilialService : IFilialService
{
    private readonly IFilialRepository _filialRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<FilialService> _logger;
    private readonly INotificadorService _notificador;

    public FilialService(
        IFilialRepository filialRepository,
        IUnitOfWork unitOfWork,
        INotificadorService notificador,
        ILogger<FilialService> logger)
    {
        _filialRepository = filialRepository ?? throw new ArgumentNullException(nameof(filialRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _notificador = notificador ?? throw new ArgumentNullException(nameof(notificador));
    }

    //#region Operações de Consulta

    public async Task<FilialDto?> ObterPorIdAsync(int id, CancellationToken ct = default)
    {
        var filial = await _filialRepository.ObterPrimeiroAsync(
            f => f.IdFilial == id,
            incluir: e => e.Include(c => c.Endereco),
            rastreado: false,
            ct: ct);

        if (filial == null)
        {
            _notificador.Add($"Filial com ID {id} não encontrada.");
            return null;
        }

        return filial.ToDto();
    }

    private async Task<Filial?> ObterPorId(int id, CancellationToken ct = default)
    {
        var filtro = (Expression<Func<Filial, bool>>)(f => f.IdFilial == id);
        var filial = await _filialRepository.ObterPrimeiroAsync(filtro: filtro, incluir: e => e.Include(c => c.Endereco), true, ct);
        if (filial == null)
            return null;
        return filial;
    }

    //public async Task<FilialDto?> ObterPorIdComVeiculosAsync(int id, CancellationToken ct = default)
    //{
    //    try
    //    {
    //        _logger.LogInformation("Buscando filial com veículos por ID: {Id}", id);

    //        var filial = await _filialRepository.ObterPorIdCompletoAsync(id, ct);
    //        if (filial == null)
    //            return null;

    //        var totalVeiculos = filial.Veiculos?.Count ?? 0;
    //        var veiculosDisponiveis = filial.Veiculos?.Count(v => v.Disponivel) ?? 0;

    //        return filial.ToDto(totalVeiculos, veiculosDisponiveis);
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Erro ao buscar filial com veículos por ID: {Id}", id);
    //        throw;
    //    }
    //}

    public async Task<IReadOnlyList<FilialDto>> ObterTodasAsync(CancellationToken ct = default)
    {        
        var filiais = await _filialRepository.ObterAsync(
            ordenarPor: q => q.OrderBy(f => f.Cidade).ThenBy(f => f.Nome),
            incluir: q => q.Include(f => f.Endereco),
            ct: ct);

        var resultado = new List<FilialDto>();

        foreach (var filial in filiais)
        {
            //var totalVeiculos = await _filialRepository.ContarVeiculosNaFilialAsync(filial.IdFilial, ct);
            //var veiculosDisponiveis = await _filialRepository.ContarVeiculosDisponiveisNaFilialAsync(filial.IdFilial, ct);

            resultado.Add(filial.ToDto());
        }
        return resultado;        
    }

    //public async Task<IReadOnlyList<FilialDto>> ObterAtivasAsync(CancellationToken ct = default)
    //{
    //    try
    //    {
    //        _logger.LogInformation("Buscando filiais ativas");

    //        var filiais = await _filialRepository.ObterAtivasAsync(ct);

    //        var resultado = new List<FilialDto>();

    //        foreach (var filial in filiais)
    //        {
    //            var totalVeiculos = await _filialRepository.ContarVeiculosNaFilialAsync(filial.IdFilial, ct);
    //            var veiculosDisponiveis = await _filialRepository.ContarVeiculosDisponiveisNaFilialAsync(filial.IdFilial, ct);

    //            resultado.Add(filial.ToDto(totalVeiculos, veiculosDisponiveis));
    //        }

    //        return resultado;
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Erro ao buscar filiais ativas");
    //        throw;
    //    }
    //}

    //public async Task<IReadOnlyList<FilialResumoDto>> ObterResumoAsync(CancellationToken ct = default)
    //{
    //    try
    //    {
    //        _logger.LogInformation("Buscando resumo das filiais");

    //        var filiais = await _filialRepository.ObterAtivasAsync(ct);

    //        var resultado = new List<FilialResumoDto>();

    //        foreach (var filial in filiais)
    //        {
    //            var totalVeiculos = await _filialRepository.ContarVeiculosNaFilialAsync(filial.IdFilial, ct);
    //            var veiculosDisponiveis = await _filialRepository.ContarVeiculosDisponiveisNaFilialAsync(filial.IdFilial, ct);

    //            resultado.Add(filial.ToResumoDto(totalVeiculos, veiculosDisponiveis));
    //        }

    //        return resultado;
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Erro ao buscar resumo das filiais");
    //        throw;
    //    }
    //}

    //public async Task<bool> ExisteFilialAsync(int id, CancellationToken ct = default)
    //{
    //    try
    //    {
    //        return await _filialRepository.ExisteAsync(f => f.IdFilial == id, ct);
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Erro ao verificar existência da filial: {Id}", id);
    //        throw;
    //    }
    //}

    //public async Task<int> ContarAtivasAsync(CancellationToken ct = default)
    //{
    //    try
    //    {
    //        return await _filialRepository.ContarAsync(f => f.Ativo, ct);
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Erro ao contar filiais ativas");
    //        throw;
    //    }
    //}

    //public async Task<IReadOnlyList<FilialDto>> ObterComFiltroAsync(
    //    Expression<Func<Filial, bool>>? filtro = null,
    //    Func<IQueryable<Filial>, IOrderedQueryable<Filial>>? ordenarPor = null,
    //    CancellationToken ct = default)
    //{
    //    try
    //    {
    //        var filiais = await _repositorioGlobal.ObterComFiltroAsync<Filial>(
    //            filtro: filtro,
    //            incluir: q => q.Include(f => f.Endereco),
    //            ordenarPor: ordenarPor,
    //            asNoTracking: true,
    //            ct: ct);

    //        var resultado = new List<FilialDto>();

    //        foreach (var filial in filiais)
    //        {
    //            var totalVeiculos = await _filialRepository.ContarVeiculosNaFilialAsync(filial.IdFilial, ct);
    //            var veiculosDisponiveis = await _filialRepository.ContarVeiculosDisponiveisNaFilialAsync(filial.IdFilial, ct);

    //            resultado.Add(filial.ToDto(totalVeiculos, veiculosDisponiveis));
    //        }

    //        return resultado;
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Erro ao buscar filiais com filtro");
    //        throw;
    //    }
    //}

    //#endregion

    //#region Operações de CRUD

    public async Task<FilialDto> CriarFilialAsync(CriarFilialDto filialDto, CancellationToken ct = default)
    {
        // Validações
        await ValidarCriacaoFilialAsync(filialDto, ct);
        // Criar entidade
        var filial = filialDto.ToEntity();
        await _filialRepository.InserirSalvarAsync(filial, ct);
        return filial.ToDto();       
    }

    public async Task<bool> AtualizarFilialAsync(int id, AtualizarFilialDto filialDto, CancellationToken ct = default)
    {
        var filial = await _filialRepository.ObterPrimeiroAsync(
            f => f.IdFilial == id,
            incluir: e => e.Include(c => c.Endereco),
            rastreado: true,
            ct: ct);

        if (filial == null)
        {
            _notificador.Add($"Filial com ID {id} não encontrada.");
            return false;
        }

        if (!await ValidarAtualizacaoFilialAsync(id, filialDto, ct))
            return false;

        filial.AtualizarDto(filialDto);

        var rows = await _filialRepository.SalvarAsync(ct);
        if (rows == 0)
        {
            _notificador.Add("Nenhuma alteração foi realizada.");
            return false;
        }

        return true;
    }


    public async Task<bool> ExcluirFilialAsync(int id, CancellationToken ct = default)
    {
        // Verificar se filial existe
        var filial = await _filialRepository.ObterPrimeiroAsync(filtro: f => f.IdFilial == id, incluir: e => e.Include(c => c.Endereco), true, ct);
        if (filial == null)
        {
            _notificador.Add($"Filial com ID {id} não encontrada.");
            return false;
        }

        // Verificar se filial possui veículos
        //if (await FilialPossuiVeiculosAsync(id, ct))
        //    throw new InvalidOperationException("Não é possível excluir filial com veículos cadastrados.");

        //// Verificar se filial possui locações ativas
        //if (await FilialPossuiLocacoesAtivasAsync(id, ct))
        //    throw new InvalidOperationException("Filial possui locações ativas. Transfira as locações antes de excluir.");

        // Excluir filial
        await _filialRepository.ExcluirSalvarAsync(filial, ct);
        return true;
        
    }

    public async Task<bool> AtivarFilialAsync(int id, CancellationToken ct = default)
    {
        var filial = await ObterPorId(id, ct);
        if (filial == null)
        {
            _notificador.Add($"Filial com ID {id} não encontrada.");
            return false;
        }
        ;

        if (filial.Ativo)
            return true;

        filial.Ativo = true;
        var atualizado = await _filialRepository.AtualizarSalvarAsync(filial, ct);
        return atualizado;       
    }

    public async Task<bool> DesativarFilialAsync(int id, CancellationToken ct = default)
    {        
        var filial = await ObterPorId(id, ct);
        if (filial == null)
        {
            _notificador.Add($"Filial com ID {id} não encontrada.");
            return false;
        }

        if (!filial.Ativo)
            return true; // Já está inativa

        // Verificar se filial possui veículos
        //if (await FilialPossuiVeiculosAsync(id, ct))
        //    throw new InvalidOperationException("Filial possui veículos. Transfira os veículos antes de desativar.");

        filial.Ativo = false;
        var atualizado = await _filialRepository.AtualizarSalvarAsync(filial, ct);

        return atualizado;       
    }

    //#endregion

    //#region Operações Específicas

    //public async Task<bool> TransferirVeiculoAsync(int veiculoId, int filialOrigemId, int filialDestinoId, CancellationToken ct = default)
    //{
    //    await _unitOfWork.BeginTransactionAsync(ct);

    //    try
    //    {
    //        _logger.LogInformation("Transferindo veículo {VeiculoId} da filial {Origem} para {Destino}",
    //            veiculoId, filialOrigemId, filialDestinoId);

    //        // Verificar se filiais existem e estão ativas
    //        var filialOrigem = await _filialRepository.ObterPorIdAsync(filialOrigemId, ct);
    //        var filialDestino = await _filialRepository.ObterPorIdAsync(filialDestinoId, ct);

    //        if (filialOrigem == null || !filialOrigem.Ativo)
    //            throw new InvalidOperationException("Filial de origem não encontrada ou inativa.");

    //        if (filialDestino == null || !filialDestino.Ativo)
    //            throw new InvalidOperationException("Filial de destino não encontrada ou inativa.");

    //        // Buscar veículo
    //        var veiculo = await _repositorioGlobal.ObterPrimeiroOuDefaultAsync<Veiculo>(
    //            filtro: v => v.IdVeiculo == veiculoId && v.FilialId == filialOrigemId,
    //            ct: ct);

    //        if (veiculo == null)
    //            throw new KeyNotFoundException($"Veículo {veiculoId} não encontrado na filial {filialOrigemId}.");

    //        if (!veiculo.Disponivel)
    //            throw new InvalidOperationException("Não é possível transferir veículo alugado.");

    //        // Transferir veículo
    //        veiculo.FilialId = filialDestinoId;

    //        await _repositorioGlobal.AtualizarAsync(veiculo, ct);
    //        await _unitOfWork.CommitAsync(ct);

    //        _logger.LogInformation("Veículo {VeiculoId} transferido com sucesso", veiculoId);
    //        return true;
    //    }
    //    catch (Exception ex)
    //    {
    //        await _unitOfWork.RollbackAsync(ct);
    //        _logger.LogError(ex, "Erro ao transferir veículo {VeiculoId}", veiculoId);
    //        throw;
    //    }
    //}

    //public async Task<IReadOnlyList<VeiculoDto>> ObterVeiculosDaFilialAsync(int filialId, CancellationToken ct = default)
    //{
    //    try
    //    {
    //        _logger.LogInformation("Buscando veículos da filial {FilialId}", filialId);

    //        var veiculos = await _repositorioGlobal.ObterComFiltroAsync<Veiculo>(
    //            filtro: v => v.FilialId == filialId,
    //            ordenarPor: q => q.OrderBy(v => v.Modelo),
    //            asNoTracking: true,
    //            ct: ct);

    //        // Converter para DTO (ajuste conforme seu VeiculoDto)
    //        return veiculos.Select(v => new VeiculoDto
    //        {
    //            IdVeiculo = v.IdVeiculo,
    //            Modelo = v.Modelo,
    //            Marca = v.Marca,
    //            Placa = v.Placa,
    //            Disponivel = v.Disponivel
    //            // ... outros campos
    //        }).ToList();
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Erro ao buscar veículos da filial {FilialId}", filialId);
    //        throw;
    //    }
    //}

    //public async Task<EstatisticasFilialDto> ObterEstatisticasFilialAsync(int filialId, CancellationToken ct = default)
    //{
    //    try
    //    {
    //        _logger.LogInformation("Buscando estatísticas da filial {FilialId}", filialId);

    //        // Verificar se filial existe
    //        if (!await ExisteFilialAsync(filialId, ct))
    //            throw new KeyNotFoundException($"Filial com ID {filialId} não encontrada.");

    //        // Buscar dados agregados
    //        var veiculos = await _repositorioGlobal.ObterComFiltroAsync<Veiculo>(
    //            filtro: v => v.FilialId == filialId,
    //            asNoTracking: true,
    //            ct: ct);

    //        var totalVeiculos = veiculos.Count;
    //        var veiculosDisponiveis = veiculos.Count(v => v.Disponivel);
    //        var veiculosAlugados = veiculos.Count(v => !v.Disponivel);

    //        // Buscar locações do mês (exemplo)
    //        var inicioMes = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
    //        var fimMes = inicioMes.AddMonths(1).AddDays(-1);

    //        var locacoesMes = await _repositorioGlobal.ObterComFiltroAsync<Locacao>(
    //            filtro: l => l.FilialRetiradaId == filialId &&
    //                        l.DataLocacao >= inicioMes &&
    //                        l.DataLocacao <= fimMes,
    //            asNoTracking: true,
    //            ct: ct);

    //        var totalLocacoesMes = locacoesMes.Count;
    //        var faturamentoMes = locacoesMes.Sum(l => l.ValorTotal);

    //        // Calcular taxa de ocupação
    //        var taxaOcupacao = totalVeiculos > 0
    //            ? (decimal)veiculosAlugados / totalVeiculos * 100
    //            : 0;

    //        return new EstatisticasFilialDto
    //        {
    //            TotalVeiculos = totalVeiculos,
    //            VeiculosDisponiveis = veiculosDisponiveis,
    //            VeiculosAlugados = veiculosAlugados,
    //            TotalLocacoesMes = totalLocacoesMes,
    //            FaturamentoMes = faturamentoMes,
    //            TaxaOcupacao = taxaOcupacao
    //        };
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Erro ao buscar estatísticas da filial {FilialId}", filialId);
    //        throw;
    //    }
    //}

    //public async Task<bool> ValidarFilialParaLocacaoAsync(int filialId, CancellationToken ct = default)
    //{
    //    try
    //    {
    //        var filial = await _filialRepository.ObterPorIdAsync(filialId, ct);

    //        // Filial deve existir e estar ativa
    //        if (filial == null || !filial.Ativo)
    //            return false;

    //        // Filial deve ter veículos disponíveis
    //        var veiculosDisponiveis = await _filialRepository.ContarVeiculosDisponiveisNaFilialAsync(filialId, ct);
    //        return veiculosDisponiveis > 0;
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Erro ao validar filial para locação: {FilialId}", filialId);
    //        throw;
    //    }
    //}

    //public async Task<bool> FilialPossuiVeiculosAsync(int filialId, CancellationToken ct = default)
    //{
    //    try
    //    {
    //        var totalVeiculos = await _filialRepository.ContarVeiculosNaFilialAsync(filialId, ct);
    //        return totalVeiculos > 0;
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Erro ao verificar se filial possui veículos: {FilialId}", filialId);
    //        throw;
    //    }
    //}

    //public async Task<bool> FilialPossuiLocacoesAtivasAsync(int filialId, CancellationToken ct = default)
    //{
    //    try
    //    {
    //        // Buscar locações ativas ou atrasadas na filial
    //        var locacoesAtivas = await _repositorioGlobal.ExisteAsync<Locacao>(
    //            l => (l.FilialRetiradaId == filialId || l.FilialDevolucaoId == filialId) &&
    //                (l.Status == StatusLocacao.Ativa || l.Status == StatusLocacao.Atrasada),
    //            ct);

    //        return locacoesAtivas;
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Erro ao verificar se filial possui locações ativas: {FilialId}", filialId);
    //        throw;
    //    }
    //}

    //#endregion

    //#region Validações

    public async Task<bool> ValidarCriacaoFilialAsync(CriarFilialDto filialDto, CancellationToken ct = default)
    {
        var existe = await _filialRepository.ExisteAsync(f => f.Nome == filialDto.Nome, ct);

        if (existe)
        {
            _notificador.Add("Já existe uma filial com este nome.");
            return false;
        }

        return true;
    }

    public async Task<bool> ValidarAtualizacaoFilialAsync(int id, AtualizarFilialDto filialDto, CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(filialDto.Nome))
        {
            var nomeExiste = await _filialRepository.ExisteAsync(filtro:s=>s.Nome == filialDto.Nome, ct);
            if (nomeExiste)
            {
                _notificador.Add("Já existe uma filial com este nome.");
                return false;
            }
        }
        return true;
    }

    //#endregion

    //#region Métodos Auxiliares

    //public async Task<int> ContarVeiculosNaFilialAsync(int filialId, CancellationToken ct = default)
    //{
    //    return await _filialRepository.ContarVeiculosNaFilialAsync(filialId, ct);
    //}

    //public async Task<int> ContarVeiculosDisponiveisNaFilialAsync(int filialId, CancellationToken ct = default)
    //{
    //    return await _filialRepository.ContarVeiculosDisponiveisNaFilialAsync(filialId, ct);
    //}

    //#endregion
}