# 01 — Arquitetura

## Projetos e dependências

A solução `Locadora_Auto-Api.sln` tem seis projetos, todos `net8.0`.

```mermaid
flowchart TB
    subgraph Backend["Back-end — API"]
        direction TB
        ApiP["Locadora_Auto.Api<br/>Controllers, Extensions,<br/>Filters, Middleware"]
        AppP["Locadora_Auto.Application<br/>Services, DTOs, Mappers,<br/>Notificador, Jobs"]
        InfraP["Locadora_Auto.Infra<br/>LocadoraDbContext, Configuracao,<br/>Repositorio, Identity"]
        DomainP["Locadora_Auto.Domain<br/>Entidades, IRepositorio,<br/>Auditoria — sem dependências"]
    end

    subgraph Frontend["Front-end — Blazor Server"]
        direction TB
        FrontP["Locadora_Auto.Front<br/>Components, Pages, Tabela"]
        FrontSvcP["Locadora_Auto.Front.Services<br/>ApiHttpService, Servicos,<br/>CustomAuthStateProvider"]
        FrontModP["Locadora_Auto.Front.Models<br/>DTOs e modelos de tabela"]
    end

    ApiP --> AppP
    ApiP --> InfraP
    AppP --> InfraP
    AppP --> DomainP
    InfraP --> DomainP

    FrontP --> FrontSvcP
    FrontP --> FrontModP
    FrontSvcP --> FrontModP
    FrontSvcP -.->|HTTP REST| ApiP
```

`Locadora_Auto.Domain` não referencia nenhum outro projeto — é o núcleo. A dependência de
`Application` para `Infra` existe porque os serviços de aplicação recebem os repositórios
concretos e o `LocadoraDbContext` por injeção.

## Camadas e responsabilidades

```mermaid
flowchart TB
    subgraph L1["Apresentação — Locadora_Auto.Api"]
        C["Controller<br/>herda MainController"]
        MW["ExceptionMiddleware"]
        F["Filters<br/>AuditResultFilter<br/>ProblemDetailsOperationFilter"]
    end

    subgraph L2["Aplicação — Locadora_Auto.Application"]
        S["Service<br/>ClienteService, LocacaoService, ..."]
        DTO["DTOs<br/>Models/Dto"]
        MAP["Mappers<br/>Models/Mappers"]
        NOT["INotificadorService<br/>scoped"]
    end

    subgraph L3["Domínio — Locadora_Auto.Domain"]
        E["Entidades<br/>fábricas estáticas Criar<br/>e métodos de comportamento"]
        IR["IRepositorio + IRepositorioGlobal"]
        AUD["IAuditoria / ITemporalEntity"]
    end

    subgraph L4["Infraestrutura — Locadora_Auto.Infra"]
        R["RepositorioGlobal + repositórios concretos"]
        CTX["LocadoraDbContext"]
        CFG["IEntityTypeConfiguration<br/>Data/Configuracao"]
        UOW["UnitOfWork"]
    end

    DB[("PostgreSQL")]

    C --> S
    C -.->|lê notificações| NOT
    S --> DTO
    S --> MAP
    S --> E
    S --> IR
    S -.->|Add mensagem| NOT
    S --> UOW
    IR -.->|implementado por| R
    R --> CTX
    CFG --> CTX
    CTX --> DB
    E -.->|marcadas por| AUD
    AUD -.->|lidas em SaveChangesAsync| CTX
    MW -.->|envolve| C
    F -.->|envolve| C
```

## Fluxo de uma requisição bem-sucedida

```mermaid
sequenceDiagram
    autonumber
    actor Cli as Cliente HTTP
    participant Ctl as XxxController
    participant Svc as XxxService
    participant Rep as XxxRepository
    participant Ctx as LocadoraDbContext
    participant Db as PostgreSQL

    Cli->>Ctl: POST /api/v1/recurso  { dto }
    Ctl->>Svc: CriarAsync(dto, ct)
    Svc->>Svc: Entidade.Criar(...) — valida invariantes
    Svc->>Rep: InserirSalvarAsync(entidade, ct)
    Rep->>Ctx: Add + SaveChangesAsync(ct)
    Ctx->>Ctx: AplicarAuditoria() + CriarHistoricoTemporal()
    Ctx->>Db: INSERT
    Db-->>Ctx: id gerado
    Ctx-->>Rep: linhas afetadas
    Rep-->>Svc: entidade persistida
    Svc->>Svc: Mapper.ToDto(entidade)
    Svc-->>Ctl: XxxDto
    Ctl->>Ctl: CustomResponse(dto, Created)
    Ctl-->>Cli: 201 Created + payload
```

