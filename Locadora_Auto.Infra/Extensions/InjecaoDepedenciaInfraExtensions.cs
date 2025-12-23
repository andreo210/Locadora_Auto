using Locadora_Auto.Domain.IRepositorio;
using Locadora_Auto.Infra.Configuration;
using Locadora_Auto.Infra.Data.CurrentUsers;
using Locadora_Auto.Infra.Data.Repositorio;
using Locadora_Auto.Infra.ServiceHttp.Configuration;
using Locadora_Auto.Infra.ServiceHttp.Servicos.CadastroBase.CadadastroBase;
using Locadora_Auto.Infra.ServiceHttp.Servicos.CadastroBase.CadadastroBaseAdmin;
using Locadora_Auto.Infra.ServiceHttp.Servicos.CadastroBase.CadastroBaseRead;
using Locadora_Auto.Infra.ServiceHttp.Servicos.LoginAdmin;
using Locadora_Auto.Infra.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Locadora_Auto.Infra.Extensions
{
    public static class InjecaoDepedenciaInfraExtensions
    {
        //adiciona os repositórios
        public static IServiceCollection AddSqlServerRepositories(this IServiceCollection services)
        {
            //usuario corrente
            services.AddScoped<ICurrentUser, CurrentUser>();

            //injetar repositorios
            services.AddScoped<ILogMensagemRepository, LogMensagemRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            
            return services;
        }
        //adiciona o dbcontext
        public static IServiceCollection AddMySqlDbContext<TContext>(this IServiceCollection services, string ConnectionString) where TContext : DbContext
        {
            services.AddDbContext<TContext>(options =>
            {
                options.UseMySql(
                    ConnectionString,
                    ServerVersion.AutoDetect(ConnectionString),
                    mySqlOptions =>
                    {
                        mySqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(10),
                            errorNumbersToAdd: null);
                    });
            });
            return services;
        }

        //adiciona serviços http
        public static IServiceCollection AddHttpServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IUsersAsp, UsersAsp>();
            services.AddHttpClient<ILoginService, LoginService>();

            //registrando servicos
            services.AddHttpContextAccessor();
            //configuração de tokens
            services.AddTransient<HttpClientAuthorizationAdminInterno>();
            services.AddTransient<HttpClientAuthorizationAdminExterno>();
            services.AddTransient<HttpClientAuthorizationUser>();


            ///httpClients
            services.AddHttpClient<ICadastroBaseService, CadastroBaseService>()
              .AddHttpMessageHandler<HttpClientAuthorizationUser>()
              .AddPolicyHandler(PollyExtensions.TentarTrezVezes());

            services.AddHttpClient<ICadastroBaseReadService, CadastroBaseReadService>()
              .AddHttpMessageHandler<HttpClientAuthorizationAdminInterno>()
              .AddPolicyHandler(PollyExtensions.TentarTrezVezes());

            services.AddHttpClient<ICadastroBaseAdminService, CadastroBaseAdminService>()
              .AddHttpMessageHandler<HttpClientAuthorizationAdminInterno>()
              .AddPolicyHandler(PollyExtensions.TentarTrezVezes());


            //configuraçoes do appsetting
            services.Configure<ApiConfig>(configuration.GetSection("ApiConfig"));
            services.Configure<KeycloakInternoConfig>(configuration.GetSection("KeycloakInternoConfig"));
            services.Configure<KeycloakExternoConfig>(configuration.GetSection("KeycloakExternoConfig"));

            return services;
        }
    }
}
