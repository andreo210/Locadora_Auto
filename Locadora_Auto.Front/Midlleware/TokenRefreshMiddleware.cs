using Locadora_Auto.Front.Models.Response;
using Microsoft.AspNetCore.Authentication;
using System.IdentityModel.Tokens.Jwt;

namespace Locadora_Auto.Front.Midlleware
{
    public class TokenRefreshMiddleware
    {
        private readonly RequestDelegate _next;

        public TokenRefreshMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var accessToken = await context.GetTokenAsync("access_token");
                var refreshToken = await context.GetTokenAsync("refresh_token");

                if (!string.IsNullOrEmpty(accessToken))
                {
                    var handler = new JwtSecurityTokenHandler();
                    var jwt = handler.ReadJwtToken(accessToken);

                    // Se expirou
                    if (jwt.ValidTo <= DateTime.Now)
                    {
                        if (!string.IsNullOrEmpty(refreshToken))
                        {

                            var _api = context.RequestServices
                                             .GetRequiredService<IApiHttpService>();

                            var response = await _api.PostAsync<TokenResponse,string>("/api/v1/Users/renovar", refreshToken);

                            if (response.objeto !=null)
                            {
                                var authResult =  await context.AuthenticateAsync("Cookies");

                                authResult.Properties.UpdateTokenValue("access_token", response.objeto.AccessToken);

                                authResult.Properties.UpdateTokenValue("refresh_token",response.objeto.RefreshToken);

                                await context.SignInAsync("Cookies", authResult.Principal, authResult.Properties);
                            }
                            else
                            {
                                await context.SignOutAsync("Cookies");
                            }
                        }
                        else
                        {
                            await context.SignOutAsync("Cookies");
                        }
                    }
                }
            }

            await _next(context);
        }
    }
}
