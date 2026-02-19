using Locadora_Auto.Front.Models.OAuth;
using Locadora_Auto.Front.Models.Response;

namespace Locadora_Auto.Front.Services.Servicos.Login
{
    public interface ILoginService
    {
        Task<TokenResponse?> Login(LoginRequest request);
    }
}
