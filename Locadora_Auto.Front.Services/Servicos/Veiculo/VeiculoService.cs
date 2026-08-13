using Locadora_Auto.Front.Models.Request.Veiculo;
using Locadora_Auto.Front.Models.Response;
using System.Net;

namespace Locadora_Auto.Front.Services.Servicos.Veiculo
{
    public class VeiculoService : IVeiculoService
    {
        private const string RotaBase = "api/v1/veiculos";

        private readonly IApiHttpService _api;

        public VeiculoService(IApiHttpService api)
        {
            _api = api;
        }

        public async Task<VeiculoResponse?> Inserir(CriarVeiculoRequest request)
        {
            var (objeto, code) = await _api.PostAsync<VeiculoResponse, CriarVeiculoRequest>(RotaBase, request);
            if (code == HttpStatusCode.Created || code == HttpStatusCode.OK)
            {
                return objeto;
            }
            return null;
        }

        public async Task<bool?> Atualizar(int id, EditarVeiculoRequest request)
        {
            return await _api.PutAsync<EditarVeiculoRequest>($"{RotaBase}/{id}", request);
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

        public async Task<VeiculoResponse?> ObterPorId(int id)
        {
            return await _api.GetAsync<VeiculoResponse>($"{RotaBase}/{id}");
        }

        public async Task<PaginatedResponse<VeiculoResponse>> ObterTodos(
            string? termo = null,
            int? idCategoria = null,
            int? idFilial = null,
            int? idStatus = null,
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

            if (idCategoria.HasValue)
                queryParams.Add($"idCategoria={idCategoria}");

            if (idFilial.HasValue)
                queryParams.Add($"idFilial={idFilial}");

            if (idStatus.HasValue)
                queryParams.Add($"idStatus={idStatus}");

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

            return await _api.GetAsync<PaginatedResponse<VeiculoResponse>>(url)
                   ?? new PaginatedResponse<VeiculoResponse>();
        }

        public async Task<List<VeiculoResponse>> ObterDisponiveis(int? idFilial = null)
        {
            var url = idFilial.HasValue
                ? $"{RotaBase}/disponiveis?idFilial={idFilial}"
                : $"{RotaBase}/disponiveis";

            return await _api.GetAsync<List<VeiculoResponse>>(url) ?? new();
        }

        // ========================= MANUTENÇÃO =========================

        public async Task<List<ManutencaoResponse>> ObterManutencoes(int id)
        {
            return await _api.GetAsync<List<ManutencaoResponse>>($"{RotaBase}/{id}/manutencao") ?? new();
        }

        public async Task<bool> IniciarManutencao(int id, IniciarManutencaoRequest request)
        {
            var url = $"{RotaBase}/{id}/manutencao/iniciar-manutencao";
            var (_, code) = await _api.PostAsync<bool, IniciarManutencaoRequest>(url, request);
            return EhSucesso(code);
        }

        public async Task<bool> TerminarManutencao(int id, TerminarManutencaoRequest request)
        {
            var url = $"{RotaBase}/{id}/manutencao/terminar-manutencao";
            var (_, code) = await _api.PostAsync<bool, TerminarManutencaoRequest>(url, request);
            return EhSucesso(code);
        }

        public async Task<bool> CancelarManutencao(int id, int idManutencao)
        {
            var url = $"{RotaBase}/{id}/manutencao/cancelar-manutencao/{idManutencao}";
            var (_, code) = await _api.PostAsync<bool, object>(url, new { });
            return EhSucesso(code);
        }

        public async Task<bool> AtualizarManutencao(int id, AtualizarManutencaoRequest request)
        {
            var url = $"{RotaBase}/{id}/manutencao/atualizar-manutencao";
            var (_, code) = await _api.PostAsync<bool, AtualizarManutencaoRequest>(url, request);
            return EhSucesso(code);
        }

        private static bool EhSucesso(HttpStatusCode code) =>
            code == HttpStatusCode.Created || code == HttpStatusCode.OK || code == HttpStatusCode.NoContent;
    }
}
