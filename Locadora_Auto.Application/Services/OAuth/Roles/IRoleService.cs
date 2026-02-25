using Locadora_Auto.Application.Models.Dto;

namespace Locadora_Auto.Application.Services.OAuth.Roles
{
    public interface IRoleService
    {
        Task<RoleDto> CriarRolesAsync(string nome, string descricao, CancellationToken ct = default);
        Task AtribuirRoleAsync(string userId, string roleName);
        Task RemoverRoleAsync(string userId, string roleName);
        Task<IList<string>> ObterRolesAsync(string userId);
        Task<IList<RoleDto>> ObterTodasRoles();
    }
}
