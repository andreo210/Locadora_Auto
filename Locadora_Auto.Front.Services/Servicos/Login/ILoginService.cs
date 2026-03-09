using Locadora_Auto.Front.Models.OAuth;
using Locadora_Auto.Front.Models.Response;
using System.Security.Claims;

namespace Locadora_Auto.Front.Services.Servicos.Login
{
    public interface ILoginService
    {
        Task<(ClaimsPrincipal? Principal, string? AccessToken, string? RefreshToken)> Login(LoginRequest request);
    }
}
