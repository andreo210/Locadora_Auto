using Locadora_Auto.Api.Configurations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace Locadora_Auto.Api.Extensions
{
    /// <summary>
    /// Classe de extensão para configuração de autenticação e autorização JWT na aplicação.
    /// Suporta múltiplos esquemas (interno e externo) com políticas customizadas.
    /// </summary>
    public static class AuthenticationExtension
    {
        /// <summary>
        /// Configura os serviços de autenticação e autorização da aplicação.
        /// Define os esquemas "interno" e "externo" com validação via JWT e políticas específicas.
        /// </summary>
        /// <param name="services">Coleção de serviços da aplicação.</param>
        /// <param name="configuration">Configurações da aplicação, incluindo dados de autenticação.</param>
        /// <param name="environment">Ambiente atual da aplicação (Desenvolvimento, Produção, etc.).</param>
        /// <returns>Instância atualizada de IServiceCollection.</returns>
        public static IServiceCollection AddApplicationAuthentication(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
        {
            services.AddTransient<IClaimsTransformation>(x => new ClaimsTransformer(configuration["AuthenticationExterno:ValidAudience"] ?? ""));
            services.AddTransient<IClaimsTransformation>(x => new ClaimsTransformer(configuration["AuthenticationInterno:ValidAudience"] ?? ""));

            var openidConfigurationInterno = ObterJWKSAsync(configuration["AuthenticationInterno:Authority"]!, environment, CancellationToken.None).Result;
            var openidConfigurationExterno = ObterJWKSAsync(configuration["AuthenticationExterno:Authority"]!, environment, CancellationToken.None).Result;
            services.AddAuthentication()

            // Configuração do esquema externo
            .AddJwtBearer("externo", o =>
            {
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = false,
                    ValidateIssuer = true,
                    ValidIssuers = new[] { configuration["AuthenticationExterno:ValidIssuer"] ?? "" },
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKeys = openidConfigurationExterno.SigningKeys,
                    ValidateLifetime = true
                };

                o.Events = new JwtBearerEvents()
                {
                    OnTokenValidated = c =>
                    {
                        var accessToken = c.SecurityToken as JsonWebToken;
                        if (accessToken != null)
                        {
                            ClaimsIdentity identity = c.Principal.Identity as ClaimsIdentity;
                            if (identity != null)
                            {
                                identity.AddClaim(new Claim("access_token", accessToken.EncodedToken));
                            }
                        }
                        Console.WriteLine("User successfully authenticated");
                        return Task.CompletedTask;
                    }
                };
            })

            // Configuração do esquema interno
            .AddJwtBearer("interno", o =>
            {
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = false,
                    ValidateIssuer = true,
                    ValidIssuers = new[] { configuration["AuthenticationInterno:ValidIssuer"] ?? "" },
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKeys = openidConfigurationInterno.SigningKeys,
                    ValidateLifetime = true
                };

                o.Events = new JwtBearerEvents()
                {
                    OnTokenValidated = c =>
                    {
                        var accessToken = c.SecurityToken as JsonWebToken;
                        if (accessToken != null)
                        {
                            ClaimsIdentity identity = c.Principal.Identity as ClaimsIdentity;
                            if (identity != null)
                            {
                                identity.AddClaim(new Claim("access_token", accessToken.EncodedToken));
                            }
                        }
                        Console.WriteLine("User successfully authenticated");
                        return Task.CompletedTask;
                    }
                };
            });

            // Definição das políticas de autorização
            services.AddAuthorization(options =>
            {
                var defaultAuthorizationPolicyBuilder = new AuthorizationPolicyBuilder("interno", "externo")
                    .RequireAuthenticatedUser();
                options.DefaultPolicy = defaultAuthorizationPolicyBuilder.Build();

                var onlySecondJwtSchemePolicyBuilder = new AuthorizationPolicyBuilder("interno");
                options.AddPolicy("SomenteInterno", onlySecondJwtSchemePolicyBuilder
                    .RequireAuthenticatedUser()
                    .Build());

                var onlyCookieSchemePolicyBuilder = new AuthorizationPolicyBuilder("externo");
                options.AddPolicy("SomenteExterno", onlyCookieSchemePolicyBuilder
                    .RequireAuthenticatedUser()
                    .Build());
            });

            return services;
        }

        /// <summary>
        /// Obtém a configuração OpenID Connect (JWKS) de uma autoridade externa.
        /// Utilizado para validar tokens JWT com base nas chaves públicas da autoridade.
        /// </summary>
        /// <param name="keycloakUrl">URL da autoridade OpenID (ex: Keycloak).</param>
        /// <param name="environment">Ambiente atual da aplicação.</param>
        /// <param name="ct">Token de cancelamento opcional.</param>
        /// <returns>Configuração OpenID Connect contendo as chaves de assinatura.</returns>
        public static async Task<OpenIdConnectConfiguration> ObterJWKSAsync(string keycloakUrl, IWebHostEnvironment environment, CancellationToken ct = default)
        {
            string metadataAddress = $"{keycloakUrl}/.well-known/openid-configuration";
            var documentRetriever = new HttpDocumentRetriever { RequireHttps = !environment.IsDevelopment() };
            var configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(metadataAddress, new OpenIdConnectConfigurationRetriever(), documentRetriever);
            OpenIdConnectConfiguration openIdConfig = await configurationManager.GetConfigurationAsync(ct);
            return openIdConfig;
        }

        /// <summary>
        /// Aplica os middlewares de autenticação e autorização ao pipeline da aplicação.
        /// </summary>
        /// <param name="app">Aplicação web configurável.</param>
        /// <returns>Instância atualizada de IApplicationBuilder.</returns>
        public static IApplicationBuilder UseAuthenticationConfig(this IApplicationBuilder app)
        {
            app.UseAuthentication();
            app.UseAuthorization();
            return app;
        }       
    }   
    
}
