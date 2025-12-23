namespace Locadora_Auto.Application.Services.OAuth.Roles
{
    public interface IRoleService
    {
        Task CriarRoleAsync(string roleName);
        Task AtribuirRoleAsync(string userId, string roleName);
        Task RemoverRoleAsync(string userId, string roleName);
        Task<IList<string>> ObterRolesAsync(string userId);
    }
}
