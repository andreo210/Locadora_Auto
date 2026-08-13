---
name: arquitetura-api
description: Arquitetura de referência para a Api .NET — camadas Domain → Infra → Application → Api com DDD, repositório genérico `RepositorioGlobal`, notificador no lugar de exceção e respostas `ProblemDetails` (RFC 7807). Use sempre que for escrever, revisar ou mover código de back-end nessa arquitetura: criar/alterar entidade, serviço, repositório, DTO ou controller; decidir em que camada uma regra mora; tratar erro de regra de negócio; montar consulta paginada/filtrada; ou iniciar um projeto novo com a mesma espinha (os arquivos base prontos estão em `assets/`). Vale também para perguntas do tipo "onde eu coloco isso", "por que não lanço exceção aqui", "como funciona o CustomResponse/notificador" e "essa regra é do domínio ou do serviço".
---

# Arquitetura da Api

Esta skill descreve a arquitetura, não um repositório. Ela vale para o `Locadora_Auto` e para qualquer projeto novo que reuse a mesma espinha — os arquivos genéricos estão em `assets/` prontos para copiar (veja `references/bootstrap.md`).

Escopo: **back-end (Api)**. Front-end não é assunto daqui.

Código, nomes e comentários em **português**; inglês só para termos de framework/HTTP (`Get`, `Post`, `Dto`, `ProblemDetails`).

## Mapa das camadas

```
        Api            controllers, filtros, middleware, extensions de startup
         │             fala HTTP; não conhece EF
         ▼
     Application       serviços (regra de negócio), DTOs, mappers, notificador
         │             orquestra; não conhece HttpContext
         ▼
       Infra           DbContext, IEntityTypeConfiguration, repositórios, UnitOfWork
         │             fala EF/Postgres; não conhece HTTP
         ▼
       Domain          entidades, invariantes, interfaces de repositório, auditoria
                       sem dependência nenhuma — nem EF, nem ASP.NET
```

A seta é a direção da dependência. O `Domain` é o único projeto sem referências: se você precisou de um `using Microsoft.EntityFrameworkCore` ou `Microsoft.AspNetCore` dentro dele, a regra está na camada errada.

A Application chega ao banco só pelas interfaces `IXxxRepository` declaradas no Domain — a implementação entra por injeção de dependência, e é por isso que a interface mora no Domain e a classe concreta na Infra. Ela referencia o EF Core (o parâmetro `incluir` das consultas usa `Include`/`ThenInclude`), e o limite é esse: expressão de consulta sim; `DbContext`, `SaveChanges` e SQL não.

## O caminho de uma requisição

```
HTTP → Controller (MainController)
         valida ModelState → ValidationResponse
         chama o serviço
       → Service (Application)
         carrega via IXxxRepository, aplica a regra
         regra violada → _notificador.Add("mensagem")
         chama método de domínio da entidade
       → Repositório (Infra, herda RepositorioGlobal<T>)
       → DbContext.SaveChangesAsync → auditoria + histórico temporal
       ← Service devolve DTO (ou bool/null)
       ← CustomResponse(resultado, status)
         tem notificação? → ProblemDetails 4xx com os erros agrupados
         não tem?        → 200/201/204 com o payload
```

Exceção não tratada em qualquer ponto cai no `ExceptionMiddleware`, que também devolve `ProblemDetails` (500).

## As cinco regras que definem a arquitetura

### 1. Falha de regra de negócio é notificação, não exceção

O serviço sinaliza o problema com `_notificador.Add("mensagem em português")` e retorna `null`/`false`. O controller chama `CustomResponse(...)`, que consulta o `INotificadorService` (registrado como **scoped**, um por requisição) e, se houver notificação, devolve `ProblemDetails` em vez do payload de sucesso.

O motivo é econômico: "cliente já cadastrado" ou "veículo indisponível" são resultados esperados do fluxo, não defeitos. Usar exceção para isso paga custo de stack trace, embaralha erro previsto com bug de verdade e obriga cada controller a ter `try/catch`. Como as notificações se acumulam na mesma instância scoped, o serviço pode reportar **todos** os problemas de uma vez e o usuário corrige tudo numa tentativa só — algo que a primeira exceção lançada impede.

```csharp
// serviço
private async Task<bool> Validar(CriarAdicionalDto dto, CancellationToken ct)
{
    if (await _repository.ExisteAsync(a => a.Nome == dto.Nome, ct))
        _notificador.Add("Adicional já cadastrado");

    if (dto.ValorDiaria < 0)
        _notificador.Add("Valor da diária inválido");

    return !_notificador.TemNotificacao();   // acumula tudo antes de decidir
}

// controller
var resultado = await _service.CriarAsync(dto, ct);
return CustomResponse(resultado, HttpStatusCode.Created);
```

