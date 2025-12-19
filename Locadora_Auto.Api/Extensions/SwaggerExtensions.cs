using Locadora_Auto.Api.Filters;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Models;
using System.Reflection;

namespace Locadora_Auto.Api.Extensions
{
    /// <summary>
    /// Classe de extensão para configuração do Swagger na aplicação.
    /// Permite gerar documentação interativa da API com suporte a autenticação JWT e versionamento.
    /// </summary>
    public static class SwaggerExtensions
    {
        /// <summary>
        /// Adiciona e configura o Swagger no pipeline de serviços.
        /// Inclui definição de segurança JWT, filtros personalizados e comentários XML.
        /// </summary>
        /// <param name="services">Coleção de serviços da aplicação.</param>
        /// <returns>Instância atualizada de IServiceCollection.</returns>
        public static IServiceCollection AddSwaggerConfig(this IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                // Aplica valores padrão às operações da API
                c.OperationFilter<SwaggerDefaultValues>();

                // Define esquema de segurança JWT para autenticação via Bearer Token
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = @"Insira seu Access Token.",
                });

                // Exige o uso do esquema de segurança em todas as operações
                c.AddSecurityRequirement(new OpenApiSecurityRequirement{
                {
                    new OpenApiSecurityScheme{
                        Reference = new OpenApiReference{
                            Id = JwtBearerDefaults.AuthenticationScheme,
                            Type = ReferenceType.SecurityScheme
                        }
                    }, new List<string>()
                }});

                // Schema explícito para ProblemDetails
                c.MapType<ProblemDetails>(() => new OpenApiSchema
                {
                    Type = "object",
                    Properties =
                    {
                        ["type"] = new OpenApiSchema { Type = "string" },
                        ["title"] = new OpenApiSchema { Type = "string" },
                        ["status"] = new OpenApiSchema { Type = "integer", Format = "int32" },
                        ["detail"] = new OpenApiSchema { Type = "string" },
                        ["instance"] = new OpenApiSchema { Type = "string" }
                    }
                });

                c.MapType<ValidationProblemDetails>(() => new OpenApiSchema
                {
                    Type = "object",
                    Properties =
                    {
                        ["type"] = new OpenApiSchema { Type = "string" },
                        ["title"] = new OpenApiSchema { Type = "string" },
                        ["status"] = new OpenApiSchema { Type = "integer", Format = "int32" },
                        ["errors"] = new OpenApiSchema
                        {
                            Type = "object",
                            AdditionalProperties = new OpenApiSchema
                            {
                                Type = "array",
                                Items = new OpenApiSchema { Type = "string" }
                            }
                        }
                    }
                });

                // Inclui comentários XML gerados pelo compilador para enriquecer a documentação
                var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));

                //filtros dos problemas padronizados
                c.OperationFilter<ProblemDetailsOperationFilter>();
            });

            return services;
        }

        /// <summary>
        /// Aplica o middleware do Swagger ao pipeline da aplicação.
        /// Configura a interface interativa do Swagger UI com suporte a múltiplas versões da API.
        /// </summary>
        /// <param name="app">Aplicação web configurável.</param>
        /// <param name="provider">Provedor de descrição de versões da API.</param>
        /// <returns>Instância atualizada de IApplicationBuilder.</returns>
        public static IApplicationBuilder UseSwaggerConfig(this IApplicationBuilder app, IApiVersionDescriptionProvider provider)
        {
            // Middleware para gerar o JSON da documentação
            app.UseSwagger();

            // Interface gráfica do Swagger UI com suporte a múltiplas versões
            app.UseSwaggerUI(options =>
            {
                foreach (var description in provider.ApiVersionDescriptions)
                {
                    options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", description.GroupName.ToUpperInvariant());
                }
            });

            return app;
        }
    }


}