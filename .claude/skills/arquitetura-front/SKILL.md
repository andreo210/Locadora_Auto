---
name: arquitetura-front
description: Arquitetura de referência do front Blazor Server — três projetos (Front / Front.Services / Front.Models), páginas que nunca falam HTTP, `ApiHttpService` traduzindo `ProblemDetails` em toast, `TabelaGenerica` para toda listagem paginada e `EditForm` + FluentValidation nos formulários. Use sempre que for escrever, revisar ou mover código de front nessa arquitetura: criar/alterar página `.razor`, componente, serviço de consumo da Api, `Request`/`Response`/validador; montar tela de listagem com filtro, ordenação, paginação ou ações em massa; montar formulário de cadastro/edição; exibir erro, toast ou confirmação; ou iniciar um front novo com a mesma espinha (os arquivos base prontos estão em `assets/`). Vale também para perguntas do tipo "por que meu `@onclick` não dispara", "onde eu ponho essa chamada da Api", "como pagino essa lista", "como mostro o erro que a Api devolveu" e "esse validador é do front ou da Api".
---

# Arquitetura do Front

Esta skill descreve a arquitetura, não um repositório. Ela vale para o `Locadora_Auto` e para qualquer front novo que reuse a mesma espinha — os arquivos genéricos estão em `assets/` prontos para copiar (veja `references/bootstrap.md`).

Escopo: **front-end (Blazor Server + os projetos que o alimentam)**. A Api é assunto da skill `arquitetura-api`; as duas se encontram no contrato HTTP e em nada mais.

Código, nomes e comentários em **português**; inglês só para termos de framework/HTTP (`Request`, `Response`, `Parameter`, `EditForm`).

## Mapa dos projetos

```
   Front                 páginas .razor, componentes, layout, tema, DI de UI
  (Blazor Server)        fala com o usuário; não monta URL nem lê status code
        │
        ▼
   Front.Services        IXxxService, ApiHttpService, notificação, token
        │                fala HTTP; não conhece Razor nem estado de tela
        ▼
   Front.Models          XxxRequest, XxxResponse, validadores, tipos de erro
                         sem UI e sem HttpClient — só contrato e regra de formato
```

A seta é a direção da dependência. `Front.Models` é o único projeto que não referencia nenhum outro: se você precisou de um `using Microsoft.AspNetCore.Components` lá dentro, o tipo está no projeto errado — é por isso que `ColunaTabela<T>` (que usa `RenderFragment`) mora em `Front/Models/Tabelas/`, e não no projeto `Front.Models`.

`Front.Services` referencia `Microsoft.AspNetCore.Components.Forms` por um motivo só: `IBrowserFile` no upload de arquivos. É a única concessão — `RenderFragment`, `NavigationManager` e `StateHasChanged` continuam fora.

## O caminho de uma interação

```
usuário clica → Página .razor (@rendermode InteractiveServer)
                  guarda o estado da tela: carregando, pagina, termo, ordenacao
                  chama IXxxService
                → XxxService (Front.Services)
                  monta rota + query string, chama IApiHttpService
                → ApiHttpService
                  HttpClient nomeado (BaseAddress = ApiConfig) + handler de token
                  2xx  → desserializa e devolve o objeto
                  4xx  → lê o ProblemDetails, dispara o toast, devolve null/false
                  5xx  → loga, dispara toast genérico, devolve null/false
                ← Página reage ao null/false: não navega, recarrega ou só para
```

O `NotificationDisplay` (montado uma vez no `MainLayout`) escuta o evento do `INotificationService` e desenha o toast. Nenhuma página precisa saber disso — ela só chama `ShowSuccess`/`ShowError` quando quer falar por conta própria.

## As seis regras que definem a arquitetura

### 1. A página não fala HTTP

Página injeta `IXxxService` e chama método com nome de intenção (`ObterTodos`, `Inserir`, `Desativar`). Rota, query string, verbo e status code ficam no serviço.

O motivo é o custo de manutenção: uma rota da Api que muda deve ser um `const string RotaBase` corrigido em um arquivo, não uma caçada por string interpolada espalhada em quatro `.razor` por entidade. E o code-block de uma página já carrega estado, ciclo de vida e render — encher de `HttpClient` transforma revisão de tela em revisão de rede.

```csharp
// Front.Services/Servicos/Seguro/SeguroService.cs
public class SeguroService : ISeguroService
{
    private const string RotaBase = "api/v1/seguros";
    private readonly IApiHttpService _api;

    public async Task<SeguroResponse?> ObterPorId(int id) =>
        await _api.GetAsync<SeguroResponse>($"{RotaBase}/{id}");
}
```

