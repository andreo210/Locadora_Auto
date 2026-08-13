using Locadora_Auto.Front.Models.Request.Reserva;
using Locadora_Auto.Front.Models.Response;

namespace Locadora_Auto.Front.Services.Servicos.Reserva
{
    public interface IReservaService
    {
        Task<ReservaResponse?> Inserir(CriarReservaRequest request);
        Task<PaginatedResponse<ReservaResponse>> ObterTodos(
            string? termo = null,
            int? status = null,
            int? idFilial = null,
            int? idCliente = null,
            int pagina = 1,
            int itensPorPagina = 10,
            string? ordenarPor = null,
            string? direcao = null,
            CancellationToken ct = default);
        Task<List<ReservaResponse>> ObterPorCliente(int idCliente);
        Task<ReservaResponse?> ObterPorId(int id);
        Task<bool> Cancelar(int id);
        Task<bool> Finalizar(int id);
        Task<bool> ExpirarVencidas();
    }
}
