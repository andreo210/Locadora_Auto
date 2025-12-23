using Locadora_Auto.Application.Configuration.Ultils.UploadArquivo;
using Locadora_Auto.Application.Configuration.Ultils.UploadArquivoDataBase;
using Locadora_Auto.Application.Services;
using Locadora_Auto.Application.Services.Email;
using Locadora_Auto.Application.Services.JobsBackgroundService;
using Locadora_Auto.Application.Services.OAuth.Roles;
using Locadora_Auto.Application.Services.OAuth.Users;
using Locadora_Auto.Infra.ServiceHttp.Servicos.LoginAdmin;
using Microsoft.Extensions.DependencyInjection;

namespace Locadora_Auto.Application.Extensions
{
    public static class InjecaoDependenciaApplicationsExtensions
    {
        public static IServiceCollection AddInjecaoDependenciaApplicationsConfig(this IServiceCollection services)
        {
            //TODO: aqui voçe registra os serviços específicos da sua Applications
            services.AddHostedService<TokenExternoBackgroundService>();


            // Singletons
            services.AddSingleton<IMailService, MailService>();
            services.AddSingleton<IMessageQueue, MessageQueue>();



            // Registrando MessageSenderBackgroundService como ambos BackgroundService e Singleton para acesso via DI
            services.AddSingleton<MessageSenderBackgroundService>();
            services.AddSingleton<IMessageSenderBackgroundService>(provider => provider.GetRequiredService<MessageSenderBackgroundService>());
            services.AddHostedService(provider => provider.GetRequiredService<MessageSenderBackgroundService>());

            //utils
            services.AddScoped<IUploadDownloadFileService, UploadDownloadFileService>();
            services.AddScoped<IPdfStorageService, PdfStorageService>();



            //identidade
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IRoleService, RoleService>();



            services.AddSingleton<ILoginService, LoginService>();

            return services;
        }
    }
}
