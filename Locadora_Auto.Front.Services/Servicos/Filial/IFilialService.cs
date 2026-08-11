using Locadora_Auto.Front.Models.Request.Filial;
using Locadora_Auto.Front.Models.Response;
using Microsoft.AspNetCore.Components.Forms;

namespace Locadora_Auto.Front.Services.Servicos.Filial
{
    public interface IFilialService
    {
        Task<FilialResponse?> Inserir(CriarFilialRequest request);
        Task<PaginatedResponse<FilialResponse>> ObterTodos(string? nome = null,int pagina = 1, int itensPorPagina = 10, CancellationToken ct = default);
        Task<bool> Excluir(string id);
        Task<FilialResponse> ObterPorId(string id);
        Task<bool?> Atualizar(int id, EditarFilialRequest request);
        Task<bool> UploadFotos(int filialId, List<IBrowserFile> fotos);
        Task<bool> ExcluirFoto(int filialId, int idFoto);
        Task<bool> Ativar(int filialId);
        Task<bool> Desativar(int filialId);
    }
}
