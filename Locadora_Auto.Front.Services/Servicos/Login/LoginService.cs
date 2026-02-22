using Locadora_Auto.Front.Models.OAuth;
using Locadora_Auto.Front.Models.Response;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Nodes;

namespace Locadora_Auto.Front.Services.Servicos.Login
{
    public class LoginService : ILoginService
    {
        private readonly IApiHttpService _api;
        private readonly IHttpContextAccessor _contextAccessor;

        public LoginService(IApiHttpService api, IHttpContextAccessor contextAccessor = null)
        {
            _api = api;
            _contextAccessor = contextAccessor;
        }

        public async Task<(ClaimsPrincipal? Principal, string? AccessToken, string? RefreshToken)> Login(LoginRequest request)
        {
            var response = await _api.PostAsync<TokenResponse, LoginRequest>("api/v1/Users/autenticar", request);

            if (response == null || string.IsNullOrWhiteSpace(response.AccessToken))
                return (null, null, null);

            var token = response.AccessToken;
            var handler = new JwtSecurityTokenHandler();

            var jwt = handler.ReadJwtToken(token);
            var kid = jwt.Header.Kid;

            // Buscar JWKS
            using var http = new HttpClient();
            var jwksJson = await _api.GetAsync<JsonObject>(".well-known/jwks.json");

            var jwks = new JsonWebKeySet(jwksJson.ToJsonString());

            var key = jwks.Keys.FirstOrDefault(k => k.Kid == kid);

            if (key == null)
                return (null, null, null);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = "https://localhost:61977",

                ValidateAudience = true,
                ValidAudience = "locadora-front",

                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key
            };

            try
            {
                var principal = handler.ValidateToken(token, validationParameters, out _);
                return (principal, token,response.RefreshToken);
            }
            catch
            {
                return (null, null, null);
            }
        }
    }

    public static class JwtParser
    {
        public static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
        {
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(jwt);

            return token.Claims;
        }
    }
}
