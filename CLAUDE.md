# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

Sistema de locadora de automóveis. Código, identificadores e comentários em **português**. Mantenha esse padrão ao escrever código novo — use inglês apenas para conceitos de framework/HTTP (`Get`, `Post`, `Dto`, `ProblemDetails`).

## Build

```powershell
dotnet build Locadora_Auto-Api.sln -c Debug --nologo
```

O build atual passa com 0 erros e ~333 warnings — os warnings são pré-existentes, não trate como regressão.

Todos os projetos são `net8.0` e o SDK/runtime 8.0 estão instalados — nada de `--roll-forward`. Na prática a execução/debug acontece no Visual Studio. Portas fixas em `launchSettings.json`: Api `https://localhost:61977`, Front `https://localhost:62259`.

**Não há projetos de teste no repositório.** Não invente comandos de teste.

## Git

Não há `.gitattributes`; a proteção contra ruído de fim de linha é o `core.autocrlf=true` do Windows. **Nunca use `git add -A`, `git add .` ou `git commit -a`** — se o repositório for editado pelo WSL, o `git status` enche de arquivos que só trocaram CRLF→LF e isso vira uma reescrita global no commit. Sempre adicione arquivos explicitamente: `git add <caminho/do/arquivo>` (a skill `/commit-seguro` faz isso).

Trabalho acontece em `andre-dev` e vai para `main` por PR. Mensagens de commit são curtas, minúsculas, em português, sem prefixos.

## Arquitetura

Camadas: **Controller (Api) → Service (Application) → Repository (Infra) → `LocadoraDbContext`**, com DTOs e mappers entre elas.

- `Locadora_Auto.Domain` — entidades (`Entidades/`), interfaces de repositório (`IRepositorio/`), contratos de auditoria (`Auditoria/`). Sem dependências.
- `Locadora_Auto.Infra` — `Data/LocadoraDbContext.cs`, um `IEntityTypeConfiguration` por entidade em `Data/Configuracao/`, repositórios em `Data/Repositorio/`.
- `Locadora_Auto.Application` — serviços em `Services/<Area>Services/`, DTOs em `Models/Dto/`, mappers em `Models/Mappers/`.
- `Locadora_Auto.Api` — controllers em `V1/Controllers/`, um par `AddXxxConfig`/`UseXxxConfig` por preocupação em `Extensions/`.
- `Locadora_Auto.Front` — Blazor Server; consome a Api via `Locadora_Auto.Front.Services`.

Telas de listagem usam o componente genérico `Front/Components/Tabela/TabelaGenerica.razor` (filtro, ordenação, paginação, seleção e ações em massa) configurado por `ColunaTabela<T>` / `AcaoTabela<T>` em `Front/Models/Tabelas/`. Reaproveite em listagens novas em vez de escrever `<table>` na mão — veja `Pages/Clientes/ListarCliente.razor`. **O arquivo está em `Components/Tabela/` mas declara `@namespace Locadora_Auto.Front.Components.UI`** — o `@using` é `...Components.UI`.

### Erros: notificações, não exceções

Serviços sinalizam falha de regra de negócio chamando `_notificador.Add("mensagem")` — **não lançam exceção**. Controllers herdam de `MainController` e retornam `CustomResponse(resultado, HttpStatusCode.X)`; `CustomResponse` consulta o `INotificadorService` (scoped) e, se houver notificação, devolve um `ProblemDetails` (RFC 7807) em vez do payload de sucesso. Siga esse fluxo em código novo.

### Acesso a dados

Repositórios herdam `RepositorioGlobal<TEntity>`, que já oferece `ObterAsync`, `ObterPrimeiroAsync`, `ObterPaginadoComFiltroAsync`, `InserirSalvarAsync`, `AtualizarSalvarAsync`, `ExcluirSalvarAsync` — todos com `filtro` / `ordenarPor` / `incluir` opcionais, `AsNoTracking` por padrão (`rastreado: true` para rastrear) e `CancellationToken ct`. Prefira esses métodos a escrever LINQ novo no repositório concreto.

