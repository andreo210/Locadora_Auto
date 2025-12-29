using Locadora_Auto.Application.Models.Dto;

namespace Locadora_Auto.Application.Services.OAuth.Token
{
    public interface ITokenService
    {
        Task<TokenDto> GerarToken(string email);
    }
}
