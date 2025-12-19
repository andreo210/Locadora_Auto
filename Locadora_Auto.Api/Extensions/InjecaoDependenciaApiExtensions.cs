using Locadora_Auto.Api.Configurations;
using Locadora_Auto.Api.Filters;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Locadora_Auto.Api.Extensions
{
    /// <summary>
    /// Classe de extensão para configurar a injeção de dependência da API.
    /// Permite registrar serviços e configurações específicas no container de DI.
    /// </summary>
    public static class InjecaoDependenciaApiExtensions
    {
        /// <summary>
        /// Adiciona configurações e serviços relacionados à API no container de injeção de dependência.
        /// Inclui configuração do Swagger e (opcionalmente) mapeamento de seções de configuração para objetos fortemente tipados.
        /// </summary>
        /// <param name="services">Coleção de serviços da aplicação.</param>
        /// <param name="Configuration">Instância de IConfiguration contendo as configurações da aplicação.</param>
        /// <returns>Instância atualizada de IServiceCollection.</returns>
        public static IServiceCollection AddInjecaoDependenciaApiConfig(this IServiceCollection services, IConfiguration Configuration)
        {
            //filtros
            services.AddScoped<AuditResultFilter>();


            services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigurarSwaggerOptions>();
            //TODO: aqui voçe registra os serviços específicos da sua API

            return services;
        }
    }

}
