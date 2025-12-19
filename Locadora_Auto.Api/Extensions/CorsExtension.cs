namespace Locadora_Auto.Api.Extensions
{
    /// <summary>
    /// Classe de extensão para configuração de CORS (Cross-Origin Resource Sharing) na aplicação.
    /// </summary>
    public static class CorsExtension
    {
        /// <summary>
        /// Adiciona a política de CORS chamada "MeuCors" ao pipeline de serviços.
        /// Permite qualquer origem, método e cabeçalho.
        /// </summary>
        /// <param name="services">Coleção de serviços da aplicação.</param>
        /// <returns>Instância atualizada de IServiceCollection.</returns>
        public static IServiceCollection AddCorsConfig(this IServiceCollection services)
        {
            services.AddCors(o => o.AddPolicy("MeuCors", builder =>
            {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader();
            }));
            return services;
        }

        /// <summary>
        /// Aplica a política de CORS "MeuCors" ao pipeline de requisições da aplicação.
        /// </summary>
        /// <param name="app">Aplicação web configurável.</param>
        /// <returns>Instância atualizada de IApplicationBuilder.</returns>
        public static IApplicationBuilder UseCorsConfig(this IApplicationBuilder app)
        {
            app.UseCors("MeuCors");
            return app;
        }
    }

}
