using Locadora_Auto.Api.Configurations;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text.Json.Serialization;

namespace Locadora_Auto.Api.Extensions
{
    /// <summary>
    /// Classe de extensão para configuração da API, incluindo Swagger, JSON e roteamento.
    /// </summary>
    public static class ApiExtension
    {
        /// <summary>
        /// Configura os serviços da API, incluindo Swagger, controladores e opções de serialização JSON.
        /// </summary>
        /// <param name="services">Coleção de serviços da aplicação.</param>
        /// <returns>Instância atualizada de IServiceCollection.</returns>
        public static IServiceCollection AddApiConfig(this IServiceCollection services)
        {
            services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigurarSwaggerOptions>();

            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    // Ignora ciclos de referência entre objetos (ex: relações circulares entre entidades)
                    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                    options.JsonSerializerOptions.WriteIndented = true;
                });

            services.AddEndpointsApiExplorer();
            return services;
        }

        /// <summary>
        /// Configura o pipeline da aplicação, incluindo roteamento e redirecionamento HTTPS.
        /// </summary>
        /// <param name="app">Aplicação web configurável.</param>
        /// <returns>Instância atualizada de IApplicationBuilder.</returns>
        public static IApplicationBuilder UseApiConfig(this IApplicationBuilder app)
        {
            app.UseRouting();
            app.UseHttpsRedirection();
            return app;
        }
    }
}
