using Locadora_Auto.Infra.Data.CurrentUsers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Locadora_Auto.Infra.Data
{
    /// <summary>
    /// Usada só pelas ferramentas do EF (migrations add/script/database update). Em tempo de design não
    /// existe HttpContext, então o CurrentUser real quebra — aqui entra um usuário fixo no lugar dele.
    /// A conexão vem do appsettings do projeto de startup, a mesma que a Api usa.
    /// </summary>
    public class LocadoraDbContextFactory : IDesignTimeDbContextFactory<LocadoraDbContext>
    {
        public LocadoraDbContext CreateDbContext(string[] args)
        {
            var options = new DbContextOptionsBuilder<LocadoraDbContext>()
                .UseNpgsql(ObterConexao())
                .UseSnakeCaseNamingConvention()
                .Options;

            return new LocadoraDbContext(options, new UsuarioDesignTime());
        }

        private static string ObterConexao()
        {
            //o dotnet ef roda com o diretório do projeto de startup (Locadora_Auto.Api) como corrente
            var ambiente = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

            var configuracao = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile($"appsettings.{ambiente}.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var conexao = configuracao.GetConnectionString("dbModelo");

            if (string.IsNullOrWhiteSpace(conexao))
                throw new InvalidOperationException(
                    "Connection string 'dbModelo' não encontrada. Rode o comando com --startup-project " +
                    "Locadora_Auto.Api ou defina a variável de ambiente ConnectionStrings__dbModelo.");

            return conexao;
        }

        private sealed class UsuarioDesignTime : ICurrentUser
        {
            public string? UserId => "SYSTEM";
            public bool IsAuthenticated => false;
        }
    }
}
