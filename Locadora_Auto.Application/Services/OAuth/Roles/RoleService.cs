using Locadora_Auto.Application.Configuration.Ultils.NotificadorServices;
using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Domain.Entidades.Indentity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Polly;

namespace Locadora_Auto.Application.Services.OAuth.Roles
{
    public class RoleService : IRoleService
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly INotificadorService _notificador;

        public RoleService(
            UserManager<User> userManager,
            INotificadorService notificador,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _notificador = notificador;
        }

        public async Task<RoleDto> CriarRolesAsync(string nome, CancellationToken ct = default)
        {
            // Verificar se a role já existe
            var roleExists = await _roleManager.RoleExistsAsync(nome);
            if (roleExists)
            {
                _notificador.Add("Esta role já existe");
            }
            // Criar a nova role
            var role = new IdentityRole
            {
                Name = nome,
                NormalizedName = nome.ToUpper()
            };

            var result = await _roleManager.CreateAsync(role);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    _notificador.Add(error.Description);
                }
            }

            var resposta = new RoleDto
            {
                Id = role.Id,
                Nome = role.Name
            };
            return resposta;

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

        public async Task<IList<RoleDto>> ObterTodasRoles()
        {
            var roles = await _roleManager.Roles.ToListAsync();
            var rolesResponse = new List<RoleDto>();

            foreach (var role in roles)
            {

                rolesResponse.Add(new RoleDto
                {
                    Id = role.Id,
                    Nome = role.Name
                });
            }

            return rolesResponse;
        }
    }

}