Exceção continua sendo a ferramenta certa para **invariante de domínio quebrada** (`DomainException`, `InvalidOperationException`) — ou seja, quando o estado pedido é impossível e chegar ali já é sintoma de bug ou de validação que faltou no serviço. Regra prática: se a mensagem faz sentido para o usuário final, é notificação; se faz sentido só para quem escreveu o código, é exceção.

### 2. A entidade protege as próprias invariantes

Toda entidade tem `set` privado, construtor sem parâmetros protegido/privado (o EF precisa dele), uma fábrica `static Criar(...)` que valida e um método por transição de estado (`Atualizar`, `Ativar`, `Cancelar`, `Finalizar`). Não existe entidade anêmica com `{ get; set; }` público.

Isso mantém a regra perto do dado: quem quiser inventar um `Status` inválido não consegue, porque não há caminho até a propriedade. Serviços novos passam a herdar a garantia de graça, em vez de repetir o `if`.

```csharp
public class Adicional
{
    public int IdAdicional { get; private set; }
    public string Nome { get; private set; } = null!;
    public bool Ativo { get; private set; }

    protected Adicional() { }                       // EF

    public static Adicional Criar(string nome, decimal valorDiaria)
    {
        if (valorDiaria < 0) throw new DomainException("Valor inválido");
        return new Adicional { Nome = nome, ValorDiaria = valorDiaria, Ativo = true };
    }

    public void Desativar() => Ativo = false;
}
```

`internal static Criar` (em vez de `public`) marca **entidade de agregado**: ela só nasce pela raiz. `Reserva.Criar` é internal porque a porta de entrada é `Clientes.ReservarVeiculo(...)`; `Multa`, `Vistoria` e `Caucao` idem, dentro de `Locacao`. Ter repositório próprio **não** faz de uma entidade uma raiz — o repositório serve para consulta, que não atravessa invariante. Detalhes e as exceções deliberadas em `references/dominio.md`.

### 3. Persistência passa pelo `RepositorioGlobal`

`RepositorioGlobal<TEntity>` já entrega consulta com `filtro` / `ordenarPor` / `incluir`, paginação, projeção, existência, contagem e as escritas — tudo com `CancellationToken` e `AsNoTracking` por padrão. O repositório concreto costuma ser três linhas:

```csharp
public class AdicionalRepository : RepositorioGlobal<Adicional>, IAdicionalRepository
{
    public AdicionalRepository(LocadoraDbContext ctx) : base(ctx) { }
}
```

Só escreva LINQ novo na classe concreta quando a consulta for realmente específica e não couber em `filtro`/`incluir`. O ganho é que qualquer melhoria (split query, projeção, paginação) chega a todas as entidades de uma vez.

Duas armadilhas de tracking, que causam o "salvei e não mudou nada":
- leitura é `AsNoTracking` por padrão; para **alterar** a entidade carregue com `rastreado: true`;
- `AtualizarSalvarAsync` cobre os dois casos (rastreada ou desanexada), mas sobrescreve todas as colunas quando a entidade chega desanexada;
- o token de concorrência (`xmin`) só acusa conflito na entidade rastreada — mais um motivo para `rastreado: true` antes de alterar.

Assinaturas completas em `references/persistencia.md`.

### 4. Cada camada fala uma língua só

- Controller não faz LINQ, não monta `Expression`, não conhece entidade — recebe DTO, chama serviço, devolve `CustomResponse`.
- Serviço não conhece `HttpContext`, `IFormFile` cru nem `Request`; recebe DTO e devolve DTO/`bool`/`PaginatedResult<TDto>`.
- Entidade não conhece DTO. O mapeamento é **manual**, em métodos de extensão `.ToDto()` / `.ToDtoList()` em `Models/Mappers/` — não há AutoMapper e não é para introduzir um: mapper escrito à mão quebra em tempo de compilação quando a entidade muda, em vez de em produção.
- `PaginatedResult<TEntity>` volta do repositório com entidades; o serviço reprojeta para `PaginatedResult<TDto>` antes de devolver.

### 5. Escrita é sempre assíncrona e em UTC

O `SaveChangesAsync` sobrescrito é quem aplica auditoria (`IAuditoria`) e gera histórico temporal (`ITemporalEntity<>`). O `SaveChanges()` síncrono **não passa por nada disso** — usá-lo grava registro sem autoria e sem histórico, silenciosamente.

Datas: colunas são `timestamp with time zone` e o driver do Postgres só aceita `DateTime` com `Kind=Utc`. Use `DateTime.UtcNow`, nunca `DateTime.Now`, inclusive nas comparações dentro da entidade.

## Onde cada arquivo mora

