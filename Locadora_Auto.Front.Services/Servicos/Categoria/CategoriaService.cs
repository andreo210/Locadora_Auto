using Locadora_Auto.Front.Models.Request.Categoria;
using Locadora_Auto.Front.Models.Request.Cliente;
using Locadora_Auto.Front.Models.Response;
using System.Net;
using System.Net.Http;
using System.Text.Json;

namespace Locadora_Auto.Front.Services.Servicos.Funcionario
{
    public class CategoriaService : ICategoriaService
    {
        private readonly IApiHttpService _api;

        public CategoriaService(IApiHttpService api)
        {
            _api = api;
        }

        public async Task<CategoriaResponse?> Inserir(CriarCategoriaRequest request)
        {
            var (objeto, code) = await _api.PostAsync<CategoriaResponse, CriarCategoriaRequest>("api/v1/categorias-veiculos", request);
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

        public async Task<PaginatedResponse<CategoriaResponse>> ObterTodos(
        string? nome = null,
        int pagina = 1,
        int itensPorPagina = 10,
        CancellationToken ct = default)
        {
            
            // Construir query string
            var queryParams = new List<string>();

            if (!string.IsNullOrWhiteSpace(nome))
                queryParams.Add($"nome={Uri.EscapeDataString(nome)}");
;

            // Adicionar paginação
            queryParams.Add($"pagina={pagina}");
            queryParams.Add($"itensPorPagina={itensPorPagina}");


            var queryString = queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
            var url = $"api/v1/categorias-veiculos{queryString}";

            return await _api.GetAsync<PaginatedResponse<CategoriaResponse>>(url);
        }

    }
}