## Tratamento de erros

Regras de negócio **não lançam exceção**: o serviço registra a mensagem no
`INotificadorService` (scoped, portanto vive durante toda a requisição) e retorna `null`/`false`.
O `MainController.CustomResponse` consulta o notificador antes de montar a resposta e, havendo
notificação, devolve um `ProblemDetails` (RFC 7807) no lugar do payload de sucesso.

```mermaid
flowchart TB
    Req["Requisição chega ao Controller"] --> Svc["Service executa a operação"]

    Svc --> Dec{"Regra de negócio<br/>violada?"}

    Dec -->|Não| Ok["Retorna resultado"]
    Dec -->|Sim| Add["_notificador.Add('mensagem')<br/>retorna null / false"]

    Ok --> Resp["CustomResponse(resultado, status)"]
    Add --> Resp

    Resp --> Check{"_notificador.<br/>TemNotificacao()?"}
    Check -->|Não| Sucesso["200 OK / 201 Created / 204 No Content"]
    Check -->|Sim| Problem["NotificationProblemAdapterMapper<br/>.ToProblemDetails()"]
    Problem --> Erro["StatusCode + ProblemDetails<br/>RFC 7807"]

    Exc["Exceção não tratada<br/>DomainException, InvalidOperationException, ..."] --> MW["ExceptionMiddleware"]
    MW --> Erro
```

Fora esse caminho, o `MainController` ainda expõe atalhos diretos: `NotFound(mensagem)`,
`Forbidden(mensagem)`, `ProblemResponse(status, detail, ...)` e `ValidationResponse(modelState)`
para erros de validação de modelo.

## Acesso a dados

Todo repositório concreto herda `RepositorioGlobal<TEntity>`, que já implementa a superfície
genérica de consulta e escrita. Consultas são `AsNoTracking` por padrão — para alterar uma
entidade é preciso pedir `rastreado: true`.

```mermaid
classDiagram
    direction LR

    class IRepositorioGlobal~TEntity~ {
        <<interface>>
        +ObterAsync(filtro, ordenarPor, incluir, rastreado, ct) IReadOnlyList~TEntity~
        +ObterPrimeiroAsync(filtro, incluir, rastreado, ct) TEntity
        +ObterPorIdAsync(id, rastreado, ct) TEntity
        +ObterPaginadoComFiltroAsync(filtro, ordenarPor, incluir, pagina, itensPorPagina, ...) PaginatedResult~TEntity~
        +ObterComFiltroAsync(filtro, ordenarPor, incluir, ...) IReadOnlyList~TEntity~
        +ObterComFiltroEProjecaoAsync(projecao, filtro, ...) IReadOnlyList~TResult~
        +ExisteAsync(filtro, ct) bool
        +ContarAsync(filtro, ct) int
        +InserirSalvarAsync(entidade, ct) TEntity
        +InserirSalvarListasAsync(entidades, ct) List~TEntity~
        +AtualizarSalvarAsync(entidade, ct) bool
        +ExcluirSalvarAsync(entidade, ct) void
        +SalvarAsync(ct) int
    }

    class RepositorioGlobal~TEntity~ {
        #LocadoraDbContext _context
    }

    class IUnitOfWork {
        <<interface>>
        +HasActiveTransaction bool
        +BeginTransactionAsync(ct)
        +CommitAsync(ct)
        +RollbackAsync(ct)
        +ExecuteTransactionAsync~T~(action, ct) T
    }

    class UnitOfWork {
        -LocadoraDbContext _context
        -IDbContextTransaction _transaction
    }

    class LocadoraDbContext {
        +DbSet~Clientes~ Clientes
        +DbSet~Funcionario~ Funcionarios
        +DbSet~Veiculo~ Veiculos
        +DbSet~Locacao~ Locacoes
        +DbSet~Pagamento~ Pagamentos
        +DbSet~Manutencao~ Manutencoes
        +DbSet~Reserva~ Reservas
        +DbSet~Vistoria~ Vistorias
        +DbSet~Multa~ Multas
        +DbSet~Caucao~ Caucoes
        +DbSet~Endereco~ Enderecos
        +DbSet~Seguro~ Seguros
        +DbSet~LocacaoSeguro~ LocacaoSeguros
        +DbSet~Dano~ Danos
        +SaveChangesAsync(ct) int
        -AplicarAuditoria()
        -CriarHistoricoTemporal()
    }

    IRepositorioGlobal~TEntity~ <|.. RepositorioGlobal~TEntity~
    RepositorioGlobal~TEntity~ <|-- ClienteRepository
    RepositorioGlobal~TEntity~ <|-- VeiculosRepository
    RepositorioGlobal~TEntity~ <|-- LocacaoRepository
    RepositorioGlobal~TEntity~ <|-- FilialRepository
    RepositorioGlobal~TEntity~ <|-- OutrosRepositorios
    RepositorioGlobal~TEntity~ --> LocadoraDbContext
    IUnitOfWork <|.. UnitOfWork
    UnitOfWork --> LocadoraDbContext
```

