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

        #region Manutencao
        Task<bool> IniciarManutencao(int id, CriarManutencaoDto dto, CancellationToken ct = default);
        Task<bool> TerminaManutencao(int id, TerminarManutencaoDto dto, CancellationToken ct = default);
        Task<bool> CancelarManutencao(int id, int idManutencao, CancellationToken ct = default);
        Task<bool> AtualizarDescricaoManutencao(int id, AtualizarManutencaoDto dto, CancellationToken ct = default);
        #endregion Manutencao
    }

}
