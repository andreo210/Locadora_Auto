using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Locadora_Auto.Api.Filters
{
    /// <summary>
    /// Filtro de operação para o Swagger que aplica valores padrão aos parâmetros da API.
    /// Utilizado para enriquecer a documentação gerada com descrições, valores default e obrigatoriedade.
    /// </summary>
    public class SwaggerDefaultValues : IOperationFilter
    {
        /// <summary>
        /// Aplica modificações à operação Swagger, preenchendo descrições e valores padrão dos parâmetros.
        /// </summary>
        /// <param name="operation">Operação da API que está sendo documentada.</param>
        /// <param name="context">Contexto da operação, contendo metadados da API.</param>
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation.Parameters == null)
            {
                return;
            }

            foreach (var parameter in operation.Parameters)
            {
                // Obtém a descrição do parâmetro a partir do contexto da API
                var description = context.ApiDescription
                    .ParameterDescriptions
                    .First(p => p.Name == parameter.Name);

                var routeInfo = description.RouteInfo;

                // Define se a operação está obsoleta
                operation.Deprecated = OpenApiOperation.DeprecatedDefault;

                // Preenche a descrição do parâmetro, se estiver ausente
                if (parameter.Description == null)
                {
                    parameter.Description = description.ModelMetadata?.Description;
                }

                // Se não houver informações de rota, pula para o próximo parâmetro
                if (routeInfo == null)
                {
                    continue;
                }

                // Define valor padrão para parâmetros que não estão na rota e não têm valor default
                if (parameter.In != ParameterLocation.Path && parameter.Schema.Default == null)
                {
                    parameter.Schema.Default = new OpenApiString(routeInfo.DefaultValue?.ToString());
                }

                // Define se o parâmetro é obrigatório
                parameter.Required |= !routeInfo.IsOptional;
            }
        }
    }

}
