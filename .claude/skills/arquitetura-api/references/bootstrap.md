# Montar a arquitetura num projeto novo

Roteiro para levar esta espinha para outro projeto. Substitua `Meu.Projeto` pelo nome real em tudo que segue.

## 1. Solução e projetos

```powershell
dotnet new sln -n Meu.Projeto
dotnet new classlib -n Meu.Projeto.Domain
dotnet new classlib -n Meu.Projeto.Infra
dotnet new classlib -n Meu.Projeto.Application
dotnet new webapi   -n Meu.Projeto.Api

dotnet sln add Meu.Projeto.Domain Meu.Projeto.Infra Meu.Projeto.Application Meu.Projeto.Api

dotnet add Meu.Projeto.Infra       reference Meu.Projeto.Domain
dotnet add Meu.Projeto.Application reference Meu.Projeto.Domain Meu.Projeto.Infra
dotnet add Meu.Projeto.Api         reference Meu.Projeto.Application Meu.Projeto.Infra
```

Nos quatro `.csproj`: `<Nullable>enable</Nullable>` e `<ImplicitUsings>enable</ImplicitUsings>` — os arquivos de `assets/` contam com isso.

Sobre a referência `Application → Infra`: ela existe porque o parâmetro `incluir` das consultas usa `Include`/`ThenInclude` do EF Core. É a única concessão; `DbContext`, `SaveChanges` e SQL continuam fora da Application. Se quiser evitá-la, referencie o pacote `Microsoft.EntityFrameworkCore` direto na Application em vez do projeto de Infra.

## 2. Pacotes

**Domain** — nenhum. É a regra que mantém o domínio limpo. (Se o projeto usa ASP.NET Identity e a entidade de usuário herda `IdentityUser`, aí sim entra `Microsoft.AspNetCore.Identity.EntityFrameworkCore`.)

**Infra**
```
Microsoft.EntityFrameworkCore
Npgsql.EntityFrameworkCore.PostgreSQL      # ou o provider do seu banco
EFCore.NamingConventions                   # snake_case
```

O provider é quem traz o `Microsoft.EntityFrameworkCore.Relational`, de onde vem o `AsSplitQuery` usado no `RepositorioGlobal` — só com o pacote base o repositório não compila.

**Application** — precisa dos tipos de `ProblemDetails`/`HttpContext`. A forma limpa numa class library:
```xml
<ItemGroup>
  <FrameworkReference Include="Microsoft.AspNetCore.App" />
</ItemGroup>
```

**Api**
```
Microsoft.EntityFrameworkCore.Design       # PrivateAssets=all
Swashbuckle.AspNetCore
```

## 3. Copiar os arquivos base

De `assets/` para o projeto, mantendo a estrutura:

| Origem | Destino |
|---|---|
| `Domain/IRepositorioGlobal.cs` | `Meu.Projeto.Domain/` |
| `Domain/PaginatedResult.cs` | `Meu.Projeto.Domain/` |
| `Domain/DomainException.cs` | `Meu.Projeto.Domain/Entidades/` |
| `Domain/IRepositorio/IUnitOfWork.cs` | `Meu.Projeto.Domain/IRepositorio/` |
| `Domain/Auditoria/*.cs` | `Meu.Projeto.Domain/Auditoria/` |
| `Infra/RepositorioGlobal.cs` | `Meu.Projeto.Infra/Data/` |
| `Infra/UnitOfWork.cs` | `Meu.Projeto.Infra/Data/` |
| `Infra/AuditoriaExtensions.cs` | `Meu.Projeto.Infra/Data/` |
| `Infra/CurrentUser.cs` | `Meu.Projeto.Infra/Data/CurrentUsers/` |
| `Application/NotificadorService.cs` | `Meu.Projeto.Application/Configuration/Ultils/NotificadorServices/` |
| `Application/Notificacao.cs` | `Meu.Projeto.Application/Models/` |
| `Application/ProblemDetailsFactories.cs` | `Meu.Projeto.Application/Extensions/` |
| `Application/NotificationProblemAdapterMapper.cs` | `Meu.Projeto.Application/Models/Mappers/` |
| `Api/MainController.cs` | `Meu.Projeto.Api/Controllers/` |
| `Api/ExceptionMiddleware.cs` | `Meu.Projeto.Api/Middleware/` |

Depois troque `{{RootNamespace}}` por `Meu.Projeto` em todos eles:

```powershell
Get-ChildItem -Recurse -Filter *.cs |
  ForEach-Object {
    (Get-Content $_.FullName -Raw).Replace('{{RootNamespace}}', 'Meu.Projeto') |
      Set-Content $_.FullName -Encoding utf8
  }
```

O token foi escolhido de propósito para **não compilar** se ficar para trás — placeholder que passa despercebido vira namespace errado em produção.

Nomes de pasta como `Configuration/Ultils/` vêm com o typo do projeto original. Em projeto novo, corrija (`Utils`); num projeto existente, use a grafia que já está lá.

## 4. Contexto

