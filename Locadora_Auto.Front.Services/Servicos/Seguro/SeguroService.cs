using Locadora_Auto.Front.Models.Request.Seguro;
using Locadora_Auto.Front.Models.Response;
using System.Net;

namespace Locadora_Auto.Front.Services.Servicos.Seguro
{
    public class SeguroService : ISeguroService
    {
        private const string RotaBase = "api/v1/seguros";

        private readonly IApiHttpService _api;

        public SeguroService(IApiHttpService api)
        {
            _api = api;
        }

        public async Task<SeguroResponse?> Inserir(CriarSeguroRequest request)
        {
            var (objeto, code) = await _api.PostAsync<SeguroResponse, CriarSeguroRequest>(RotaBase, request);
            if (code == HttpStatusCode.Created || code == HttpStatusCode.OK)
            {
                return objeto;
            }
            return null;
        }

        public async Task<bool?> Atualizar(int id, EditarSeguroRequest request)
        {
            return await _api.PutAsync<EditarSeguroRequest>($"{RotaBase}/{id}", request);
        }

        public async Task<bool> Excluir(int id)
        {
            return await _api.DeleteAsync($"{RotaBase}/{id}");
        }

        public async Task<bool> Ativar(int id)
        {
            return await _api.PatchAsync($"{RotaBase}/{id}/ativar", new { });
        }

        public async Task<bool> Desativar(int id)
        {
            return await _api.PatchAsync($"{RotaBase}/{id}/desativar", new { });
        }

        public async Task<SeguroResponse?> ObterPorId(int id)
        {
            return await _api.GetAsync<SeguroResponse>($"{RotaBase}/{id}");
        }

        public async Task<PaginatedResponse<SeguroResponse>> ObterTodos(
            string? termo = null,
            bool? ativo = null,
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

            if (ativo.HasValue)
                queryParams.Add($"ativo={ativo.Value.ToString().ToLower()}");

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

            return await _api.GetAsync<PaginatedResponse<SeguroResponse>>(url)
                   ?? new PaginatedResponse<SeguroResponse>();
        }

        public async Task<List<SeguroResponse>> ObterAtivos()
        {
            return await _api.GetAsync<List<SeguroResponse>>($"{RotaBase}/obter-ativos") ?? new();
        }
    }
}