`LocadoraDbContext` herda `IdentityDbContext<User, IdentityRole, string>` e sobrescreve
**apenas `SaveChangesAsync`**. O `SaveChanges()` síncrono não aplica auditoria nem histórico
temporal — por isso todo o código usa a versão assíncrona.

### Auditoria e histórico temporal

```mermaid
flowchart LR
    Save["SaveChangesAsync(ct)"] --> Aud["AplicarAuditoria()"]
    Aud --> Hist["CriarHistoricoTemporal()"]
    Hist --> Base["base.SaveChangesAsync(ct)"]

    Aud -.->|"entidade implementa<br/>IAuditoria"| A1["Added → DataCriacao, IdUsuarioCriacao<br/>Modified → DataModificacao, IdUsuarioModificacao"]
    Hist -.->|"entidade implementa<br/>ITemporalEntity de THistory"| H1["Modified/Deleted → cria THistory<br/>com DataEvento, Acao (UPDATE/DELETE)<br/>e UsuarioEvento vindo do ICurrentUser"]
```

Hoje apenas duas entidades participam desse mecanismo:

| Entidade | Interfaces | Tabela de histórico |
|---|---|---|
| `Clientes` | `IAuditoria`, `ITemporalEntity<ClienteHistorico>` | `tb_cliente_historico` |
| `User` | `ITemporalEntity<UserHistorico>` | `tb_user_historico` |

## Banco de dados

- PostgreSQL via **Npgsql**; o modelo do EF Core é a fonte de verdade e o schema sai das
  migrations em `Infra/Data/Migrations/`.
- Nomenclatura **snake_case**, resultado de `UseSnakeCaseNamingConvention()` somada aos
  `ToTable`/`HasColumnName` explícitos em `Data/Configuracao/`.
- Colunas de data são `timestamp with time zone`. Um conversor global em `OnModelCreating`
  normaliza todo `DateTime`/`DateTime?` para `Kind=Utc` na escrita e remarca na leitura — daí
  a regra de sempre usar `DateTime.UtcNow`.
- `LocadoraDbContextFactory` (`IDesignTimeDbContextFactory`) existe para as migrations, porque
  o `CurrentUser` real depende de `HttpContext`.
- `db.sql` na raiz é o schema **MySQL antigo**, apenas referência histórica.
  `db_postgres.sql` é gerado por `dotnet ef migrations script --idempotent`.

## Autenticação

Duas abordagens convivem no repositório:

```mermaid
flowchart TB
    subgraph ApiAuth["API — tokens próprios"]
        Rsa["RsaKeyService<br/>gera/lê a chave em Jwt:PrivateKeyPath"]
        Tok["TokenService<br/>assina JWT RS256"]
        Jwks["GET /.well-known/jwks.json<br/>JwksController"]
        Refresh["refresh_tokens<br/>TokenRepository"]
        Rsa --> Tok
        Tok --> Jwks
        Tok --> Refresh
    end

    subgraph FrontAuth["Front — cookie"]
        Login["LoginService<br/>POST api/v1/Users/autenticar"]
        Cookie["Cookie de autenticação<br/>tokens guardados em<br/>AuthenticationProperties"]
        Provider["CustomAuthStateProvider"]
        Login --> Cookie --> Provider
    end

    subgraph Kc["Keycloak"]
        KcCfg["KeycloakInternoConfig<br/>KeycloakExternoConfig<br/>configurados no appsettings"]
    end

    FrontAuth -->|Bearer| ApiAuth
    Kc -.->|não ativado| ApiAuth

    style Kc stroke-dasharray: 5 5
```

## Estado atual do `Program.cs`

Vários blocos estão comentados para facilitar o desenvolvimento e serão reativados:

