using Hangfire;
using Hangfire.SqlServer;
using Locadora_Auto.Application.Services.JobsHangfire;

namespace Locadora_Auto.Api.Extensions
{
    
    /// <summary>
    /// Classe de extensão para configuração do Hangfire na aplicação.
    /// Permite agendamento e execução de tarefas em background com persistência em SQL Server.
    /// </summary>
    public static class HangFireExtension
    {
        /// <summary>
        /// Adiciona a configuração do Hangfire ao pipeline de serviços, incluindo o servidor e o armazenamento em SQL Server.
        /// </summary>
        /// <param name="services">Coleção de serviços da aplicação.</param>
        /// <param name="Configuration">Configurações da aplicação, incluindo connection strings.</param>
        /// <returns>Instância atualizada de IServiceCollection.</returns>
        public static IServiceCollection AddHangFireConfig(this IServiceCollection services, IConfiguration Configuration)
        {
            // Configura o Hangfire com serialização recomendada e armazenamento em SQL Server
            services.AddHangfire(configuration => configuration
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage(Configuration.GetConnectionString("HangfireConnection"),
                new SqlServerStorageOptions
                {
                    CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                    SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                    QueuePollInterval = TimeSpan.Zero,
                    UseRecommendedIsolationLevel = true,
                    DisableGlobalLocks = true,
                    SchemaName = "NomeDoServiço",
                }
            ));

            // Adiciona o servidor do Hangfire para processar jobs em background
            services.AddHangfireServer();

            return services;
        }

        /// <summary>
        /// Aplica o painel de controle do Hangfire ao pipeline da aplicação e executa configuração adicional de jobs.
        /// </summary>
        /// <param name="app">Aplicação web configurável.</param>
        /// <returns>Instância atualizada de IApplicationBuilder.</returns>
        public static IApplicationBuilder UseHangFireConfig(this IApplicationBuilder app)
        {
            // Exibe o dashboard do Hangfire para monitoramento de jobs
            app.UseHangfireDashboard();

            // Executa configuração personalizada de jobs, para obter token interno do client admin-cli
            app.ApplicationServices.GetService<IJobsLoginHandler>()?.SetAdminTokenInterno();

            return app;
        }
    }

    
}
