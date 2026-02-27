using Locadora_Auto.Front.Models.Request;
using Locadora_Auto.Front.Models.Response;

namespace Locadora_Auto.Front.Services.Servicos.Funcionario
{
    public interface IFuncionarioService
    {
        Task<FuncionarioResponse?> Inserir(FuncionarioRequest request);
        Task<List<RoleResponse?>> ObterTodasRoles();
        //Task<List<FuncionarioResponse>?> ObterTodos(string? url = null);
        Task<PaginatedResponse<FuncionarioResponse>> ObterTodos(
        string? nome = null,
        string? cargo = null,
        bool? ativos = null,
        int pagina = 1,
        int itensPorPagina = 10,
         string? ordenarPor = "Matricula",
        string? ordem = "asc",
        CancellationToken ct = default);
    }
}
