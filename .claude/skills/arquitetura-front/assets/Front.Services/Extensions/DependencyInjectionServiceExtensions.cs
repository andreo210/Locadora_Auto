using {{RootNamespace}}.Front.Services.Configuration;
using {{RootNamespace}}.Front.Services.Servicos;
using {{RootNamespace}}.Front.Services.Utils.Notificacao;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace {{RootNamespace}}.Front.Services.Extensions
{
    public static class DependencyInjectionServiceExtensions
    {
        /// <summary>
        /// Registro único do front. Tudo Scoped: em Blazor Server o escopo é o
        /// circuito, então serviço com estado (notificação, diálogo) vive enquanto
        /// a aba estiver aberta — que é exatamente o desejado.
        /// </summary>
        public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IConfirmDialogService, ConfirmDialogService>();

            services.Configure<ApiConfig>(configuration.GetSection("ApiConfig"));
            services.AddHttpContextAccessor();
            services.AddScoped<JwtAuthorizationHandler>();

            var apiConfig = configuration.GetSection("ApiConfig").Get<ApiConfig>();

            // Falhar aqui, na subida, é melhor do que descobrir pela primeira tela
            // que devolve erro de conexão sem dizer por quê.
            if (string.IsNullOrWhiteSpace(apiConfig?.BaseUrlApiLocacao))
                throw new InvalidOperationException("ApiConfig:BaseUrlApiLocacao não configurado.");

            // Cliente tipado: o HttpClientFactory cuida do pool de conexões.
            // `new HttpClient()` avulso prende socket em TIME_WAIT e ignora mudança de DNS.
            services.AddHttpClient<IApiHttpService, ApiHttpService>(client =>
            {
                client.BaseAddress = new Uri(apiConfig.BaseUrlApiLocacao);
            })
            .AddHttpMessageHandler<JwtAuthorizationHandler>();

            // Um registro por área — a partir daqui é o CRUD do projeto:
            // services.AddScoped<IXxxService, XxxService>();

            return services;
        }
    }
}