`LocadoraDbContext` sobrescreve **apenas `SaveChangesAsync`** para aplicar auditoria (`IAuditoria`) e histórico temporal (`ITemporalEntity<>`). O `SaveChanges()` síncrono ignora tudo isso — **sempre use a versão assíncrona**.

### Banco de dados

PostgreSQL via Npgsql. **O modelo é a fonte de verdade** — o schema sai de migrations do EF Core em `Infra/Data/Migrations/`. Ao alterar entidade ou `*Config.cs`, gere a migration na mesma mudança:

```powershell
dotnet ef migrations add <Nome> --project Locadora_Auto.Infra --startup-project Locadora_Auto.Api --output-dir Data/Migrations
```

`LocadoraDbContextFactory` (`IDesignTimeDbContextFactory`) existe porque o `CurrentUser` real precisa de `HttpContext` e quebra em tempo de design. Ela lê `ConnectionStrings__dbModelo` do ambiente e cai num default local.

Tabelas e colunas são **snake_case**. Isso vem de `UseSnakeCaseNamingConvention()` **somada** aos `ToTable`/`HasColumnName` explícitos — a convenção só nomeia o que não foi configurado à mão, então nomes novos precisam já vir em snake_case.

Datas: as colunas são `timestamp with time zone` e o Npgsql só aceita `DateTime` com `Kind=Utc`. Um conversor global em `OnModelCreating` normaliza tudo na escrita — **mas use `DateTime.UtcNow`, nunca `DateTime.Now`**, ou a hora gravada fica certa e as comparações em memória erram por 3h.

O `db.sql` na raiz é o schema **MySQL antigo**, mantido só como referência histórica; `db_postgres.sql` é gerado por `dotnet ef migrations script --idempotent`.

## Estado atual do `Program.cs`

Autenticação (`AddApplicationAuthentication`), Elmah, health checks, Hangfire e CORS estão **comentados temporariamente para desenvolvimento**, assim como os `[Authorize]` dos controllers. Isso será reativado — não remova esses blocos comentados como "limpeza", e escreva controllers novos assumindo que a autenticação voltará.

`AddHangFireConfig`/`UseHangFireConfig` são chamados nos comentários mas **não existem no repositório**; descomentar não compila.

## Armadilhas

- `AddSqlServerRepositories()` e `SqlServerHealthCheck` são nomes errados — o banco é PostgreSQL.
- No Postgres o `LIKE` é sensível a maiúsculas e acentos, ao contrário do `utf8mb4_unicode_ci` do MySQL. Buscas por texto usam `.ToLower().Contains(...)` dos dois lados; acento ainda diferencia (resolver exigiria a extensão `unaccent`).
- Typos consolidados em caminhos e namespaces: `Entidades/Indentity/`, `Configuration/Ultils/`, `Front/Midlleware/`, `InjecaoDepedencia` (Infra). Use a grafia existente; não renomeie sem pedido.
- Versionamento usa o pacote legado `Microsoft.AspNetCore.Mvc.Versioning` (v5), não `Asp.Versioning.*`. As rotas são inconsistentes: alguns controllers usam `api/v{version:apiVersion}/[controller]`, outros fixam `api/v1/<nome>`. Siga o padrão do controller vizinho.
- A Api emite os próprios tokens RS256 (`RsaKeyService` gera a chave em `Jwt:PrivateKeyPath` se não existir; JWKS em `GET /.well-known/jwks.json`). Existe também configuração de Keycloak no appsettings — as duas abordagens convivem; pergunte antes de mexer em autenticação.
- O Front usa autenticação por **cookie** e guarda os tokens da Api em `AuthenticationProperties` — modelo diferente do da Api.
- `Front` aponta para `ApiConfig:BaseUrlApiLocacao = https://localhost:44310/`, que não corresponde à porta real da Api.
- Arquivos são UTF-8 **com BOM** e CRLF. Preserve ao editar.

## Configuração

Segredos ficam em `appsettings.Development.json`, que **está commitado** (não há user-secrets). A chave `ConnectionStrings:dbModelo` é a única string de conexão realmente usada. Não adicione segredos novos ao repositório sem avisar.
