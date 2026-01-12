using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Domain.Entidades.Indentity;
using Microsoft.AspNetCore.Identity;

namespace Locadora_Auto.Application.Services.OAuth.Users
{
    public interface IUserService
    {
        Task<UserDto> CriarAsync(CreateUserDto dto);
        Task<UserDto?> ObterPorIdAsync(string id);
        Task<UserDto?> ObterPorCpfAsync(string cpf);
        Task<IEnumerable<UserDto>> ListarAsync();
        //Task DesativarAsync(string id);
        Task<bool> AtualizarAsync(string id);
        Task<UserDto?> ObterPorEmail(string email);
        Task<SignInResult> LoginAsync(LoginDto dto);
        Task<User?> ObterPorCpf(string cpf);
        Task<User> DesativarToken(string refreshToken);
    }

}
