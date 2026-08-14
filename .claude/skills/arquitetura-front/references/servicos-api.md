# Serviços: falar com a Api

Conteúdo: `IApiHttpService` · molde de `IXxxService` · consulta paginada · como o erro vira toast · DI e `HttpClient` · token · `ApiConfig`.

## `IApiHttpService`

Um único ponto de saída HTTP. Todo `XxxService` o injeta; nada mais no front cria `HttpClient`.

```csharp
Task<T?>                                 GetAsync<T>(string url);
Task<(TResponse? objeto, HttpStatusCode code)> PostAsync<TResponse, TRequest>(string url, TRequest data);
Task<bool>                               PostAsync<TRequest>(string url, TRequest data);
Task<TResponse?>                         PutAsync<TRequest, TResponse>(string url, TRequest data);
Task<bool>                               PutAsync<TRequest>(string url, TRequest data);
Task<TResponse?>                         PatchAsync<TRequest, TResponse>(string url, TRequest data);
Task<bool>                               PatchAsync<TRequest>(string url, TRequest data);
Task<bool>                               DeleteAsync(string url);
Task<bool>                               PostMultipartAsync(string url, List<IBrowserFile> arquivos, string campoArquivo);
```

Três decisões que explicam a assinatura:

- **`GetAsync` devolve `default` em erro**, não lança. A página trata `null` como "não veio", e a mensagem já foi exibida.
- **`PostAsync<TResponse, TRequest>` devolve a tupla com o `HttpStatusCode`** porque criação distingue `201` de `200` e às vezes de `204`. Quando o resultado importa, use essa sobrecarga.
- **A serialização é camelCase, case-insensitive na leitura, ignorando nulos na escrita** — casa com o padrão do ASP.NET Core do outro lado sem atributo em nenhum DTO.

## Molde de `IXxxService`

Interface e implementação lado a lado em `Servicos/<Area>/`. A rota base é uma `const` — é o que faz mudança de rota ser edição de uma linha.

```csharp
public interface ISeguroService
{
    Task<SeguroResponse?> Inserir(CriarSeguroRequest request);
    Task<bool?> Atualizar(int id, EditarSeguroRequest request);
    Task<bool> Excluir(int id);
    Task<bool> Ativar(int id);
    Task<bool> Desativar(int id);
    Task<SeguroResponse?> ObterPorId(int id);
    Task<List<SeguroResponse>> ObterAtivos();
    Task<PaginatedResponse<SeguroResponse>> ObterTodos(
        string? termo = null,
        bool? ativo = null,
        int pagina = 1,
        int itensPorPagina = 10,
        string? ordenarPor = null,
        string? direcao = null,
        CancellationToken ct = default);
}

public class SeguroService : ISeguroService
{
    private const string RotaBase = "api/v1/seguros";
    private readonly IApiHttpService _api;

    public SeguroService(IApiHttpService api) => _api = api;

    public async Task<SeguroResponse?> Inserir(CriarSeguroRequest request)
    {
        var (objeto, code) = await _api.PostAsync<SeguroResponse, CriarSeguroRequest>(RotaBase, request);
        return code is HttpStatusCode.Created or HttpStatusCode.OK ? objeto : null;
    }

    public Task<bool> Excluir(int id) => _api.DeleteAsync($"{RotaBase}/{id}");

    public Task<bool> Ativar(int id) => _api.PatchAsync($"{RotaBase}/{id}/ativar", new { });
}
```

Convenções que valem seguir porque a página as assume:

- nome de intenção, não de verbo HTTP (`Inserir`, `ObterTodos`, `Desativar`);
- coleção nunca volta `null` — `?? new()` no serviço, para a página não precisar de guarda;
- `PATCH` sem corpo manda `new { }`: alguns servidores recusam `PATCH` sem `Content-Type`.

## Consulta paginada

A Api paginada aceita `pagina` / `itensPorPagina` / `termo` / `ordenarPor` / `direcao` (teto de 200 itens) mais os filtros próprios da entidade, e devolve `Items` + `Total`. A montagem da query segue sempre a mesma forma: só entra o que tem valor, e `direcao` só faz sentido acompanhando `ordenarPor`.

```csharp
public async Task<PaginatedResponse<SeguroResponse>> ObterTodos(
    string? termo = null, bool? ativo = null, int pagina = 1, int itensPorPagina = 10,
    string? ordenarPor = null, string? direcao = null, CancellationToken ct = default)
{
    var query = new QueryPaginada(pagina, itensPorPagina, termo, ordenarPor, direcao)
        .Com("ativo", ativo);

    return await _api.GetAsync<PaginatedResponse<SeguroResponse>>($"{RotaBase}{query}")
           ?? new PaginatedResponse<SeguroResponse>();
}
```

`QueryPaginada` (em `assets/Services/`) existe porque a alternativa é a mesma sequência de `if (x.HasValue) queryParams.Add(...)` copiada em cada serviço — e foi ali que o `Uri.EscapeDataString` foi esquecido nos termos com espaço. No `Locadora_Auto` os serviços ainda montam a lista à mão; ao escrever um serviço novo lá, siga o vizinho para não misturar dois estilos na mesma pasta.

`PaginatedResponse<T>` espelha o `PaginatedResult<T>` da Api:

```csharp
public class PaginatedResponse<T>
{
    public List<T> Items { get; set; } = new();
    public int Total { get; set; }
    public int Pagina { get; set; }
    public int TotalPaginas { get; set; }
    public int ItensPorPagina { get; set; }

    public bool TemPaginaAnterior => Pagina > 1;
    public bool TemProximaPagina => Pagina < TotalPaginas;
}
```

