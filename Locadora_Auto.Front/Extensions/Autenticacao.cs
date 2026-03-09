using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Locadora_Auto.Front.Extensions
{
    public static class AutenticacaoExtensions
    {
        public static IServiceCollection AddAutenticacao(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "Cookies";
                options.DefaultChallengeScheme = "Cookies";
                options.DefaultSignInScheme = "Cookies";
            })
             .AddCookie("Cookies", options =>
             {
                 options.LoginPath = "/login";
                 options.AccessDeniedPath = "/acesso-negado";
                 options.ExpireTimeSpan = TimeSpan.FromHours(2);
                 options.SlidingExpiration = true;
                 options.Cookie.SameSite = SameSiteMode.Lax;
                 options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
             });

            services.AddAuthorization();
            services.AddCascadingAuthenticationState();

            return services;
        }
    }
}
