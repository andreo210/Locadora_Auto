using Locadora_Auto.Front.Models.Request.Filial;
using Locadora_Auto.Front.Models.Response;
using Microsoft.AspNetCore.Components.Forms;
using System.Net;

namespace Locadora_Auto.Front.Services.Servicos.Filial
{
    public class FilialService : IFilialService
    {
        private const string RotaBase = "api/v1/filiais";

        private readonly IApiHttpService _api;

        public FilialService(IApiHttpService api)
        {
            _api = api;
        }

        public async Task<FilialResponse?> Inserir(CriarFilialRequest request)
        {
            request.Cidade = request.Endereco.Cidade;
            var (objeto, code) = await _api.PostAsync<FilialResponse, CriarFilialRequest>(RotaBase, request);
            if (code == HttpStatusCode.Created || code == HttpStatusCode.OK)
            {
                return objeto;
            }
            return null;
        }

        public async Task<bool?> Atualizar(int id, EditarFilialRequest request)
        {
            request.Cidade = request.Endereco?.Cidade;
            return await _api.PutAsync<EditarFilialRequest>($"{RotaBase}/{id}", request);
        }

        public async Task<bool> UploadFotos(int filialId, List<IBrowserFile> fotos)
        {
            var url = $"{RotaBase}/{filialId}/registrar-foto";
            return await _api.PostMultipartAsync(url, fotos, "fotos");
        }

        public async Task<bool> ExcluirFoto(int filialId, int idFoto)
        {
            var url = $"{RotaBase}/{filialId}/excluir-foto/{idFoto}";
            return await _api.DeleteAsync(url);
        }

        public async Task<bool> Ativar(int filialId)
        {
            return await _api.PatchAsync($"{RotaBase}/{filialId}/ativar", new { });
        }

        public async Task<bool> Desativar(int filialId)
        {
            return await _api.PatchAsync($"{RotaBase}/{filialId}/desativar", new { });
        }

        public async Task<bool> Excluir(string id)
        {
            return await _api.DeleteAsync($"{RotaBase}/{id}");
        }

        public async Task<FilialResponse> ObterPorId(string id)
        {
            return await _api.GetAsync<FilialResponse>($"{RotaBase}/{id}");
        }

        public async Task<PaginatedResponse<FilialResponse>> ObterTodos(
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
            var url = $"{RotaBase}{queryString}";

            return await _api.GetAsync<PaginatedResponse<FilialResponse>>(url);
        }
    }
}
