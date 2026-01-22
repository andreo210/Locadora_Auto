using Locadora_Auto.Application.Models;
using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Application.Models.Mappers;
using Locadora_Auto.Application.Services.Notificador;
using Locadora_Auto.Application.Services.VeiculoServices;
using Locadora_Auto.Domain.IRepositorio;
using Microsoft.EntityFrameworkCore;

public class VeiculoService : IVeiculoService
{
    private readonly IVeiculosRepository _veiculoRepository;
    private readonly ICategoriaVeiculosRepository _categoriaRepository;
    private readonly IFilialRepository _filialRepository;
    private readonly INotificador _notificador;

    public VeiculoService(
        IVeiculosRepository veiculoRepository,
        ICategoriaVeiculosRepository categoriaRepository,
        IFilialRepository filialRepository,
        INotificador notificador)
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

    public async Task<IReadOnlyList<VeiculoDto>> ObterTodosAsync(CancellationToken ct = default)
    {
        var veiculos = await _veiculoRepository.ObterAsync(
            incluir: q => q.Include(v => v.Categoria)
                           .Include(v => v.FilialAtual),
            ct: ct);

        return veiculos.Select(v => v.ToDto()).ToList();
    }

    public async Task<IReadOnlyList<VeiculoDto>> ObterDisponiveisAsync(int? idFilial = null, CancellationToken ct = default)
    {
        var veiculos = await _veiculoRepository.ObterAsync(
            filtro: v => v.Ativo && v.Disponivel && (idFilial == null || v.FilialAtualId == idFilial),
            incluir: q => q.Include(v => v.Categoria)
                           .Include(v => v.FilialAtual),
            ct: ct);

        return veiculos.Select(v => v.ToDto()).ToList();
    }

    #endregion

    #region CRUD

    public async Task<VeiculoDto?> CriarAsync(CriarVeiculoDto dto, CancellationToken ct = default)
    {
        if (await _veiculoRepository.ExisteAsync(v => v.Placa == dto.Placa, ct))
        {
            _notificador.Add(new Notificacao("Placa já cadastrada"));
            return null;
        }

        if (dto.KmInicial < 0)
        {
            _notificador.Add(new Notificacao("Km inicial inválido"));
            return null;
        }

        if (!await _categoriaRepository.ExisteAsync(c => c.Id == dto.IdCategoria, ct))
        {
            _notificador.Add(new Notificacao("Categoria não encontrada"));
            return null;
        }

        if (!await _filialRepository.ExisteAsync(f => f.IdFilial == dto.IdFilialAtual, ct))
        {
            _notificador.Add(new Notificacao("Filial não encontrada"));
            return null;
        }

        var veiculo = dto.ToEntity();

        await _veiculoRepository.InserirSalvarAsync(veiculo, ct);

        return await ObterPorIdAsync(veiculo.IdVeiculo, ct);
    }

    public async Task<bool> AtualizarAsync(int id, AtualizarVeiculoDto dto, CancellationToken ct = default)
    {
        var veiculo = await _veiculoRepository.ObterPrimeiroAsync(
            v => v.IdVeiculo == id,
            rastreado: true,
            ct: ct);

        if (veiculo == null)
        {
            _notificador.Add(new Notificacao("Veículo não encontrado"));
            return false;
        }

        if (dto.KmAtual.HasValue && dto.KmAtual.Value < veiculo.KmAtual)
        {
            _notificador.Add(new Notificacao("Km não pode ser menor que o atual"));
            return false;
        }

        if (dto.Marca != null) veiculo.Marca = dto.Marca.Trim();
        if (dto.Modelo != null) veiculo.Modelo = dto.Modelo.Trim();
        if (dto.Ano.HasValue) veiculo.Ano = dto.Ano.Value;
        if (dto.KmAtual.HasValue) veiculo.KmAtual = dto.KmAtual.Value;
        if (dto.IdFilialAtual.HasValue) veiculo.FilialAtualId = dto.IdFilialAtual.Value;

        await _veiculoRepository.SalvarAsync(ct);
        return true;
    }

    public async Task<bool> AtivarAsync(int id, CancellationToken ct = default)
    {
        var veiculo = await _veiculoRepository.ObterPorId(id);
        if (veiculo == null)
        {
            _notificador.Add(new Notificacao("Veículo não encontrado"));
            return false;
        }

        veiculo.Ativo = true;
        veiculo.Disponivel = true;
        return await _veiculoRepository.AtualizarSalvarAsync(veiculo, ct);
    }

    public async Task<bool> DesativarAsync(int id, CancellationToken ct = default)
    {
        var veiculo = await _veiculoRepository.ObterPorId(id);
        if (veiculo == null)
        {
            _notificador.Add(new Notificacao("Veículo não encontrado"));
            return false;
        }

        veiculo.Ativo = false;
        veiculo.Disponivel = false;
        return await _veiculoRepository.AtualizarSalvarAsync(veiculo, ct);
    }

    #endregion
}