```mermaid
flowchart LR
    subgraph Ativo["Ativo"]
        A1["AddInjecaoDependenciaApplicationsConfig"]
        A2["AddHttpServices"]
        A3["AddPostgresDbContext"]
        A4["AddSqlServerRepositories<br/>(nome errado — é PostgreSQL)"]
        A5["AddIdentityConfiguration"]
        A6["AddApiConfig"]
        A7["AddVersionamentoConfig"]
        A8["AddSwaggerConfig"]
        A9["UseAuthenticationConfig"]
        A10["ExceptionMiddleware"]
        A11["MapControllers"]
    end

    subgraph Comentado["Comentado temporariamente"]
        C1["AddApplicationAuthentication"]
        C2["AddElmahConfig / UseElmahConfig"]
        C3["AddHealthChecksConfig / UseHealthChecksConfig"]
        C4["AddHangFireConfig / UseHangFireConfig<br/>(não existem no repositório)"]
        C5["UseCorsConfig"]
        C6["[Authorize] nos controllers"]
    end

    style Comentado stroke-dasharray: 5 5
```

## Processamento em segundo plano

`TarefaDiariaBackgroundService` é um `BackgroundService` que acorda às 03:00 todos os dias,
cria um escopo de DI, resolve o `LocadoraDbContext` e chama `SaveChangesAsync`. O corpo da
rotina ainda está vazio (a linha de limpeza de logs está comentada).

O domínio já expõe dois métodos pensados para execução agendada, ainda sem *job* que os chame:

- `Locacao.MarcarComoAtrasada(DateTime agora)` — marca como `Atrasada` a locação `Criada` cuja
  data fim prevista já passou.
- `Reserva.Expirar(DateTime agora)` — marca como `Expirado` a reserva `Reservado` cuja data de
  início já passou.

## Front-end

O `Locadora_Auto.Front` é Blazor Server e conversa com a API por `HttpClient` com políticas
Polly. As telas de listagem usam o componente genérico `Components/Tabela/TabelaGenerica.razor`
(filtro, ordenação, paginação, seleção e ações em massa), configurado por `ColunaTabela<T>` e
`AcaoTabela<T>` de `Front/Models/Tabelas/`.

```mermaid
flowchart TB
    subgraph Pages["Components/Pages"]
        P1["Auth/Login"]
        P2["Clientes/ Listar · Criar · Editar · Visualizar"]
        P3["funcionarios/ Listar · Criar · Editar · Visualizar"]
        P4["Filial/ Listar · Criar · Editar · Visualizar · UploadFotos"]
        P5["Categorias/ Listar · Criar · Editar · Visualizar · UploadFotos"]
    end

    subgraph Comp["Componentes reutilizáveis"]
        T["TabelaGenerica de T"]
        FE["Forms/EnderecoForm"]
        FI["Forms/InputCPF · InputCEP · InputTelefone · InputMoeda"]
        N["Notificacao/NotificationDisplay · ConfirmDialog"]
    end

    subgraph Svc["Locadora_Auto.Front.Services"]
        AH["ApiHttpService<br/>+ ApiValidation + Polly"]
        S1["ClienteService"]
        S2["FuncionarioService"]
        S3["FilialService"]
        S4["CategoriaService"]
        S5["LoginService + CustomAuthStateProvider"]
    end

    Pages --> Comp
    Pages --> Svc
    S1 & S2 & S3 & S4 & S5 --> AH
    AH -->|REST| API["Locadora_Auto.Api"]
```

O componente `TabelaGenerica.razor` está fisicamente em `Components/Tabela/` mas declara
`@namespace Locadora_Auto.Front.Components.UI` — o `@using` correto é `...Components.UI`.

## Observações

- `AddSqlServerRepositories()` e `SqlServerHealthCheck` têm nomes herdados de outro banco; a
  implementação é PostgreSQL.
- Versionamento usa o pacote legado `Microsoft.AspNetCore.Mvc.Versioning` (v5). As rotas são
  inconsistentes: `ClientesController`, `FuncionariosController` e `UsersController` usam
  `api/v{version:apiVersion}/[controller]`; os demais fixam `api/v1/<nome>` — e
  `LocacoesController` usa `api/locacoes`, sem versão nenhuma.
- No PostgreSQL o `LIKE` é sensível a maiúsculas e a acentos (diferente do
  `utf8mb4_unicode_ci` do MySQL antigo). As buscas por texto aplicam `.ToLower().Contains(...)`
  dos dois lados; acento continua diferenciando.
- `Locadora_Auto.Front` aponta para `ApiConfig:BaseUrlApiLocacao = https://localhost:44310/`,
  que não corresponde à porta real da API (`61977`).
- Segredos ficam em `appsettings.Development.json`, que está commitado — não há user-secrets.
