using Locadora_Auto.Front.Models.Request.Cliente;
using Locadora_Auto.Front.Models.Response;
using System.Net;
using System.Net.Http;
using System.Text.Json;

namespace Locadora_Auto.Front.Services.Servicos.Funcionario
{
    public class ClienteService : IClienteService
    {
        private readonly IApiHttpService _api;

        public ClienteService(IApiHttpService api)
        {
            _api = api;
        }

        public async Task<ClienteResponse?> Inserir(CriarClienteRequest request)
        {
            var (objeto, code) = await _api.PostAsync<ClienteResponse, CriarClienteRequest>("api/v1/Clientes", request);
            if (code == HttpStatusCode.Created || code == HttpStatusCode.OK)
            {
                return objeto;
            }
            return null;
        }

        public async Task<bool?> Atualizar(int id,ClienteEditarRequest request)
        {
            return await _api.PutAsync<ClienteEditarRequest>($"api/v1/Clientes/{id}", request);
        }


        public async Task<bool> Excluir(string id)
        {           
            return await _api.DeleteAsync($"api/v1/Clientes/{id}");            
        }

        public async Task<bool> Ativar(string id)
        {
            return await _api.PatchAsync($"api/v1/Clientes/{id}/ativar", id);
        }
        public async Task<bool> Desativar(string id)
        {
            return await _api.PatchAsync($"api/v1/Clientes/{id}/desativar", id);
        }

        public async Task<ClienteResponse> ObterPorId(string id)
        {
            return await _api.GetAsync<ClienteResponse>($"api/v1/Clientes/{id}");
        }

        public async Task<PaginatedResponse<ClienteResponse>> ObterTodos(
        string? nome = null,
        string? cargo = null,
        bool? ativos = null,
        int pagina = 1,
        int itensPorPagina = 10,
         string? ordenarPor = null,
        string? ordem = null,
        CancellationToken ct = default)
        {
            
            // Construir query string
            var queryParams = new List<string>();

            if (!string.IsNullOrWhiteSpace(nome))
                queryParams.Add($"nome={Uri.EscapeDataString(nome)}");

            if (!string.IsNullOrWhiteSpace(cargo))
                queryParams.Add($"cpf={Uri.EscapeDataString(cargo)}");

            if (ativos.HasValue)
                queryParams.Add($"ativos={ativos.Value.ToString().ToLower()}");

            // Adicionar paginação
            queryParams.Add($"pagina={pagina}");
            queryParams.Add($"itensPorPagina={itensPorPagina}");

            // ordenação
            if (!string.IsNullOrWhiteSpace(ordenarPor))
            {
                queryParams.Add($"ordenarPor={ordenarPor}");
                queryParams.Add($"ordem={ordem}");
            }
            var queryString = queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
            var url = $"api/v1/Clientes/obter-clientes-paginado/{queryString}";

            return await _api.GetAsync<PaginatedResponse<ClienteResponse>>(url);
        }

    }
}