Nem toda listagem da Api usa esses nomes: `Cliente` e `Funcionario` ainda estão na assinatura antiga (`ordem` em vez de `direcao`, `nome`/`cpf`/`cargo` em vez de `termo`), e `Filial`/`CategoriaVeiculo` não aceitam ordenação. Antes de escrever o serviço, confira o controller.

## Como o erro vira toast

`TratarErrosResponse` roda depois de **toda** resposta e é o motivo de a página não precisar de `try/catch` para regra de negócio:

| Status | Log | O que o usuário vê |
|---|---|---|
| `400` | `Debug` | os erros do `ProblemDetails`, campo a campo, em toast de validação (8s) |
| `401` | `Warning` | "Sessão expirada. Faça login novamente" |
| `403` | `Warning` | "Acesso negado" |
| `404` | `Information` | "Recurso não encontrado" |
| `409` | `Warning` | "Conflito de dados" — é a concorrência otimista da Api |
| `5xx` | `Error` (com corpo) | "Erro interno do servidor" |

O `400` é o caso importante: é por ali que chegam as notificações que o serviço da Api acumulou (`_notificador.Add`). O corpo é um `ProblemDetails` RFC 7807; o front tenta lê-lo como `ValidationProblemDetails` (que tem `Errors`) e, se não houver dicionário, como `ErrorResponse` (mensagem única).

Isso significa que **a mensagem que o usuário lê foi escrita no serviço da Api**, em português, uma vez só. Reescrevê-la na página produz duas versões da mesma regra.

O log é seletivo de propósito: `404` é rotina (o usuário digitou um id que não existe) e não deve poluir o log de erro; `5xx` sempre carrega o corpo da resposta, que é onde está a pista.

## DI e `HttpClient`

Tudo é registrado em `AddServices(configuration)`, chamado uma vez no `Program.cs` do Front:

```csharp
services.AddScoped<INotificationService, NotificationService>();
services.AddScoped<IConfirmDialogService, ConfirmDialogService>();

services.Configure<ApiConfig>(configuration.GetSection("ApiConfig"));
services.AddHttpContextAccessor();

services.AddScoped<JwtAuthorizationHandler>();

services.AddHttpClient<IApiHttpService, ApiHttpService>((provider, client) =>
{
    var config = configuration.GetSection("ApiConfig").Get<ApiConfig>();
    client.BaseAddress = new Uri(config!.BaseUrlApiLocacao!);
})
.AddHttpMessageHandler<JwtAuthorizationHandler>();

services.AddScoped<ISeguroService, SeguroService>();   // um por área
```

`AddHttpClient<TInterface, TImpl>` registra o serviço **tipado** — `ApiHttpService` recebe um `HttpClient` já configurado e o `HttpClientFactory` cuida do pool de conexões. Não instancie `new HttpClient()` para falar com a Api: sem o factory, o socket fica preso em `TIME_WAIT` e a mudança de DNS não é percebida.

`Scoped` em Blazor Server é o **circuito**, não a requisição: o serviço vive enquanto a aba estiver aberta. Para `NotificationService` e `ConfirmDialogService` isso é exatamente o desejado — são eles que guardam o evento que o componente montado no layout assina.

## Token

O `HttpClient` da Api leva o `Bearer` por um `DelegatingHandler`, que lê o token de onde o Front o guardou. O modelo do Front é **cookie**: o login chama a Api, recebe `access_token`/`refresh_token` e os grava no `AuthenticationProperties` do cookie de autenticação (`Program.cs`, endpoint `/auth/login`). O handler recupera pelo `HttpContext`:

```csharp
public class JwtAuthorizationHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _accessor;

    public JwtAuthorizationHandler(IHttpContextAccessor accessor) => _accessor = accessor;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        var contexto = _accessor.HttpContext;

        if (contexto is not null && request.Headers.Authorization is null)
        {
            var token = await contexto.GetTokenAsync("access_token");

            if (!string.IsNullOrWhiteSpace(token))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, ct);
    }
}
```

Duas coisas a saber antes de mexer nisso:

- **O `HttpContext` some depois do primeiro render.** Em Blazor Server, a partir do momento em que o circuito assume, não há requisição HTTP corrente — o `IHttpContextAccessor` pode devolver `null` numa chamada disparada por clique. Se o token precisar sobreviver ao circuito, ele tem que ser capturado no início (no `OnInitializedAsync` de um provedor com escopo de circuito) e não lido de novo a cada chamada.
- **No `Locadora_Auto` esse handler está quebrado** — a lógica acima existe numa classe aninhada que nunca é registrada, e o handler ligado ao cliente é um repassador vazio. Só não aparece porque os `[Authorize]` da Api estão comentados. A versão de `assets/` é a corrigida.

## `ApiConfig`

```json
"ApiConfig": {
  "BaseUrlApiLocacao": "https://localhost:61977/"
}
```

```csharp
public class ApiConfig
{
    public string? BaseUrlApiLocacao { get; set; }
}
```

A barra final importa: `BaseAddress` sem barra faz o `HttpClient` descartar o último segmento do caminho ao combinar com uma rota relativa. Pelo mesmo motivo, a rota do serviço começa **sem** barra (`api/v1/seguros`, não `/api/v1/seguros` — a barra inicial joga fora o caminho da base).

No `Locadora_Auto` esse valor aponta hoje para `https://localhost:44310/`, que não é a porta da Api. É a primeira coisa a conferir quando toda tela devolve erro de conexão.
