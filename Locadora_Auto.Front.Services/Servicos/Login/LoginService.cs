using Locadora_Auto.Front.Models.OAuth;
using Locadora_Auto.Front.Models.Response;

namespace Locadora_Auto.Front.Services.Servicos.Login
{
    public class LoginService : ILoginService
    {
        private readonly IApiHttpService _api;

        public LoginService(IApiHttpService api) 
        {
            _api = api;
        }

        public async Task<TokenResponse?> Login(LoginRequest request)
        {
            return await _api.PostAsync<TokenResponse, LoginRequest>("api/auth/login", request);
        }
    }
}
