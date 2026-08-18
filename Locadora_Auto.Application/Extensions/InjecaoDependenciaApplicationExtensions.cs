using Locadora_Auto.Application.Configuration.Ultils.EmailServices;
using Locadora_Auto.Application.Configuration.Ultils.NotificadorServices;
using Locadora_Auto.Application.Configuration.Ultils.UploadArquivoServices;
using Locadora_Auto.Application.Configuration.Ultils.ValidadorArquivoServices;
using Locadora_Auto.Application.Jobs;
using Locadora_Auto.Application.Jobs.JobsBackgroundService;
using Locadora_Auto.Application.Services.AdicionaisServices;
using Locadora_Auto.Application.Services.CategoriaVeiculosServices;
using Locadora_Auto.Application.Services.ClienteServices;
using Locadora_Auto.Application.Services.FilialServices;
using Locadora_Auto.Application.Services.FuncionarioServices;
using Locadora_Auto.Application.Services.ImageService;
using Locadora_Auto.Application.Services.LocacaoServices;
using Locadora_Auto.Application.Services.MultaServices;
using Locadora_Auto.Application.Services.OAuth.Roles;
using Locadora_Auto.Application.Services.OAuth.Token;
using Locadora_Auto.Application.Services.OAuth.Users;
using Locadora_Auto.Application.Services.ReservaServices;
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

            // As três varreduras operacionais. Um BackgroundService por varredura, e não um host
            // com três métodos: cada uma tem cadência própria (a preparação se mede em minutos, a
            // reserva em horas), chave de configuração própria e falha isolada — uma exceção que
            // escapasse do laço de uma não pode levar as outras duas junto.
            //
            // Nenhuma tem porta na Api: são lote de agendador, e as que precisam de acionamento
            // manual já têm a delas (PATCH veiculos/{id}/liberar-preparacao,
            // PATCH reservas/expirar-vencidas).

            // RN-45: solta o carro que o pátio esqueceu em preparação
            services.AddHostedService<LiberacaoPreparacaoBackgroundService>();

            // expira a reserva que ninguém foi buscar — enquanto ela vive, a disponibilidade
            // desconta da frota um carro que está no pátio
            services.AddHostedService<ExpiracaoReservaBackgroundService>();

            // RN-60: marca a locação que passou do fim previsto com o carro ainda na rua
            services.AddHostedService<AtrasoLocacaoBackgroundService>();

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
            services.AddScoped<IIndicadoresFrotaService, IndicadoresFrotaService>();
            services.AddScoped<IFuncionarioService, FuncionarioService>();
            services.AddScoped<IFilialService, FilialService>();
            services.AddScoped<ILocacaoService, LocacaoService>();
            services.AddScoped<IMultaService, MultaService>();
            services.AddScoped<IAdicionalService, AdicionalService>();
            services.AddScoped<IReservaService, ReservaService>();

            //redimensionamento de imagens
            services.AddScoped<IImageService, ImageService>();

            //notificador e validadors
            services.AddScoped<INotificadorService, NotificadorService>();
            services.AddTransient<IValidadorArquivoService, ValidadorArquivoService>();

            //serviço de chaves RSA
            services.AddSingleton<RsaKeyService>();


            return services;
        }
    }
}
