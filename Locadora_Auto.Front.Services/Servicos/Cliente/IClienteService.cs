using Locadora_Auto.Front.Models.Request;
using Locadora_Auto.Front.Models.Response;

namespace Locadora_Auto.Front.Services.Servicos.Funcionario
{
    public interface IClienteService
    {
        Task<ClienteResponse?> Inserir(ClienteRequest request);
        Task<bool?> Atualizar(int id, ClienteEditarRequest request);
        Task<bool> Excluir(string id);
        Task<PaginatedResponse<ClienteResponse>> ObterTodos(
        string? nome = null,
        string? cargo = null,
        bool? ativos = null,
        int pagina = 1,
        int itensPorPagina = 10,
         string? ordenarPor = null,
        string? ordem = null,
        CancellationToken ct = default);
        Task<bool> Ativar(string id);
        Task<bool> Desativar(string id);
        Task<ClienteResponse> ObterPorId(string id);
    }
}
