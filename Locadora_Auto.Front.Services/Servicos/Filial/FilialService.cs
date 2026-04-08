using Locadora_Auto.Front.Models.Request;
using Locadora_Auto.Front.Models.Request.Categoria;
using Locadora_Auto.Front.Models.Request.Filial;
using Locadora_Auto.Front.Models.Response;
using Microsoft.AspNetCore.Components.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Locadora_Auto.Front.Services.Servicos.Filial
{
    public class FilialService : IFilialService
    {
        private readonly IApiHttpService _api;

        public FilialService(IApiHttpService api)
        {
            _api = api;
        }

        public async Task<FilialResponse?> Inserir(CriarFilialRequest request)
        {
            request.Cidade = request.Endereco.Cidade;
            var (objeto, code) = await _api.PostAsync<FilialResponse, CriarFilialRequest>("api/v1/filiais", request);
            if (code == HttpStatusCode.Created || code == HttpStatusCode.OK)
            {
                return objeto;
            }
            return null;
        }

        public async Task<bool> UploadFotos(int categoriaId, List<IBrowserFile> fotos)
        {
            var url = $"api/v1/categorias-veiculos/{categoriaId}/registrar-foto";
            return await _api.PostMultipartAsync(url, fotos, "fotos");
        }

        public async Task<bool?> Atualizar(int id, AtualizarCategoriaRequest request)
        {
            return await _api.PutAsync<AtualizarCategoriaRequest>($"api/v1/categorias-veiculos/{id}", request);
        }

        public async Task<bool> ExcluirFoto(int categoriaId, int idFoto)
        {
            var url = $"api/v1/categorias-veiculos/{categoriaId}/excluir-foto/{idFoto}";
            return await _api.DeleteAsync(url);
        }


        public async Task<bool> Excluir(string id)
        {
            return await _api.DeleteAsync($"api/v1/categorias-veiculos/{id}");
        }

        public async Task<CategoriaResponse> ObterPorId(string id)
        {
            return await _api.GetAsync<CategoriaResponse>($"api/v1/categorias-veiculos/{id}");
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

            // Adicionar paginação
            queryParams.Add($"pagina={pagina}");
            queryParams.Add($"itensPorPagina={itensPorPagina}");


            var queryString = queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
            var url = $"api/v1/categorias-veiculos{queryString}";

            return await _api.GetAsync<PaginatedResponse<CategoriaResponse>>(url);
        }
    }
}