```csharp
public class AppDbContext : DbContext            // ou IdentityDbContext<User, IdentityRole, string>
{
    private readonly ICurrentUser _currentUser;

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUser currentUser)
        : base(options) => _currentUser = currentUser;

    public DbSet<Produto> Produtos => Set<Produto>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(builder);           // antes do conversor, para alcançar o Identity
        builder.AplicarConversorDataUtc();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        var usuario = _currentUser.UserId ?? "SYSTEM";
        ChangeTracker.AplicarAuditoria(usuario);
        this.CriarHistoricoTemporal(usuario);
        return await base.SaveChangesAsync(ct);
    }
}
```

Se o `ICurrentUser` depende de `HttpContext`, acrescente um `IDesignTimeDbContextFactory` — sem ele, `dotnet ef` quebra ao resolver o contexto:

```csharp
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(Environment.GetEnvironmentVariable("ConnectionStrings__dbModelo")
                       ?? "Host=localhost;Database=meu_banco;Username=postgres;Password=postgres")
            .UseSnakeCaseNamingConvention()
            .Options;

        return new AppDbContext(options, new UsuarioDesignTime());   // ICurrentUser fake
    }
}
```

## 5. Injeção de dependência

Uma extension por camada, cada uma no seu projeto:

```csharp
// Infra
public static IServiceCollection AddPostgresDbContext(this IServiceCollection services, string connectionString)
{
    services.AddDbContext<AppDbContext>(options =>
    {
        options.UseNpgsql(connectionString,
            npgsql => npgsql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorCodesToAdd: null));
        options.UseSnakeCaseNamingConvention();
    });

    services.AddHttpContextAccessor();
    services.AddScoped<ICurrentUser, CurrentUser>();
    services.AddScoped<IUnitOfWork, UnitOfWork>();
    return services;
}

public static IServiceCollection AddRepositories(this IServiceCollection services)
{
    services.AddScoped<IProdutoRepository, ProdutoRepository>();
    return services;
}

// Application
public static IServiceCollection AddInjecaoDependenciaApplicationsConfig(this IServiceCollection services)
{
    services.AddScoped<INotificadorService, NotificadorService>();   // scoped, sempre
    services.AddScoped<IProdutoService, ProdutoService>();
    return services;
}
```

`UnitOfWork` recebe `DbContext` no construtor; registre também `services.AddScoped<DbContext>(sp => sp.GetRequiredService<AppDbContext>());` ou troque o tipo do parâmetro para o seu contexto concreto.

`Program.cs`:

```csharp
services.AddInjecaoDependenciaApplicationsConfig();
services.AddPostgresDbContext(config["ConnectionStrings:dbModelo"] ?? "");
services.AddRepositories();
services.AddControllers()
        .AddJsonOptions(o => o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);
services.AddEndpointsApiExplorer();
services.AddSwaggerGen();

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
app.UseRouting();
app.UseHttpsRedirection();
app.UseMiddleware<ExceptionMiddleware>();
app.MapControllers();
app.Run();
```

## 6. Primeira fatia vertical

Faça uma entidade simples inteira antes de escrever a segunda — ela vira o molde que o resto do time copia. Ordem: entidade + `IXxxRepository` → `XxxConfig` + repositório + `DbSet` + DI → DTO + mapper + serviço → controller.

Migration inicial:

```powershell
dotnet ef migrations add Inicial --project Meu.Projeto.Infra --startup-project Meu.Projeto.Api --output-dir Data/Migrations
dotnet ef database update --project Meu.Projeto.Infra --startup-project Meu.Projeto.Api
```

## O que é obrigatório e o que é opcional

**Obrigatório** — sem isso a arquitetura descrita nesta skill não fecha:
- as quatro camadas com a direção de dependência preservada;
- `IRepositorioGlobal` + `RepositorioGlobal`;
- notificador scoped + `MainController`/`CustomResponse` + `ProblemDetails`;
- entidade com `Criar` e estado encapsulado.

**Opcional, por necessidade**:
- `IAuditoria` — só se o projeto precisa saber quem criou/alterou;
- `ITemporalEntity<>` — só se precisa do valor anterior;
- `IUnitOfWork` — só se existe operação com escrita em mais de um agregado;
- Identity, versionamento de Api, health checks, jobs.

## Diferenças em relação ao Locadora_Auto

Os arquivos de `assets/` são a versão de referência, com alguns ajustes sobre o que está no `Locadora_Auto` hoje:

| Ajuste | Motivo |
|---|---|
| `ObterPorIdAsync` desanexa quando `rastreado: false` | no repositório atual o parâmetro está invertido (passar `true` desanexa) e `null` estoura |
| parâmetro genérico renomeado para `TConsulta` | evita o warning de shadowing do `TEntity` da classe; a chamada não muda |
| `DomainException` é `public` | no projeto está `internal`, o que impede usá-la fora do Domain |
| `ExceptionMiddleware` usa `ILogger` e esconde detalhe fora de Development | remove o acoplamento com Elmah e evita vazar mensagem interna |
| `CurrentUser` usa `IHttpContextAccessor` direto | o do projeto depende de um `IUsersAsp` específico dele |
| auditoria/histórico como extensões | o `LocadoraDbContext` herda `IdentityDbContext` e não podia usar classe base |

Em projeto que já existe, **não** substitua o arquivo em uso pelo de `assets/` sem pedido explícito: o do repositório pode ter divergido de propósito, e trocar a base mexe em todas as consultas de uma vez.

Detalhe de edição no `Locadora_Auto`: os `.cs` estão em UTF-8 **com BOM** e CRLF. Preserve ao editar.
