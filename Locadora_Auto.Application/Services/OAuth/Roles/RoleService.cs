using Locadora_Auto.Domain.Entidades.Indentity;
using Microsoft.AspNetCore.Identity;

namespace Locadora_Auto.Application.Services.OAuth.Roles
{
    public class RoleService : IRoleService
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public RoleService(
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task CriarRoleAsync(string roleName)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                var result = await _roleManager.CreateAsync(new IdentityRole(roleName));

                if (!result.Succeeded)
                    throw new InvalidOperationException(string.Join(", ",
                        result.Errors.Select(e => e.Description)));
            }
        }

        public async Task AtribuirRoleAsync(string userId, string roleName)
        {
            var user = await _userManager.FindByIdAsync(userId)
                ?? throw new KeyNotFoundException("Usuário não encontrado");

            if (!await _roleManager.RoleExistsAsync(roleName))
                throw new KeyNotFoundException("Role não existe");

            if (!await _userManager.IsInRoleAsync(user, roleName))
            {
                var result = await _userManager.AddToRoleAsync(user, roleName);

                if (!result.Succeeded)
                    throw new InvalidOperationException(string.Join(", ",
                        result.Errors.Select(e => e.Description)));
            }
        }

        public async Task RemoverRoleAsync(string userId, string roleName)
        {
            var user = await _userManager.FindByIdAsync(userId)
                ?? throw new KeyNotFoundException("Usuário não encontrado");

            await _userManager.RemoveFromRoleAsync(user, roleName);
        }

        public async Task<IList<string>> ObterRolesAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId)
                ?? throw new KeyNotFoundException("Usuário não encontrado");

            return await _userManager.GetRolesAsync(user);
        }
    }

}
