using Locadora_Auto.Domain.IRepositorio;
using Locadora_Auto.Infra.Configuration;
using Locadora_Auto.Infra.Data;
using Locadora_Auto.Infra.Data.CurrentUsers;
using Locadora_Auto.Infra.Data.Repositorio;
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
            //injetar repositorios
            //services.AddScoped<ILogMensagemRepository, LogMensagemRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<ITokenRepository, TokenRepository>();
            services.AddScoped<IClienteRepository, ClienteRepository>();
            services.AddScoped<ICategoriaVeiculosRepository, CategoriaVeiculosRepository>();
            services.AddScoped<IFuncionarioRepository, FuncionarioRepository>();
            services.AddScoped<IVeiculosRepository, VeiculosRepository>();
            services.AddScoped<IAdicionalRepository, AdicionalRepository>();
            services.AddScoped<IVistoriaRepository, VistoriaRepository>();
            services.AddScoped<IFilialRepository, FilialRepository>();
            services.AddScoped<ILocacaoRepository, LocacaoRepository>();
            services.AddScoped<ILocacaoSeguroRepository, LocacaoSeguroRepository>();
            services.AddScoped<ISeguroRepository, SeguroRepository>();
            services.AddScoped<IMultaRepository, MultaRepository>();
            services.AddScoped<IReservaRepository, ReservaRepository>();

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
                    }
                );
            });

            //usuario corrente
            services.AddScoped<ICurrentUser, CurrentUser>();

            //transaction
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            return services;
        }

        //adiciona serviços http
        public static IServiceCollection AddHttpServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IUsersAsp, UsersAsp>();

            //registrando servicos
            services.AddHttpContextAccessor();


            //configuraçoes do appsetting
            services.Configure<ApiConfig>(configuration.GetSection("ApiConfig"));
            services.Configure<KeycloakInternoConfig>(configuration.GetSection("KeycloakInternoConfig"));
            services.Configure<KeycloakExternoConfig>(configuration.GetSection("KeycloakExternoConfig"));

            return services;
        }
    }
}
