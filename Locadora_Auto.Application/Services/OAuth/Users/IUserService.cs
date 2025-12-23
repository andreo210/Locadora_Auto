using Locadora_Auto.Application.Models.Dto;

namespace Locadora_Auto.Application.Services.OAuth.Users
{
    public interface IUserService
    {
        Task<UserDto> CriarAsync(CreateUserDto dto);
        Task<UserDto?> ObterPorIdAsync(string id);
        Task<UserDto?> ObterPorCpfAsync(string cpf);
        Task<IEnumerable<UserDto>> ListarAsync();
        Task DesativarAsync(string id);
        Task<bool> AtualizarAsync(string id);
    }

}
