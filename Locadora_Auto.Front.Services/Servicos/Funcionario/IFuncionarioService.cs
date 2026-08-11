using Locadora_Auto.Front.Models.Request.Funcionario;
using Locadora_Auto.Front.Models.Response;

namespace Locadora_Auto.Front.Services.Servicos.Funcionario
{
    public interface IFuncionarioService
    {
        Task<FuncionarioResponse?> Inserir(FuncionarioRequest request);
        Task<bool?> Atualizar(int id, FuncionarioEditarRequest request);
        Task<List<RoleResponse?>> ObterTodasRoles();
        Task<bool> Excluir(string id);
        Task<PaginatedResponse<FuncionarioResponse>> ObterTodos(
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
        Task<FuncionarioResponse> ObterPorId(string id);
    }
}
