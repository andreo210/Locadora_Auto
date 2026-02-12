using Locadora_Auto.Application.Configuration.Ultils.Email;
using Locadora_Auto.Application.Configuration.Ultils.NotificadorServices;
using Locadora_Auto.Application.Configuration.Ultils.UploadArquivo;
using Locadora_Auto.Application.Configuration.Ultils.ValidadorArquivoServices;
using Locadora_Auto.Application.Jobs;
using Locadora_Auto.Application.Services;
using Locadora_Auto.Application.Services.CategoriaVeiculosServices;
using Locadora_Auto.Application.Services.ClienteServices;
using Locadora_Auto.Application.Services.FilialServices;
using Locadora_Auto.Application.Services.FuncionarioServices;
using Locadora_Auto.Application.Services.LocacaoServices;
using Locadora_Auto.Application.Services.MultaServices;
using Locadora_Auto.Application.Services.OAuth.Roles;
using Locadora_Auto.Application.Services.OAuth.Token;
using Locadora_Auto.Application.Services.OAuth.Users;
using Locadora_Auto.Application.Services.SeguroServices;
using Locadora_Auto.Application.Services.VeiculoServices;
using Microsoft.Extensions.DependencyInjection;

namespace Locadora_Auto.Application.Extensions
{
    public static class InjecaoDependenciaApplicationsExtensions
    {
        public static IServiceCollection AddInjecaoDependenciaApplicationsConfig(this IServiceCollection services)
        {
            // Singletons
            services.AddSingleton<IMailService, MailService>();
            services.AddSingleton<IMessageQueue, MessageQueue>();



            // Registrando MessageSenderBackgroundService como ambos BackgroundService e Singleton para acesso via DI
            services.AddSingleton<MessageSenderBackgroundService>();
            services.AddSingleton<IMessageSenderBackgroundService>(provider => provider.GetRequiredService<MessageSenderBackgroundService>());
            services.AddHostedService(provider => provider.GetRequiredService<MessageSenderBackgroundService>());

            //utils
            services.AddScoped<IUploadDownloadFileService, UploadDownloadFileService>();
            //services.AddScoped<IPdfStorageService, PdfStorageService>();



            //identidade
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<ITokenService, TokenService>();

            //regras de negócio
            services.AddScoped<IClienteService, ClienteService>();
            services.AddScoped<ISeguroService, SeguroService>();
            services.AddScoped<ICategoriaVeiculoService, CategoriaVeiculoService>();
            services.AddScoped<IVeiculoService, VeiculoService>();
            services.AddScoped<IFuncionarioService, FuncionarioService>();
            services.AddScoped<IFilialService, FilialService>();
            services.AddScoped<ILocacaoService, LocacaoService>();
            services.AddScoped<IMultaService, MultaService>();

            //notificador e validadors
            services.AddScoped<INotificadorService, NotificadorService>();
            services.AddTransient<IValidadorArquivoService, ValidadorArquivoService>();

            //serviço de chaves RSA
            services.AddSingleton<RsaKeyService>();


            return services;
        }
    }
}
