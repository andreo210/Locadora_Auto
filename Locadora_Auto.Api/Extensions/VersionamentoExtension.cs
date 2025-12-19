using Microsoft.AspNetCore.Mvc;

namespace Locadora_Auto.Api.Extensions
{
    public static class VersionamentoExtension
    {
        public static IServiceCollection AddVersionamentoConfig(this IServiceCollection services)
        {
            services.AddApiVersioning(options =>
            {
                options.AssumeDefaultVersionWhenUnspecified = true; //assume a versão defaul
                options.DefaultApiVersion = new ApiVersion(1, 0);//define a versão
                options.ReportApiVersions = true; //reporta se a api ta ok ou absoleta
            });

            services.AddVersionedApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });

            return services;
        }
    }
}
