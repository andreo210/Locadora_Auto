using Blazored.LocalStorage;
using Locadora_Auto.Front.Services.Configuration;
using Locadora_Auto.Front.Services.Servicos.Login;
using Locadora_Auto.Front.Services.Servicos.Notificacao;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Locadora_Auto.Front.Extensions
{
    public static class DependencyInjectionServiceExtensions
    {
        public static IServiceCollection AddServices(this IServiceCollection services,IConfiguration configuration)
        {
            services.Configure<ApiConfig>(configuration.GetSection("ApiConfig"));

            services.AddBlazoredLocalStorage();

            services.AddScoped<JwtAuthorizationHandler>();
            services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();

            services.AddHttpClient<IApiHttpService, ApiHttpService>((provider, client) =>
            {
                var config = configuration.GetSection("ApiConfig").Get<ApiConfig>();
                client.BaseAddress = new Uri(config.BaseUrlApiLocacao);
            })
            .AddHttpMessageHandler<JwtAuthorizationHandler>();

            services.AddScoped<ILoginService, LoginService>();
            services.AddScoped<INotificationService, NotificationService>();

            return services;
        }
    }
}
