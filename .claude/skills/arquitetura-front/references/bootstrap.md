# Montar a arquitetura num front novo

Roteiro para levar esta espinha para outro projeto. Substitua `Meu.Projeto` pelo nome real em tudo que segue.

## 1. Projetos

```powershell
dotnet new blazor  -n Meu.Projeto.Front --interactivity Server
dotnet new classlib -n Meu.Projeto.Front.Services
dotnet new classlib -n Meu.Projeto.Front.Models

dotnet sln add Meu.Projeto.Front Meu.Projeto.Front.Services Meu.Projeto.Front.Models

dotnet add Meu.Projeto.Front.Services reference Meu.Projeto.Front.Models
dotnet add Meu.Projeto.Front          reference Meu.Projeto.Front.Services Meu.Projeto.Front.Models
```

Nos três `.csproj`: `<Nullable>enable</Nullable>` e `<ImplicitUsings>enable</ImplicitUsings>` — os arquivos de `assets/` contam com isso.

`--interactivity Server` já entrega `App.razor`, `Routes.razor` e o `_Imports.razor` no formato do .NET 8. Se o template vier com `--interactivity None`, o `blazor.web.js` não abre circuito e nenhum clique funciona.

## 2. Pacotes

**Front.Models** — só `FluentValidation`. É a regra que mantém o projeto reaproveitável: nada de UI, nada de HTTP.

**Front.Services**
```
Microsoft.Extensions.Http                            # AddHttpClient
Microsoft.Extensions.Options.ConfigurationExtensions # Configure<ApiConfig>
```

Mais o framework do ASP.NET, porque três coisas do `assets/` vêm dele: `IHttpContextAccessor` e `GetTokenAsync` no handler de token, e `IBrowserFile` no upload.

```xml
<ItemGroup>
  <FrameworkReference Include="Microsoft.AspNetCore.App" />
</ItemGroup>
```

Se o projeto não tem autenticação nem upload, tire `JwtAuthorizationHandler` e `PostMultipartAsync` e a referência sai junto — sobram só os dois pacotes acima.

**Front**
```
Blazored.FluentValidation                            # <FluentValidationValidator />
FluentValidation.DependencyInjectionExtensions       # AddValidatorsFromAssemblyContaining
Serilog.AspNetCore                                   # opcional, se quiser log em arquivo
```

Bootstrap e Bootstrap Icons entram pelo `App.razor` (CDN ou `wwwroot/`). A `TabelaGenerica`, o `ConfirmDialog` e o `NotificationDisplay` usam classes do Bootstrap 5 e ícones `bi-*`; sem eles a tela funciona, mas fica sem estilo — e o dropdown de ações em massa precisa do `bootstrap.bundle.min.js`, que traz o Popper.

## 3. Copiar os arquivos base

A pasta de primeiro nível dentro de `assets/` já é o projeto de destino, e a estrutura abaixo dela é a mesma — basta copiar o conteúdo de cada uma para o projeto correspondente:

```
assets/Front/          →  Meu.Projeto.Front/
assets/Front.Services/ →  Meu.Projeto.Front.Services/
assets/Front.Models/   →  Meu.Projeto.Front.Models/
```

```powershell
Copy-Item .\assets\Front\*          -Destination .\Meu.Projeto.Front\          -Recurse
Copy-Item .\assets\Front.Services\* -Destination .\Meu.Projeto.Front.Services\ -Recurse
Copy-Item .\assets\Front.Models\*   -Destination .\Meu.Projeto.Front.Models\   -Recurse
```

`ColunaTabela` e `AcaoTabela` vêm em `assets/Front/Models/Tabelas/`, ou seja, no projeto **Blazor** e não em `Front.Models` — é o `RenderFragment` que impede o contrário, e é isso que mantém `Front.Models` livre de UI.

Depois troque `{{RootNamespace}}` por `Meu.Projeto` em todos eles:

```powershell
Get-ChildItem -Recurse -Include *.cs,*.razor |
  ForEach-Object {
    (Get-Content $_.FullName -Raw).Replace('{{RootNamespace}}', 'Meu.Projeto') |
      Set-Content $_.FullName -Encoding utf8
  }
```

O token foi escolhido de propósito para **não compilar** se ficar para trás — placeholder que passa despercebido vira namespace errado em produção.

