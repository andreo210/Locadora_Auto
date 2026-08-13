using Locadora_Auto.Front.Models.Request.Reserva;
using Locadora_Auto.Front.Models.Response;
using System.Net;

namespace Locadora_Auto.Front.Services.Servicos.Reserva
{
    public class ReservaService : IReservaService
    {
        private const string RotaBase = "api/v1/reservas";

        private readonly IApiHttpService _api;

        public ReservaService(IApiHttpService api)
        {
            _api = api;
        }

        public async Task<ReservaResponse?> Inserir(CriarReservaRequest request)
        {
            var (objeto, code) = await _api.PostAsync<ReservaResponse, CriarReservaRequest>(RotaBase, request);
            if (code == HttpStatusCode.Created || code == HttpStatusCode.OK)
            {
                return objeto;
            }
            return null;
        }

        public async Task<bool> Cancelar(int id)
        {
            return await _api.PatchAsync($"{RotaBase}/{id}/cancelar", new { });
        }

        public async Task<bool> Finalizar(int id)
        {
            return await _api.PatchAsync($"{RotaBase}/{id}/finalizar", new { });
        }

        public async Task<bool> ExpirarVencidas()
        {
            return await _api.PatchAsync($"{RotaBase}/expirar-vencidas", new { });
        }

        public async Task<ReservaResponse?> ObterPorId(int id)
        {
            return await _api.GetAsync<ReservaResponse>($"{RotaBase}/{id}");
        }

        public async Task<List<ReservaResponse>> ObterPorCliente(int idCliente)
        {
            return await _api.GetAsync<List<ReservaResponse>>($"{RotaBase}/cliente/{idCliente}") ?? new();
        }

        public async Task<PaginatedResponse<ReservaResponse>> ObterTodos(
            string? termo = null,
            int? status = null,
            int? idFilial = null,
            int? idCliente = null,
            int pagina = 1,
            int itensPorPagina = 10,
            string? ordenarPor = null,
            string? direcao = null,
            CancellationToken ct = default)
        {
            // Construir query string
            var queryParams = new List<string>();

            if (!string.IsNullOrWhiteSpace(termo))
                queryParams.Add($"termo={Uri.EscapeDataString(termo)}");

            if (status.HasValue)
                queryParams.Add($"status={status.Value}");

            if (idFilial.HasValue)
                queryParams.Add($"idFilial={idFilial.Value}");

            if (idCliente.HasValue)
                queryParams.Add($"idCliente={idCliente.Value}");

            if (!string.IsNullOrWhiteSpace(ordenarPor))
            {
                queryParams.Add($"ordenarPor={Uri.EscapeDataString(ordenarPor)}");
                queryParams.Add($"direcao={(string.IsNullOrWhiteSpace(direcao) ? "asc" : direcao)}");
            }

            // Adicionar paginação
            queryParams.Add($"pagina={pagina}");
            queryParams.Add($"itensPorPagina={itensPorPagina}");

            var queryString = queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
            var url = $"{RotaBase}{queryString}";

            return await _api.GetAsync<PaginatedResponse<ReservaResponse>>(url)
                   ?? new PaginatedResponse<ReservaResponse>();
        }
    }
}
