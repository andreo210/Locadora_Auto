namespace Locadora_Auto.Api.Configurations
{
    using Microsoft.Extensions.Diagnostics.HealthChecks;

    public class ExternalApiHealthCheck : IHealthCheck
    {
        private readonly string _url;
        private readonly HttpClient _httpClient;

        public ExternalApiHealthCheck(string url)
        {
            _url = url;
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(5)
            };
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.GetAsync(_url, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    return HealthCheckResult.Healthy($"API externa {_url} OK");
                }

                return HealthCheckResult.Degraded(
                    $"API externa {_url} respondeu com código {response.StatusCode}");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy($"Falha ao acessar API externa {_url}", ex);
            }
        }
        
    }

}
