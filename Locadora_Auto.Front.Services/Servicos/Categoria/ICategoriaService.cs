using Locadora_Auto.Front.Models.Request.Categoria;
using Locadora_Auto.Front.Models.Request.Cliente;
using Locadora_Auto.Front.Models.Response;
using Microsoft.AspNetCore.Components.Forms;

namespace Locadora_Auto.Front.Services.Servicos.Funcionario
{
    public interface ICategoriaService
    {
        Task<CategoriaResponse?> Inserir(CriarCategoriaRequest request);
        Task<bool> UploadFotos(int categoriaId, List<IBrowserFile> fotos);
        Task<bool?> Atualizar(int id, AtualizarCategoriaRequest request);
        Task<bool> Excluir(string id);
        Task<PaginatedResponse<CategoriaResponse>> ObterTodos(
        string? nome = null,
        int pagina = 1,
        int itensPorPagina = 10,
        CancellationToken ct = default);
        Task<CategoriaResponse> ObterPorId(string id);
    }
}
