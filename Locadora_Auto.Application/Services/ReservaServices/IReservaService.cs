using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Domain;

namespace Locadora_Auto.Application.Services.ReservaServices
{
    public interface IReservaService
    {
        #region Consultas
        Task<ReservaDto?> ObterPorIdAsync(int id, CancellationToken ct = default);
        Task<PaginatedResult<ReservaDto>> ObterTodosPaginadoAsync(
            ConsultaPaginadaRequest consulta,
            int? status = null,
            int? idFilial = null,
            int? idCliente = null,
            CancellationToken ct = default);
        Task<IReadOnlyList<ReservaDto>> ObterPorClienteAsync(int idCliente, CancellationToken ct = default);
        #endregion Consultas

        #region Gravacao
        Task<ReservaDto?> CriarAsync(CriarReservaDto dto, CancellationToken ct = default);
        Task<bool> CancelarAsync(int id, CancellationToken ct = default);
        Task<bool> FinalizarAsync(int id, CancellationToken ct = default);

        /// <summary>
        /// Marca como Expirado toda reserva ainda Reservado cuja data de início já passou.
        /// Retorna quantas foram expiradas.
        /// </summary>
        Task<int> ExpirarVencidasAsync(CancellationToken ct = default);
        #endregion Gravacao
    }
}
