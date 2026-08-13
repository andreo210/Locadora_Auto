---
name: nova-entidade
description: Cria uma funcionalidade nova (entidade + CRUD) atravessando todas as camadas do Locadora_Auto — Domain, Infra, Application e Api — seguindo as convenções já existentes no repositório. Use ao adicionar uma entidade nova ou um conjunto de endpoints CRUD para uma entidade existente.
---

# Nova entidade / CRUD

Objetivo: adicionar uma funcionalidade atravessando as camadas sem inventar padrões novos. **Antes de escrever qualquer arquivo, leia um vertical slice já pronto e copie a estrutura dele.** Use `Cliente` ou `Funcionario` como referência — são os mais recentes.

A base conceitual (por que cada camada existe, notificador em vez de exceção, entidade com `Criar`, uso do `RepositorioGlobal`) está na skill **`arquitetura-api`** — consulte quando a dúvida for de desenho, não de caminho de arquivo. Aqui ficam os passos operacionais deste repositório.

Se o usuário não disse qual entidade, pergunte. Se não disse quais operações (listar / obter / criar / editar / excluir), assuma CRUD completo e confirme no final.

## Ordem de trabalho

Siga de baixo para cima; cada camada depende da anterior.

### 1. Domain — `Locadora_Auto.Domain`

- Entidade em `Entidades/<Nome>.cs`. Se precisar de auditoria, implemente `IAuditoria`; se precisar de histórico, `ITemporalEntity<THistory>` mais a classe de histórico correspondente.
- Interface do repositório em `IRepositorio/I<Nome>Repository.cs`, herdando `IRepositorioGlobal<TEntity>`. Só declare métodos além dos herdados se houver consulta realmente específica.

### 2. Infra — `Locadora_Auto.Infra`

- `Data/Configuracao/<Nome>Config.cs` implementando `IEntityTypeConfiguration<T>`. É aplicado automaticamente por `ApplyConfigurationsFromAssembly` — não é preciso registrar nada.
- `Data/Repositorio/<Nome>Repository.cs` herdando `RepositorioGlobal<T>` e implementando a interface do Domain.
- Adicione o `DbSet<T>` em `Data/LocadoraDbContext.cs`.
- Registre o repositório na extensão de injeção de dependência da Infra (`AddSqlServerRepositories()` — nome enganoso, o banco é PostgreSQL).
- **Gere a migration na mesma mudança** — o modelo é a fonte de verdade e o schema sai das migrations:
  ```powershell
  dotnet ef migrations add <Nome> --project Locadora_Auto.Infra --startup-project Locadora_Auto.Api --output-dir Data/Migrations
  ```
  Tabelas e colunas em snake_case. O `db.sql` na raiz é o schema MySQL antigo, mantido só como referência histórica — não atualize.

### 3. Application — `Locadora_Auto.Application`

- DTOs em `Models/Dto/` — normalmente um DTO de leitura e um de escrita, seguindo o sufixo usado pelos vizinhos.
- Mapper em `Models/Mappers/` como métodos de extensão `.ToDto()` / `.ToEntity()`. **Não existe AutoMapper neste projeto** — o mapeamento é escrito à mão.
- Serviço em `Services/<Area>Services/`: interface `I<Nome>Service` + implementação.
  - Regras de negócio que falham chamam `_notificador.Add("mensagem em português")`. **Não lance exceção para erro de regra de negócio.**
  - Use os métodos do `RepositorioGlobal` (`ObterAsync`, `ObterPrimeiroAsync`, `ObterPaginadoComFiltroAsync`, `InserirSalvarAsync`, `AtualizarSalvarAsync`, `ExcluirSalvarAsync`) com os parâmetros opcionais `filtro`, `ordenarPor`, `incluir` e propague o `CancellationToken`.
  - Persistência sempre pela via assíncrona — o `SaveChanges()` síncrono não aplica auditoria.
- Registre o serviço em `AddInjecaoDependenciaApplicationsConfig()`.

### 4. Api — `Locadora_Auto.Api`

- Controller em `V1/Controllers/<Nome>Controller.cs` herdando `MainController`, injetando `INotificadorService` no construtor base.
- Todo retorno passa por `CustomResponse(resultado, HttpStatusCode.X)` — ele converte notificações em `ProblemDetails` (RFC 7807) automaticamente.
- Rota: **copie o padrão do controller vizinho**, não normalize. O repositório é inconsistente de propósito histórico — alguns usam `api/v{version:apiVersion}/[controller]`, outros fixam `api/v1/<nome-plural-kebab>`.
- Os `[Authorize]` estão comentados em todo o projeto porque a autenticação está temporariamente desligada. Escreva o controller assumindo que ela volta: se os vizinhos têm `[Authorize]` comentado, faça igual.
- Documente com comentários XML em português, como os controllers existentes (`GenerateDocumentationFile` está ligado).

## Ao terminar

1. Compile:
   ```powershell
   dotnet build Locadora_Auto-Api.sln -c Debug --nologo
   ```
   Espere 0 erros. Os ~333 warnings são pré-existentes — só se preocupe com warnings nos arquivos que você criou.
2. Liste ao usuário os arquivos criados por camada e confirme se falta o front-end (Blazor) para a funcionalidade — este skill cobre só a Api.
3. Não comite automaticamente. Se o usuário pedir commit, use `/commit-seguro`.