### 2. Erro da Api já virou toast antes de a página ver

`ApiHttpService` centraliza o tratamento: em `400` ele desserializa o `ProblemDetails` que a Api devolve (o mesmo que o `CustomResponse` monta a partir do notificador) e chama `ShowValidationErrors`; nos outros status manda uma mensagem por faixa. Para a página, falha é `null` ou `false`.

Isso completa o desenho da Api: lá a regra de negócio violada não vira exceção, vira `ProblemDetails`; aqui ela não vira exceção também, vira toast. Se cada página fizesse `try/catch` para exibir "seguro já cadastrado", a mensagem escrita no serviço da Api seria reescrita à mão em toda tela — e sairia diferente em cada uma.

```csharp
var criado = await _seguroService.Inserir(seguro);

if (criado != null)
{
    NotificacaoService.ShowSuccess("Seguro cadastrado com sucesso!");
    await Task.Delay(1500);          // deixa o toast aparecer antes de sair da tela
    Navegacao.NavigateTo("/seguros");
}
// recusa da Api já foi notificada pelo ApiHttpService: aqui não se inventa mensagem
```

`try/catch` na página continua válido para **falha de transporte** — Api fora do ar, timeout, DNS. Aí a exceção é real e a tela precisa se recompor (`carregando = false`, lista vazia). Regra prática: se a Api respondeu, o toast já saiu; se ela não respondeu, é seu.

### 3. Quem pagina, ordena e filtra é o servidor

A página guarda `paginaAtual`, `itensPorPagina`, `ordenacaoPropriedade`, `ordenacaoDirecao`, `termoBuscaGlobal` e os filtros da tela; qualquer mudança neles refaz a chamada. Nunca `.Where()` ou `.OrderBy()` sobre a `List<T>` que voltou.

O motivo é aritmético: a lista em mãos é uma página só. Ordenar 10 de 4.000 registros ordena a página, não a consulta, e o rodapé "mostrando 1-10 de 4.000" passa a mentir. A `TabelaGenerica` é burra de propósito — ela não conhece a Api, só avisa `OnCarregarDados` e espera a página trazer dados novos.

Detalhe do contrato: a Api paginada usa `pagina` / `itensPorPagina` / `termo` / `ordenarPor` / `direcao` e devolve `Items` + `Total`. `ordenarPor` recebe o **nome da propriedade** que a coluna declarou, então o `Propriedade` da `ColunaTabela` precisa bater com o que o `OrdenacaoDeConsulta<T>` da Api aceita, ou a ordenação volta silenciosamente na ordem padrão.

### 4. Formulário é `EditForm` + validador FluentValidation

O `Request` mora em `Front.Models/Request/<Area>/`, o `XxxValidator : AbstractValidator<CriarXxxRequest>` em `Front.Models/Validadores/`, e a página usa `<EditForm Model="...">` com `<FluentValidationValidator />` e um `<ValidationMessage For="..." />` por campo.

O validador do front é **UX, não autoridade**: ele evita a ida ao servidor para erro de formato (campo vazio, tamanho, valor negativo). A regra que depende de estado — CPF já cadastrado, veículo indisponível, placa duplicada — não tem como ser checada aqui e não deve ser imitada; ela volta da Api como notificação e o toast a exibe. Duplicar regra de banco no front produz duas verdades que divergem na primeira mudança.

### 5. Confirmação destrutiva passa pelo `IConfirmDialogService`

```csharp
var confirmado = await ConfirmService.ConfirmAsync(
    $"Deseja realmente excluir o seguro '{seguro.Nome}'? Esta ação não pode ser desfeita.",
    "Confirmar Exclusão");

if (!confirmado) return;
```

Nunca `confirm()` ou `alert()` por JS interop. No Blazor Server o diálogo nativo do navegador **bloqueia o circuito SignalR**: enquanto ele está aberto nenhum evento sobe, e se algo der errado a aba fica surda até o reload. O `ConfirmDialog` é um modal Razor comum, e o serviço espera a resposta com um `TaskCompletionSource` — o `await` funciona como o do `confirm()`, sem travar nada.

O `<ConfirmDialog @ref="confirmDialog" />` precisa estar na página que confirma (as telas de listagem o declaram no topo).

### 6. Página com interação precisa de `@rendermode InteractiveServer`

Sem a diretiva, a página renderiza estática: `@onclick` não dispara, `@bind` não atualiza e nada no console acusa. É o erro que mais consome tempo de quem chega no projeto, e o sintoma ("a tela abre, mas os botões não fazem nada") não parece um problema de render mode.

