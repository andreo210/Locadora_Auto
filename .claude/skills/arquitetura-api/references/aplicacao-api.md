# Application e Api

Índice: [DTOs](#dtos) · [Mappers](#mappers) · [Serviços](#serviços) · [Notificador](#notificador) · [Controllers](#controllers) · [ProblemDetails](#problemdetails-rfc-7807) · [Middleware](#middleware-de-exceção) · [Startup e DI](#startup-e-injeção-de-dependência)

## DTOs

Ficam em `Models/Dto/`, agrupados por entidade num arquivo só (`AdicionalDtos.cs` contém `AdicionalDto`, `CriarAtualizarAdicionalDto`, ...). Classes simples com `{ get; set; }` público — DTO é transporte, não tem invariante para proteger.

Separação típica:
- **leitura** (`XxxDto`) — o que a tela mostra, incluindo campos desnormalizados vindos de `Include` (`NomeCliente`, `NomeFilial`);
- **escrita** (`CriarXxxDto` / `CriarAtualizarXxxDto`) — só o que o cliente pode informar. Nunca inclua `Id`, `Ativo`, `DataCriacao` ou status controlado por regra: o que a entidade decide não se recebe do corpo da requisição.

Validação de formato (`[Required]`, `[MaxLength]`, `[EmailAddress]`) vai no DTO e é checada pelo `ModelState` no controller. Validação de **regra de negócio** (já existe, saldo insuficiente, data conflitante) vai no serviço, via notificador.

## Mappers

Métodos de extensão estáticos em `Models/Mappers/`:

```csharp
public static class AdicionalMapper
{
    public static AdicionalDto ToDto(this Adicional entidade) => new()
    {
        IdAdicional = entidade.IdAdicional,
        Nome        = entidade.Nome,
        ValorDiaria = entidade.ValorDiaria,
        Ativo       = entidade.Ativo
    };

    public static List<AdicionalDto> ToDtoList(this IEnumerable<Adicional> entidades)
        => entidades is null ? new() : entidades.Select(ToDto).ToList();
}
```

Não há AutoMapper e não é para introduzir um. Mapper escrito à mão quebra o build quando a entidade muda de forma — o mapeamento por convenção quebra em produção, calado. Para entidade que só nasce por `Criar(...)`, não existe `ToEntity()`: o serviço chama a fábrica passando os campos do DTO.

## Serviços

Interface + implementação em `Services/<Area>Services/`. O serviço é o único lugar onde regra de negócio de aplicação mora: ele carrega, valida, chama o método de domínio e persiste.

```csharp
public class AdicionalService : IAdicionalService
{
    private readonly IAdicionalRepository _repository;
    private readonly INotificadorService _notificador;

    public AdicionalService(IAdicionalRepository repository, INotificadorService notificador)
    {
        _repository  = repository;
        _notificador = notificador;
    }

    public async Task<AdicionalDto?> CriarAsync(CriarAtualizarAdicionalDto dto, CancellationToken ct = default)
    {
        if (!await Validar(dto, ct)) return null;

        var adicional = Adicional.Criar(dto.Nome, dto.ValorDiaria);
        var salvo = await _repository.InserirSalvarAsync(adicional, ct);
        return salvo.ToDto();
    }

    public async Task<bool> AtualizarAsync(int id, CriarAtualizarAdicionalDto dto, CancellationToken ct = default)
    {
        var adicional = await _repository.ObterPrimeiroAsync(a => a.IdAdicional == id, rastreado: true, ct: ct);
        if (adicional is null)
        {
            _notificador.Add("Adicional não encontrado");
            return false;
        }

        if (!await Validar(dto, ct)) return false;

        adicional.Atualizar(dto.Nome, dto.ValorDiaria);
        return await _repository.AtualizarSalvarAsync(adicional, ct);
    }
}
```

Convenções de assinatura:

| Operação | Retorno |
|---|---|
| obter um | `Task<XxxDto?>` — `null` quando não existe |
| listar | `Task<IReadOnlyList<XxxDto>>` |
| listar paginado | `Task<PaginatedResult<XxxDto>>` |
| criar | `Task<XxxDto?>` — `null` quando a validação falhou |
| atualizar / ativar / excluir | `Task<bool>` |

Todo método termina com `CancellationToken ct = default` e propaga o token. Métodos de consulta que carregam entidade rastreada para outro serviço reusar (`ObterPorIdRastreado`) podem devolver a entidade — é o único caso em que entidade sai da Application, e mesmo assim ela nunca chega ao controller.

Blocos privados de validação (`Validar`, `ValidarXxx`) concentram os `_notificador.Add` e devolvem `bool`, para reuso entre criar e atualizar.

## Listagem paginada

Toda listagem recebe os mesmos cinco parâmetros e monta a mesma ordenação dinâmica. Em vez de repetir isso por tela, três peças resolvem (todas em `assets/Application/`):

**`ConsultaPaginadaRequest`** — entra no controller como `[FromQuery]`, com os nomes `pagina`, `itensPorPagina`, `termo`, `ordenarPor`, `direcao`. Os limites ficam no `set`: página mínima 1 e teto de itens por página. Sem o teto, `?itensPorPagina=100000` vira varredura de tabela disparada da barra de endereço. `TermoNormalizado` já devolve o texto aparado, em minúsculas e nulo quando vazio — a normalização que o `LIKE` do Postgres exige, escrita uma vez.

**`OrdenacaoDeConsulta<T>`** — mapa declarativo de coluna para `OrderBy`, no lugar do `switch` com duas linhas por coluna:

```csharp
private static readonly OrdenacaoDeConsulta<Reserva> Ordenacoes =
    OrdenacaoDeConsulta<Reserva>.Padrao(r => r.DataInicio, descendente: true)
        .Com("nomecliente", r => r.Cliente.Usuario!.NomeCompleto)
        .Com("datafim", r => r.DataFim)
        .Com("status", r => r.Status);
```

Declare como `static readonly` — o mapa não muda entre requisições. Coluna desconhecida cai no padrão, que é o que a tela espera quando alguém edita a URL na mão; e como a chave é `Expression`, nenhum nome de coluna em string chega ao SQL.

**`ParaDto`** — troca as entidades da página pelos DTOs preservando os metadados, no lugar do bloco que copiava `Total`/`Pagina`/`TotalPaginas`/`ItensPorPagina` campo a campo (onde esquecer uma linha passava batido no compilador e aparecia como paginação quebrada na tela).

A listagem inteira fica assim:

```csharp
public async Task<PaginatedResult<ReservaDto>> ObterTodosPaginadoAsync(
    ConsultaPaginadaRequest consulta,
    int? status = null,
    CancellationToken ct = default)
{
    var busca = consulta.TermoNormalizado;

    Expression<Func<Reserva, bool>> filtro = r =>
        (busca == null || r.Filial.Nome.ToLower().Contains(busca))
        && (status == null || (int)r.Status == status);

    var reservas = await _reservaRepository.ObterPaginadoComFiltroAsync(
        filtro: filtro,
        ordenarPor: Ordenacoes.Montar(consulta),
        incluir: IncluirRelacionados,
        pagina: consulta.Pagina,
        itensPorPagina: consulta.ItensPorPagina,
        ct: ct);

    return reservas.ParaDto(ReservaMapper.ToDtoList);
}
```

E o controller:

```csharp
[HttpGet]
public async Task<ActionResult> ObterTodos(
    CancellationToken ct,
    [FromQuery] ConsultaPaginadaRequest consulta,
    [FromQuery] int? status = null)
    => CustomResponse(await _reservaService.ObterTodosPaginadoAsync(consulta, status, ct));
```

Filtros específicos da tela (status, filial, categoria) continuam parâmetros próprios: são o que muda de listagem para listagem, e escondê-los num objeto genérico só tornaria o contrato mais difícil de ler no Swagger.

Ao adotar isso numa Api que já existe, confira os nomes que o cliente manda hoje. Trocar `ordem` por `direcao`, ou `nome` por `termo`, é mudança de contrato HTTP e quebra o front sem avisar — nesses casos, ou mantenha o nome antigo com `[FromQuery(Name = "...")]`, ou combine a troca com quem consome.

## Notificador

`INotificadorService` é **scoped**: uma instância por requisição, injetada tanto nos serviços quanto no `MainController`. É essa identidade compartilhada que faz o controller enxergar o que o serviço reportou.

```csharp
public interface INotificadorService
{
    bool TemNotificacao();
    List<Notificacao> ObterNotificacoes();
    void Add(string notificacao);
}
```

`Notificacao` carrega `Mensagem`, `Status` (padrão `BadRequest`) e `Campo` opcional. Quando o campo é informado, ele vira a chave em `errors` na resposta — útil para a tela destacar o input errado.

Padrão de uso: acumule todas as notificações possíveis antes de decidir, para o usuário corrigir tudo de uma vez:

```csharp
if (await _repository.ExisteAsync(a => a.Nome == dto.Nome, ct))
    _notificador.Add("Adicional já cadastrado");

if (dto.ValorDiaria < 0)
    _notificador.Add("Valor da diária inválido");

if (_notificador.TemNotificacao()) return false;
```

Registrá-lo como singleton vaza mensagens entre usuários; como transient, o controller recebe uma instância vazia e a resposta sai 200 com corpo nulo. Se aparecer "o serviço reprovou mas a Api devolveu sucesso", o registro de DI é o primeiro lugar para olhar.

## Controllers

Herdam `MainController` e injetam o notificador para a base:

```csharp
[ApiController]
[Route("api/v1/adicionais")]
public class AdicionalController : MainController
{
    private readonly IAdicionalService _service;

    public AdicionalController(IAdicionalService service, INotificadorService notificador)
        : base(notificador)
    {
        _service = service;
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> ObterPorId(int id, CancellationToken ct)
    {
        var resultado = await _service.ObterPorIdAsync(id, ct);
        if (resultado is null) return NotFound($"Adicional {id} não encontrado.");
        return CustomResponse(resultado);
    }

    [HttpPost]
    public async Task<ActionResult> Criar([FromBody] CriarAtualizarAdicionalDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationResponse(ModelState);

        var resultado = await _service.CriarAsync(dto, ct);
        return CustomResponse(resultado, HttpStatusCode.Created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> Atualizar(int id, [FromBody] CriarAtualizarAdicionalDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationResponse(ModelState);

        var sucesso = await _service.AtualizarAsync(id, dto, ct);
        if (!sucesso) return CustomResponse();               // vira ProblemDetails com as notificações

        return CustomResponse(null, HttpStatusCode.NoContent);
    }
}
```

O que a base oferece:

| Método | Uso |
|---|---|
| `CustomResponse(resultado, status)` | retorno padrão — verifica notificações antes de responder sucesso |
| `ValidationResponse(ModelState)` | erro de formato vindo das DataAnnotations |
| `ValidationResponse(campo, erro)` | erro de validação pontual |
| `NotFound(mensagem)` | 404 em `ProblemDetails` |
| `Forbidden(mensagem)` | 403 em `ProblemDetails` |
| `ProblemResponse(status, detail, ...)` | qualquer outro erro HTTP |

Regras:
- **todo** retorno passa por um desses. `return Ok(x)` cru pula a checagem de notificação e devolve 200 com dados incompletos;
- quando o serviço devolve `false`/`null`, chame `CustomResponse()` sem argumento — a base transforma as notificações acumuladas em 4xx;
- nada de `try/catch` para virar resposta: erro esperado é notificação, erro inesperado é papel do middleware;
- `CancellationToken ct` como último parâmetro de toda action; o ASP.NET injeta sozinho.

Rotas e versionamento variam por repositório (`api/v{version:apiVersion}/[controller]` ou `api/v1/<nome-plural>`). **Copie o padrão do controller vizinho** em vez de normalizar.

## ProblemDetails (RFC 7807)

Toda resposta de erro sai no mesmo formato, montado pelas factories em `ProblemDetailsFactories`:

| Factory | Quando |
|---|---|
| `ProblemFactory.Create(status, detail, title, type, extensions)` | erro HTTP genérico (só aceita status ≥ 400) |
| `ValidationProblemFactory.FromModelState(modelState)` | falha de DataAnnotations |
| `ValidationProblemFactory.Single(campo, erro)` / `.For<T>(x => x.Campo, erro)` | erro de um campo |
| `ExceptionProblemFactory.Create(context, exception)` | usada pelo middleware |

O `NotificationProblemAdapterMapper` é a ponte entre notificação e resposta: pega o **maior** status entre as notificações, agrupa as mensagens por campo (`"geral"` quando não há campo) e devolve algo como

```json
{
  "type": "Padrão RCF 7807",
  "title": "Erro de regra de negócio",
  "status": 400,
  "instance": "/api/v1/adicionais",
  "errors": { "geral": ["Adicional já cadastrado", "Valor da diária inválido"] }
}
```

Formato único de erro é o que permite o front tratar falha num lugar só, em vez de adivinhar o formato por endpoint.

`ProblemException` existe para casos em que a camada precisa abortar com um status específico — o middleware reconhece e preserva o `ProblemDetails` embutido. Use com parcimônia: no fluxo normal, a resposta de erro nasce do notificador.

## Middleware de exceção

`ExceptionMiddleware` é o último recurso: registra o erro no log e converte qualquer exceção não tratada em `ProblemDetails` (500, ou o status do `ProblemException`), acrescentando `instance` e `traceId`. Ele só escreve se a resposta ainda não começou.

Registre-o **depois** dos middlewares de autenticação e antes do `MapControllers`, para envolver a execução das actions.

Não exponha `ex.Message` de exceção inesperada em produção — mensagem de erro de banco vaza estrutura interna. Em desenvolvimento é útil; controle isso pelo ambiente.

## Startup e injeção de dependência

Cada preocupação tem um par de extensions `AddXxxConfig` / `UseXxxConfig`, em `Extensions/`, e o `Program.cs` fica sendo uma lista legível de chamadas:

```csharp
services.AddInjecaoDependenciaApplicationsConfig();   // serviços da Application
services.AddHttpServices(config);                     // IHttpContextAccessor, options do appsettings
services.AddPostgresDbContext<AppDbContext>(config["ConnectionStrings:dbModelo"] ?? "");
services.AddRepositories();                           // repositórios da Infra
services.AddApiConfig();                              // controllers, opções de JSON
services.AddVersionamentoConfig();
services.AddSwaggerConfig();

var app = builder.Build();
app.UseSwaggerConfig(provider);
app.UseApiConfig();
app.UseAuthenticationConfig();
app.UseMiddleware<ExceptionMiddleware>();
app.MapControllers();
app.Run();
```

Tempo de vida:

| Registro | Tempo de vida | Porquê |
|---|---|---|
| `DbContext`, repositórios, serviços | Scoped | uma unidade de trabalho por requisição |
| `INotificadorService` | **Scoped** | precisa ser a mesma instância entre serviço e controller |
| `ICurrentUser` | Scoped | depende do `HttpContext` da requisição |
| serviços sem estado (e-mail, fila, chaves) | Singleton | não tocam no `DbContext` |

Nunca injete algo scoped dentro de um singleton — o `DbContext` fica preso à primeira requisição e passa a lançar `ObjectDisposedException` intermitente. Em job/hosted service, abra um escopo (`IServiceScopeFactory.CreateScope()`) e resolva de lá.

`ReferenceHandler.IgnoreCycles` na configuração de JSON evita estouro de serialização em navegação bidirecional — é rede de segurança, não licença para devolver entidade em vez de DTO.
