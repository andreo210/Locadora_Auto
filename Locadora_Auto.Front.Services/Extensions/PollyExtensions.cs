using Polly;
using Polly.Extensions.Http;
using Polly.Retry;

namespace Locadora_Auto.Front.Services.Extensions
{
    public class PollyExtensions
    {
        public static AsyncRetryPolicy<HttpResponseMessage> TentarTrezVezes()
        {
            var retry = HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(new[]
                {
                    TimeSpan.FromSeconds(2),
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(10),
                });
            return retry;
        }
    }
}