| Camada | Pasta | O que vai lá |
|---|---|---|
| Domain | `Entidades/` | entidade, enum de status, exceção de domínio |
| Domain | `IRepositorio/` | `IXxxRepository : IRepositorioGlobal<Xxx>` |
| Domain | `Auditoria/` | `IAuditoria`, `ITemporalEntity<>`, `ITemporalHistory` |
| Infra | `Data/Configuracao/` | um `IEntityTypeConfiguration<T>` por entidade |
| Infra | `Data/Repositorio/` | repositório concreto herdando `RepositorioGlobal<T>` |
| Infra | `Data/` | `DbContext`, `UnitOfWork`, `ICurrentUser` |
| Application | `Models/Dto/` | DTO de leitura e de escrita |
| Application | `Models/Mappers/` | extensões `.ToDto()` / `.ToDtoList()` |
| Application | `Services/<Area>Services/` | `IXxxService` + `XxxService` |
| Api | `V1/Controllers/` | controller herdando `MainController` |
| Api | `Extensions/` | um par `AddXxxConfig` / `UseXxxConfig` por preocupação |

Registro de DI: repositório na extension da Infra, serviço na extension da Application, tudo `Scoped` (o notificador **precisa** ser scoped para acumular por requisição).

## Roteiro de uma funcionalidade nova

Trabalhe de baixo para cima — cada camada depende da anterior:

1. **Domain** — entidade com `Criar` + métodos de domínio; interface do repositório.
2. **Infra** — `IEntityTypeConfiguration`, repositório concreto, `DbSet` no contexto, registro na DI, migration.
3. **Application** — DTOs, mapper, `IXxxService` + implementação com notificador.
4. **Api** — controller herdando `MainController`, tudo retornando `CustomResponse`.
5. Compilar e conferir que os erros novos são zero.

Antes de escrever qualquer arquivo, **leia uma fatia vertical pronta** do projeto em que você está e siga a grafia dela (nomes de rota, sufixos de DTO, organização de pastas). A arquitetura é a mesma; as convenções de nomenclatura variam por repositório.

Neste repositório existe a skill `nova-entidade` com o passo a passo específico (caminhos, comando de migration, comando de build). Esta skill aqui é a base conceitual; quando as duas divergirem em detalhe operacional, o repositório manda.

## Anti-padrões (checklist de revisão)

- `throw` para regra de negócio esperada, no lugar de `_notificador.Add`
- entidade anêmica: `{ get; set; }` público sem `Criar` nem métodos de domínio
- `SaveChanges()` síncrono, ou `DateTime.Now`
- LINQ, `Include` ou `Expression` dentro do controller
- entidade vazando no retorno do controller (sempre DTO)
- `try/catch` no controller para transformar erro em resposta — isso já é papel do `CustomResponse` e do `ExceptionMiddleware`
- consulta nova escrita no repositório concreto quando `filtro`/`incluir` resolveriam
- carregar com `AsNoTracking` e depois tentar atualizar
- `INotificadorService` registrado como singleton ou transient (vaza entre requisições ou perde as mensagens)
- `AddScoped` esquecido: o serviço compila e explode em runtime na resolução do controller

## Referências

Leia sob demanda — não carregue tudo de uma vez:

- `references/dominio.md` — entidade, agregado e raiz, invariantes, `IAuditoria`, histórico temporal.
- `references/persistencia.md` — assinaturas do `RepositorioGlobal`, `IEntityTypeConfiguration`, `DbContext`, `UnitOfWork`/transação, migrations, snake_case, UTC.
- `references/aplicacao-api.md` — DTO, mapper, serviço, `MainController`/`CustomResponse`, `ProblemDetails`, middleware, versionamento, DI.
- `references/bootstrap.md` — montar a arquitetura num projeto novo a partir de `assets/`.

## Arquivos base

`assets/` traz a implementação de referência dos blocos genéricos, com o namespace como `{{RootNamespace}}` para troca em massa:

```
assets/Domain/       IRepositorioGlobal, PaginatedResult, DomainException, IUnitOfWork, Auditoria/
assets/Infra/        RepositorioGlobal, UnitOfWork, AuditoriaExtensions, ICurrentUser/CurrentUser
assets/Application/  INotificadorService, NotificadorService, Notificacao,
                     ProblemDetailsFactories, NotificationProblemAdapterMapper,
                     ConsultaPaginadaRequest, OrdenacaoDeConsulta, PaginatedResultMapper
assets/Api/          MainController, ExceptionMiddleware
```

Em projeto novo: copie, substitua `{{RootNamespace}}`, registre na DI. Em projeto existente, **não** troque o arquivo em uso pelo de `assets/` sem pedido explícito — o do repositório pode ter divergido de propósito.
