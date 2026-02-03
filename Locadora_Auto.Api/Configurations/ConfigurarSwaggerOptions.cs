using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Locadora_Auto.Api.Configurations
{
    /// <summary>
    /// Classe responsável por configurar as opções do Swagger para cada versão da API.
    /// Utilizada para registrar documentação separada por versão no Swagger UI.
    /// </summary>
    public class ConfigurarSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
    {
        private readonly IApiVersionDescriptionProvider provider;

        /// <summary>
        /// Inicializa a instância com o provedor de descrição de versões da API.
        /// </summary>
        /// <param name="provider">Provedor que fornece as versões disponíveis da API.</param>
        public ConfigurarSwaggerOptions(IApiVersionDescriptionProvider provider) => this.provider = provider;

        /// <summary>
        /// Configura o SwaggerGenOptions adicionando um documento Swagger para cada versão da API.
        /// </summary>
        /// <param name="options">Opções de configuração do Swagger.</param>
        public void Configure(SwaggerGenOptions options)
        {
            foreach (var description in provider.ApiVersionDescriptions)
            {
                options.SwaggerDoc(description.GroupName, CreateInfoForApiVersion(description));
            }
        }

        /// <summary>
        /// Cria as informações de exibição para uma versão específica da API.
        /// Inclui título, versão, descrição e contato.
        /// </summary>
        /// <param name="description">Descrição da versão da API.</param>
        /// <returns>Objeto OpenApiInfo com os metadados da versão.</returns>
        private static OpenApiInfo CreateInfoForApiVersion(ApiVersionDescription description)
        {
            var info = new OpenApiInfo()
            {
                Title = "API - Locadora - Veiculos",
                Version = description.ApiVersion.ToString(),
                Description = "Esta API faz parte de um conjunto de serviços do André Alcântara",
                Contact = new OpenApiContact()
                {
                    Name = "Equipe Alcântara",
                    Email = "andreoa@praiagrande.sp.gov.br."
                }
            };

            if (description.IsDeprecated)
            {
                info.Description += " Esta versão está obsoleta!";
            }

            return info;
        }
    }

}
