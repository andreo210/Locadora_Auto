using Locadora_Auto.Application.Models.Dto;

namespace Locadora_Auto.Application.Services.SeguroServices
{
    public interface ISeguroService
    {
        #region Consultas
        Task<SeguroDto?> ObterPorIdAsync(int id, CancellationToken ct = default);
        Task<IReadOnlyList<SeguroDto>> ObterTodosAsync(CancellationToken ct = default);
        Task<IReadOnlyList<SeguroDto>> ObterSeguroAtivoAsync(CancellationToken ct = default);

        #endregion Consultas

        #region Gravacao
        Task<SeguroDto?> CriarAsync(CriarOuAtualizarSeguroDto dto, CancellationToken ct = default);
        Task<bool> AtualizarAsync(int id, CriarOuAtualizarSeguroDto dto, CancellationToken ct = default);
        Task<bool> AtivarAsync(int id, CancellationToken ct = default);
        Task<bool> DesativarAsync(int id, CancellationToken ct = default);
        #endregion Gravacao
    }
}
