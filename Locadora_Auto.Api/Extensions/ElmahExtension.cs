using ElmahCore.Mvc;
using ElmahCore.Sql;

namespace Locadora_Auto.Api.Extensions
{
    /// <summary>
    /// Classe de extensão para configuração do ELMAH (Error Logging Modules and Handlers) na aplicação.
    /// Permite registrar erros em banco de dados SQL Server usando ElmahCore.
    /// </summary>
    public static class ElmahExtension
    {
        /// <summary>
        /// Adiciona a configuração do ELMAH ao pipeline de serviços, definindo a conexão e a tabela de log.
        /// </summary>
        /// <param name="services">Coleção de serviços da aplicação.</param>
        /// <param name="configuration">Configurações da aplicação, incluindo connection strings.</param>
        /// <returns>Instância atualizada de IServiceCollection.</returns>
        public static IServiceCollection AddElmahConfig(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddElmah<SqlErrorLog>(options =>
            {
                options.ConnectionString = configuration["ConnectionStrings:ElmahCore"] ?? "";
                options.SqlServerDatabaseTableName = "tbLogAPI";
            });
            return services;
        }

        /// <summary>
        /// Aplica o middleware do ELMAH ao pipeline de requisições da aplicação.
        /// Permite o registro automático de exceções durante a execução.
        /// </summary>
        /// <param name="app">Aplicação web configurável.</param>
        /// <returns>Instância atualizada de IApplicationBuilder.</returns>
        public static IApplicationBuilder UseElmahConfig(this IApplicationBuilder app)
        {
            app.UseElmah();
            return app;
        }
    }

}
