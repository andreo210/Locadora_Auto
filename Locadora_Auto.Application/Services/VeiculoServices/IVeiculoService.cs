using Locadora_Auto.Application.Models.Dto;

namespace Locadora_Auto.Application.Services.VeiculoServices
{
    public interface IVeiculoService
    {
        Task<VeiculoDto?> ObterPorIdAsync(int id, CancellationToken ct = default);
        Task<IReadOnlyList<VeiculoDto>> ObterTodosAsync(CancellationToken ct = default);
        Task<IReadOnlyList<VeiculoDto>> ObterDisponiveisAsync(int? idFilial = null, CancellationToken ct = default);

        Task<VeiculoDto?> CriarAsync(CriarVeiculoDto dto, CancellationToken ct = default);
        Task<bool> AtualizarAsync(int id, AtualizarVeiculoDto dto, CancellationToken ct = default);

        Task<bool> AtivarAsync(int id, CancellationToken ct = default);
        Task<bool> DesativarAsync(int id, CancellationToken ct = default);
    }

}
