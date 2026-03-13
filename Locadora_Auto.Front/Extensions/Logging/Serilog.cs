using Serilog;
using Serilog.Events;

namespace Locadora_Auto.Front.Extensions.Logging
{
    public static class SerilogConfiguration
    {
        // Extensions/Logging/SerilogConfiguration.cs
        public static void ConfigureSerilog(this ConfigureHostBuilder host, IConfiguration configuration, IHostEnvironment environment)
        {
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.WithMachineName()
                .Enrich.WithEnvironmentName()
                .Enrich.WithThreadId()
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", "LocadoraAuto")
                .Enrich.WithProperty("Version", "1.0.0")
                .WriteTo.Console(
                    restrictedToMinimumLevel: environment.IsDevelopment()
                        ? LogEventLevel.Debug     // Console só mostra Debug+ em Dev
                        : LogEventLevel.Information,
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File(
                    path: "logs/locadora-.txt",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            host.UseSerilog();
        }

        public static void ConfigureSerilogCompleto(this ConfigureHostBuilder host, IConfiguration configuration, IHostEnvironment environment)
        {
            var logLevel = environment.IsDevelopment()
                ? LogEventLevel.Debug
                : LogEventLevel.Information;

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Is(logLevel)
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                .Enrich.WithMachineName()
                .Enrich.WithEnvironmentName()
                .Enrich.WithThreadId()
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", "LocadoraAuto")
                .Enrich.WithProperty("Version", configuration["AppVersion"] ?? "1.0.0")
                .WriteTo.Console(
                    restrictedToMinimumLevel: LogEventLevel.Information,
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File(
                    path: "logs/locadora-.txt",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.Conditional(
                    evt => environment.IsDevelopment(),
                    wt => wt.File("logs/debug-.txt", rollingInterval: RollingInterval.Day)
                )
                .WriteTo.Conditional(
                    evt => evt.Level == LogEventLevel.Error || evt.Level == LogEventLevel.Fatal,
                    wt => wt.File("logs/error-.txt", rollingInterval: RollingInterval.Day)
                )
                .CreateLogger();

            host.UseSerilog();

            Log.Information("Aplicação LocadoraAuto iniciada em {Environment} com nível de log {LogLevel}",
                environment.EnvironmentName, logLevel);
        }
    }
}