## Onde cada arquivo mora

| Projeto | Pasta | O que vai lá |
|---|---|---|
| Front.Models | `Request/<Area>/` | `CriarXxxRequest`, `EditarXxxRequest` |
| Front.Models | `Response/` | `XxxResponse` — espelha o DTO que a Api devolve |
| Front.Models | `Validadores/` | `XxxValidator : AbstractValidator<CriarXxxRequest>` |
| Front.Models | `Enum/` | enums espelhando os da Api |
| Front.Models | `Error/` | `ValidationProblemDetails`, `ErrorResponse` |
| Front.Services | `Servicos/<Area>/` | `IXxxService` + `XxxService` |
| Front.Services | `Servicos/` | `ApiHttpService`, `PaginatedResponse<T>` |
| Front.Services | `Utils/Notificacao/` | `NotificationService`, `ConfirmDialogService` |
| Front.Services | `Extensions/` | DI, handler de token |
| Front | `Components/Pages/<Area>/` | `ListarXxx`, `CriarXxx`, `EditarXxx`, `VisualizarXxx` |
| Front | `Components/Tabela/` | `TabelaGenerica.razor` |
| Front | `Components/Forms/` | inputs com máscara e blocos reaproveitáveis de formulário |
| Front | `Components/Notificacao/` | `NotificationDisplay`, `ConfirmDialog` |
| Front | `Models/Tabelas/` | `ColunaTabela<T>`, `AcaoTabela<T>`, `AcaoEmMassa` |
| Front | `Models/Layout/` | `MenuService`, `MenuItem` |
| Front | `wwwroot/css/` | `tema.css` (cores e fontes), `app.css` (layout) |

Registro de DI: tudo em `AddServices` (`Front.Services/Extensions/`), tudo `Scoped`. Em Blazor Server "scoped" é o **circuito** — vive enquanto a aba estiver aberta, não uma requisição. Por isso serviço com estado (notificação, diálogo) funciona como singleton por usuário, e é exatamente o que se quer aqui.

## Roteiro de uma tela nova

Trabalhe de baixo para cima — cada camada depende da anterior:

1. **Front.Models** — `XxxResponse` espelhando o DTO da Api, `CriarXxxRequest`/`EditarXxxRequest`, `XxxValidator`.
2. **Front.Services** — `IXxxService` + `XxxService` com `RotaBase`, um método por endpoint; registrar em `AddServices`.
3. **Front** — `ListarXxx.razor` com `TabelaGenerica`, depois `CriarXxx` / `EditarXxx` / `VisualizarXxx`.
4. **Menu** — entrada em `MenuService.GetMenuItems()` (é ele que também dá o título do topo).
5. Compilar e abrir a tela; para rodar Api + Front fora do Visual Studio existe a skill `rodar-app`.

Antes de escrever qualquer arquivo, **leia uma fatia vertical pronta** do projeto em que você está e siga a grafia dela (nome de rota, sufixo de Request/Response, organização de pastas). A arquitetura é a mesma; as convenções variam por repositório. No `Locadora_Auto`, `Seguro` é a fatia mais recente e mais limpa.

## Anti-padrões (checklist de revisão)

- `HttpClient`, URL da Api ou `HttpStatusCode` dentro de `.razor`
- `try/catch` na página para transformar recusa da Api em mensagem — o toast já saiu
- `.Where()` / `.OrderBy()` / `.Skip().Take()` sobre a lista que a Api devolveu
- `<table>` escrita à mão numa listagem nova em vez de `TabelaGenerica`
- `confirm()` / `alert()` por JS interop
- página interativa sem `@rendermode InteractiveServer`
- validador do front repetindo regra que depende do banco (unicidade, disponibilidade)
- `Response` sendo enviado num `POST`/`PUT` no lugar do `Request` (o contrato de escrita é outro)
- componente que assina evento (`OnNotification`, `OnShow`) sem `IDisposable` para desassinar — vaza circuito
- `DateTime.Now` para comparar com data vinda da Api, que chega em UTC
- serviço novo criado mas esquecido no `AddServices` — compila e explode ao abrir a página

## Referências

Leia sob demanda — não carregue tudo de uma vez:

- `references/listagem.md` — `TabelaGenerica` parâmetro a parâmetro, `ColunaTabela`/`AcaoTabela`/`AcaoEmMassa`, o molde de estado da página, ações em massa, armadilhas de seleção e paginação.
- `references/servicos-api.md` — `IApiHttpService` assinatura a assinatura, molde de `IXxxService`, `PaginatedResponse`, tradução de erro, DI, token e `ApiConfig`.
- `references/formularios.md` — `EditForm` + FluentValidation, inputs com máscara, `InputBase<T>` customizado, upload, confirmação e toasts.
- `references/bootstrap.md` — montar a arquitetura num front novo a partir de `assets/`.

