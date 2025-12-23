using Locadora_Auto.Domain.Entidades.Indentity;
using Locadora_Auto.Infra.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Locadora_Auto.Infra.Extensions
{
    /// <summary>
    /// Classe de extensão responsável por configurar
    /// o ASP.NET Core Identity no projeto.
    ///
    /// Centraliza:
    /// - Configurações de usuário
    /// - Política de senha
    /// - Política de bloqueio (lockout)
    /// - Integração com Entity Framework
    /// - Token providers (reset senha, confirmação de email etc.)
    /// </summary>
    public static class IdentityServiceCollectionExtensions
    {
        /// <summary>
        /// Registra e configura o Identity no container de DI.
        /// Deve ser chamado no Program.cs.
        /// </summary>
        public static IServiceCollection AddIdentityConfiguration(
            this IServiceCollection services)
        {
            services
                // Registra o Identity no ASP.NET Core
                // User  -> sua entidade de usuário customizada
                // Role  -> entidade padrão de roles
                .AddIdentity<User, IdentityRole>(options =>
                {
                    // =========================
                    // CONFIGURAÇÕES DO USUÁRIO
                    // =========================

                    // Exige que o e-mail do usuário seja único
                    // (gera índice UNIQUE no banco)
                    options.User.RequireUniqueEmail = true;

                    // =========================
                    // POLÍTICA DE SENHA
                    // =========================

                    // Exige pelo menos um dígito (0–9)
                    options.Password.RequireDigit = true;

                    // Tamanho mínimo da senha
                    options.Password.RequiredLength = 8;

                    // NÃO exige caractere especial (!@#$%)
                    options.Password.RequireNonAlphanumeric = false;

                    // Exige letra maiúscula (A–Z)
                    options.Password.RequireUppercase = true;

                    // Exige letra minúscula (a–z)
                    options.Password.RequireLowercase = true;

                    // =========================
                    // BLOQUEIO DE CONTA (LOCKOUT)
                    // =========================

                    // Número máximo de tentativas inválidas de login
                    options.Lockout.MaxFailedAccessAttempts = 5;

                    // Tempo de bloqueio após exceder tentativas
                    options.Lockout.DefaultLockoutTimeSpan =
                        TimeSpan.FromMinutes(15);

                    // Define se usuários novos podem ser bloqueados
                    options.Lockout.AllowedForNewUsers = true;
                })

                // =========================
                // INTEGRAÇÃO COM EF CORE
                // =========================

                // Informa que o Identity irá persistir dados
                // usando o LocadoraDbContext (MySQL/MariaDB)
                .AddEntityFrameworkStores<LocadoraDbContext>()

                // =========================
                // TOKEN PROVIDERS
                // =========================

                // Habilita geração de tokens para:
                // - Reset de senha
                // - Confirmação de e-mail
                // - Alteração de e-mail
                // - Two-Factor Authentication (2FA)
                .AddDefaultTokenProviders();

            // Permite encadeamento de métodos no Program.cs
            return services;
        }
    }
}
