using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Application.Models.Mappers;
using Locadora_Auto.Domain.Entidades.Indentity;
using Locadora_Auto.Domain.IRepositorio;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Locadora_Auto.Application.Services.OAuth.Users
{
    public class UserService : IUserService
    {
        private readonly UserManager<User> _userManager;
        private readonly IUserRepository _userRepository;

        public UserService(
            UserManager<User> userManager,
            IUserRepository userRepository)
        {
            _userManager = userManager;
            _userRepository = userRepository;
        }

        public async Task<UserDto> CriarAsync(CreateUserDto dto)
        {
            var user = dto.CreateToEntity();

            var result = await _userManager.CreateAsync(user, dto.Password);

            if (!result.Succeeded)
                throw new InvalidOperationException(
                    string.Join(", ", result.Errors.Select(e => e.Description))
                );

            return user.ToDto();
        }


        public async Task DesativarAsync(string id)
        {
            await _userRepository.ExcluirAsync(id);
        }

        public async Task<IEnumerable<UserDto>> ListarAsync()
        {
            var model = await _userRepository.ObterTodos().ToListAsync();
             return model.ToDtoList();
        }

        public async Task<UserDto?> ObterPorCpfAsync(string cpf)
        {
            var UserName = cpf;
            var user = await _userRepository.ObterPrimeiroAsync(x => x.UserName == UserName);
            return user!.ToDto();
        }

        public async Task<UserDto?> ObterPorIdAsync(string id)
        {
            var user = await _userRepository.ObterPorId(id);
            return user!.ToDto();
        }

        public virtual async Task<bool> AtualizarAsync(string id)
        {
            var model = await _userRepository.ObterPorId(id);
            return await _userRepository.AtualizarAsync(model);
        }
    }
    
}