## Arquivos base

`assets/` traz a implementação de referência dos blocos genéricos, com o namespace como `{{RootNamespace}}` para troca em massa:

A pasta de cada arquivo já é o projeto de destino:

```
assets/Front/            Components/Tabela/TabelaGenerica.razor
                         Components/Notificacao/NotificationDisplay.razor, ConfirmDialog.razor
                         Components/Forms/InputMoeda.razor
                         Models/Tabelas/ColunaTabela.cs, AcaoTabela.cs

assets/Front.Services/   Servicos/ApiHttpService.cs, QueryPaginada.cs, PaginatedResponse.cs
                         Configuration/ApiConfig.cs
                         Extensions/JwtAuthorizationHandler.cs, DependencyInjectionServiceExtensions.cs
                         Utils/Notificacao/NotificationService.cs, ConfirmDialogService.cs

assets/Front.Models/     Notificacao/NotificationEventArgs.cs
                         Error/ValidationProblemDetails.cs, ErrorResponse.cs
```

Em front novo: copie, substitua `{{RootNamespace}}`, registre na DI. Em projeto existente, **não** troque o arquivo em uso pelo de `assets/` sem pedido explícito — o do repositório pode ter divergido de propósito, e `TabelaGenerica` e `ApiHttpService` são usados por todas as telas de uma vez.

## Armadilhas do `Locadora_Auto`

Divergências reais entre o que está no repositório e o que se esperaria. Nenhuma é para "arrumar de passagem" — mexer nelas atinge várias telas.

- **O token não está indo.** `Extensions/HttpClientAuthorizationUser.cs` declara `JwtAuthorizationHandler : DelegatingHandler` sem nenhum override, e a lógica que injeta o `Bearer` está numa classe **aninhada** (`HttpClientAuthorizationUser`) que ninguém registra. O handler ligado ao `HttpClient` hoje é um repassador vazio. Só não quebra porque os `[Authorize]` da Api estão comentados. O `assets/Services/JwtAuthorizationHandler.cs` é a versão corrigida.
- **`ApiConfig:BaseUrlApiLocacao` aponta para `https://localhost:44310/`**, que não é a porta da Api (`61977`). Corrija no `appsettings` antes de acusar o código.
- **`TabelaGenerica.razor` está em `Components/Tabela/` mas declara `@namespace ...Components.UI`** — o `@using` da página é `Locadora_Auto.Front.Components.UI`.
- **Dois lugares geram o namespace `Locadora_Auto.Front.Models.*`**: a pasta `Models/` dentro do projeto Blazor e o projeto `Locadora_Auto.Front.Models`. São assemblies diferentes com o mesmo prefixo — `...Models.Tabelas` vem do primeiro, `...Models.Response` do segundo.
- **`Front.Models/Error/ValidationProblemDetails.cs` declara `namespace Locadora_Auto.Front.Services.Models`** — pasta de um projeto, namespace de outro. O `using` que compila é o do namespace.
- **`ApiHttpService.PostAsync<TRequest>` (a sobrecarga sem retorno) devolve `true` mesmo em erro.** Use a sobrecarga com `TResponse`, que devolve o `HttpStatusCode`, quando o resultado importar.
- **`ApiHttpService` recebe `INotificationService` como parâmetro opcional `= null`** e o usa sem checagem — se o registro sumir da DI, o erro que aparece é `NullReferenceException` no meio do tratamento de erro.
- **`ObterValorFormatado` só aplica `Formato`** quando o nome da propriedade contém "data" ou "valor". Para o resto, formate com `Valor = x => x.Campo.ToString("...")`.
- **`ObterIdItem` cai em `GetProperty("Id") ?? GetProperty("IdCliente")`** quando `ObterId` não é passado, e sem nenhum dos dois gera um `Guid` novo a cada render — a seleção para de funcionar. Passe `ObterId` sempre.
- **A seleção da tabela é por página**: mudar de página zera o que estava marcado, porque `OnParametersSet` reconstrói o dicionário a partir dos itens visíveis.
- **`Midlleware/`** (com o typo) é o nome real da pasta de middleware do Front. Use a grafia existente; não renomeie sem pedido.
- Arquivos são UTF-8 **com BOM** e CRLF. Preserve ao editar.
