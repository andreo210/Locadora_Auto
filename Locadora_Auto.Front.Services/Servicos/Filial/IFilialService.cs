using Locadora_Auto.Front.Models.Request.Filial;
using Locadora_Auto.Front.Models.Response;

namespace Locadora_Auto.Front.Services.Servicos.Filial
{
    public interface IFilialService
    {
        Task<FilialResponse?> Inserir(CriarFilialRequest request);
        Task<PaginatedResponse<FilialResponse>> ObterTodos(string? nome = null,int pagina = 1, int itensPorPagina = 10, CancellationToken ct = default);
        Task<bool> Excluir(string id);
        Task<FilialResponse> ObterPorId(string id);
    }
}