## 4. `Program.cs`

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddServices(builder.Configuration);          // extension do Front.Services
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStaticFiles();
app.UseAntiforgery();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.Run();
```

`appsettings.json`:

```json
"ApiConfig": { "BaseUrlApiLocacao": "https://localhost:61977/" }
```

A barra final é obrigatória — sem ela o `HttpClient` descarta o último segmento ao combinar `BaseAddress` com a rota relativa.

## 5. Montar o chassi da UI

`App.razor` — as rotas precisam do render mode, senão a página abre estática:

```razor
<Routes @rendermode="new InteractiveServerRenderMode()" />
```

`MainLayout.razor` — o display de notificação entra **uma vez**, fora do fluxo do conteúdo, para ficar sobre tudo:

```razor
@inherits LayoutComponentBase

<NotificationDisplay />

<div class="app-layout">
    <aside class="sidebar"><NavMenu /></aside>
    <main class="content">@Body</main>
</div>
```

`_Imports.razor` — acrescente os `@using` que toda página vai querer (componentes, modelos de tabela, serviços de notificação), para não repeti-los em cada `.razor`.

Menu: uma classe `MenuService` com uma lista de `MenuItem` (título, ícone, url, subitens) resolve navegação e título do topo no mesmo lugar. Registre como `Scoped`.

## 6. Primeira tela

Faça uma entidade simples inteira antes de escrever a segunda — ela vira o molde que o resto do time copia. Ordem: `Response` + `Request` + validador → `IXxxService` + `XxxService` + registro na DI → `ListarXxx` com `TabelaGenerica` → `CriarXxx`/`EditarXxx`/`VisualizarXxx` → entrada no menu.

Escolha para isso uma entidade com listagem paginada e ativar/desativar: ela exercita busca, ordenação, paginação, ação por linha, ação em massa e confirmação — o conjunto inteiro de uma vez.

## O que é obrigatório e o que é opcional

**Obrigatório** — sem isso a arquitetura descrita nesta skill não fecha:
- os três projetos com a direção de dependência preservada;
- `IApiHttpService` como único ponto de saída HTTP, com a tradução de erro centralizada;
- `INotificationService` + `NotificationDisplay` montado no layout;
- `IXxxService` por área, com a rota em `const`;
- `TabelaGenerica` para listagem paginada pelo servidor.

**Opcional, por necessidade**:
- `JwtAuthorizationHandler` e autenticação — só se a Api exigir token;
- `IConfirmDialogService` — só se houver ação destrutiva (mas quase sempre há);
- `InputMoeda` e máscaras — só onde o campo pede;
- Serilog, tema com tokens CSS, colapso de sidebar, tema escuro.

## Diferenças em relação ao `Locadora_Auto`

Os arquivos de `assets/` são a versão de referência, com ajustes sobre o que está no repositório hoje:

| Ajuste | Motivo |
|---|---|
| `JwtAuthorizationHandler` é uma classe só, com `SendAsync` sobrescrito | no projeto a lógica está numa classe aninhada que nunca é registrada — o token não é enviado |
| `PostAsync<TRequest>` (sem retorno) devolve `response.IsSuccessStatusCode` | no projeto devolve `true` sempre, mesmo em erro |
| `INotificationService` é dependência obrigatória do `ApiHttpService` | no projeto é parâmetro opcional `= null` usado sem checagem: `NullReferenceException` dentro do tratamento de erro |
| desserialização de erro protegida por `try/catch` | corpo que não é JSON (HTML de proxy, resposta vazia) derruba o `JsonSerializer` no meio do tratamento |
| `Task.Delay(2000)` removido do tratamento de erro | segurava a resposta por 2s dentro do caminho de erro |
| `ObterValorFormatado` aplica `Formato` por tipo (`IFormattable`) | no projeto só aplica quando o nome da propriedade contém "data" ou "valor" |
| `ObterIdItem` avisa quando `ObterId` não foi passado | o fallback gerava `Guid` novo a cada render e quebrava a seleção em silêncio |
| `QueryPaginada` no lugar da lista de `if` | cada serviço remontava a query à mão; foi onde o `EscapeDataString` acabou esquecido |
| `ValidationProblemDetails` no namespace da pasta em que está | no projeto o arquivo mora em `Front.Models/Error/` mas declara `...Front.Services.Models` |
| `TabelaGenerica` declara namespace correspondente à pasta | no projeto está em `Components/Tabela/` e declara `Components.UI` |

Em projeto que já existe, **não** substitua o arquivo em uso pelo de `assets/` sem pedido explícito: `TabelaGenerica` e `ApiHttpService` são usados por todas as telas ao mesmo tempo.

Detalhe de edição no `Locadora_Auto`: os arquivos estão em UTF-8 **com BOM** e CRLF. Preserve ao editar.
