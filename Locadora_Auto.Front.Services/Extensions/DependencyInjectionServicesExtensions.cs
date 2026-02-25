using Blazored.LocalStorage;
using Locadora_Auto.Front.Services.Configuration;
using Locadora_Auto.Front.Services.Servicos.Funcionario;
using Locadora_Auto.Front.Services.Servicos.Login;
using Locadora_Auto.Front.Services.Servicos.Notificacao;
using Locadora_Auto.Front.Services.Usuarios;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Locadora_Auto.Front.Extensions
{
    public static class DependencyInjectionServiceExtensions
    {
        public static IServiceCollection AddServices(this IServiceCollection services,IConfiguration configuration)
        {
            // Primeiro, registre os serviços de notificação
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IConfirmDialogService, ConfirmDialogService>();


            services.Configure<ApiConfig>(configuration.GetSection("ApiConfig"));
            services.AddHttpContextAccessor();


            services.AddBlazoredLocalStorage();

            services.AddScoped<JwtAuthorizationHandler>();
            services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();

            services.AddHttpClient<IApiHttpService, ApiHttpService>((provider, client) =>
            {
                var config = configuration.GetSection("ApiConfig").Get<ApiConfig>();
                client.BaseAddress = new Uri(config.BaseUrlApiLocacao);
            })
            .AddHttpMessageHandler<JwtAuthorizationHandler>();

            services.AddScoped<IUsuarioAsp, UsuarioAsp>();
            services.AddScoped<ILoginService, LoginService>();
            services.AddScoped<IFuncionarioService, FuncionarioService>();


            return services;
        }
    }
}
