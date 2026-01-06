using Locadora_Auto.Api.Configurations;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using System.Text.Json;

namespace Locadora_Auto.Api.Extensions
{

    //, mas tem que se perguntar , se é preciso ter essa funcionalidade, se sim instale esse pacote
    //dotnet add package AspNetCore.HealthChecks.UI
    //dotnet add package AspNetCore.HealthChecks.UI.InMemory.Storage

    public static class HealthChecksExtension
    {
        public static IServiceCollection AddHealthChecksConfig(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHealthChecks()
                .AddCheck("sqlserver_check", new SqlServerHealthCheck(configuration.GetConnectionString("DefaultConnection")!))
                .AddCheck("external_api_check", new ExternalApiHealthCheck("https://jsonplaceholder.typicode.com/posts"));


            // Adiciona HealthChecks UI, mas tem que se perguntar , se é preciso ter essa funcionalidade
            services.AddHealthChecksUI()
                .AddInMemoryStorage();

            return services;
        }

        public static IApplicationBuilder UseHealthChecksConfig(this IApplicationBuilder app)
        {
            // Endpoint JSON customizado
            app.UseHealthChecks("/health", new HealthCheckOptions
            {
                ResponseWriter = async (context, report) =>
                {
                    context.Response.ContentType = "application/json";

                    var result = new
                    {
                        status = report.Status.ToString(),
                        checks = report.Entries.Select(entry => new
                        {
                            name = entry.Key,
                            status = entry.Value.Status.ToString(),
                            description = entry.Value.Description,
                            duration = entry.Value.Duration.ToString()
                        })
                    };

                    await context.Response.WriteAsync(JsonSerializer.Serialize(result));
                }
            });

            // Dashboard do HealthChecksUI (http://localhost:5000/hc-ui), mas tem que se perguntar , se é preciso ter essa funcionalidade
            app.UseHealthChecksUI(options =>
            {
                options.UIPath = "/hc-ui";
                options.ApiPath = "/hc-api"; // endpoint consumido pelo dashboard
            });

            return app;
        }
    }
}
