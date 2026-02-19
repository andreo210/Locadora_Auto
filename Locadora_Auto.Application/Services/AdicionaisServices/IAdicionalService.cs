using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Domain.Entidades;

namespace Locadora_Auto.Application.Services.AdicionaisServices
{
    public interface IAdicionalService
    {
        Task<AdicionalDto?> ObterPorIdAsync(int idLocacao, CancellationToken ct = default);
        Task<Adicional?> ObterPorIdRastreado(int idAdicional, CancellationToken ct = default);
        Task<IReadOnlyList<AdicionalDto>> ObterTodosAsync(CancellationToken ct = default);
        Task<IReadOnlyList<AdicionalDto>> ObterSeguroAtivoAsync(CancellationToken ct = default);
        Task<AdicionalDto?> CriarAsync(CriarAtualizarAdicionalDto dto, CancellationToken ct = default);
        Task<bool> AtualizarAsync(int id, CriarAtualizarAdicionalDto dto, CancellationToken ct = default);
        Task<bool> AtivarAsync(int id, CancellationToken ct = default);
        Task<bool> DesativarAsync(int id, CancellationToken ct = default);       
    }
}
