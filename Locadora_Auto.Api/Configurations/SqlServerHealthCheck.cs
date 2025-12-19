namespace Locadora_Auto.Api.Configurations
{
    using Microsoft.Extensions.Diagnostics.HealthChecks;
    using System.Data.SqlClient;


    public class SqlServerHealthCheck : IHealthCheck
    {
        private readonly string _connectionString;

        public SqlServerHealthCheck(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);

                return HealthCheckResult.Healthy("Conexão com SQL Server OK");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Falha ao conectar ao SQL Server", ex);
            }
        }
    }
    

}
