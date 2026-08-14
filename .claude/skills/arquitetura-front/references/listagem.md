# Listagem: `TabelaGenerica`

Toda tela de listagem usa o mesmo componente. Ele cuida de cabeçalho, busca com debounce, estado de carregamento, estado vazio, ordenação por clique no cabeçalho, seleção com ações em massa e paginação. A página cuida do estado e da chamada da Api.

Conteúdo: modelo mental · os três tipos de configuração · parâmetros · molde da página · filtros próprios · ações em massa · armadilhas.

## Modelo mental

A tabela **não conhece a Api**. Ela recebe a página de dados já pronta e, quando o usuário faz algo que muda a consulta (buscar, ordenar, trocar de página, trocar itens por página), ela atualiza os parâmetros com `@bind` e dispara `OnCarregarDados`. Quem refaz a chamada é a página.

```
TabelaGenerica                       Página
  usuário clica no cabeçalho
  → atualiza OrdenacaoPropriedade
    e OrdenacaoDirecao (@bind)
  → OnCarregarDados.InvokeAsync() ──► CarregarXxx()
                                        chama o serviço com o estado atual
                                        preenche `itens` e `totalItems`
                                        StateHasChanged
  ◄──────────────────────────────────── novo render com os dados novos
```

Consequência prática: se a lista não muda ao ordenar, o problema quase nunca é a tabela — é o `ordenarPor` que a Api não reconheceu, ou o `CarregarXxx` que não repassou o estado.

## Os três tipos de configuração

### `ColunaTabela<TItem>`

```csharp
new ColunaTabela<SeguroResponse>
{
    Titulo = "Valor Diária",       // texto do cabeçalho
    Propriedade = "ValorDiaria",   // nome enviado à Api em `ordenarPor`; também é o fallback de leitura por reflection
    Valor = s => s.ValorDiaria.ToString("C2"),   // como renderizar (vence a reflection)
    Template = null,               // RenderFragment quando precisa de HTML (badge, link, ícone)
    Ordenavel = true,
    Largura = "120",
    Alinhamento = "right",
}
```

`Valor` e `Template` são as duas formas de dizer o que aparece na célula; `Template` vence quando os dois existem. Sem nenhum dos dois, a coluna lê `Propriedade` por reflection — funciona, mas perde o erro de compilação quando a propriedade é renomeada, então prefira `Valor` para qualquer coisa formatada.

Coluna com HTML usa `Template` com a sintaxe de razor template:

```csharp
Template = seguro => @<span class="badge @(seguro.Ativo ? "bg-success" : "bg-danger")">
    @(seguro.Ativo ? "Ativo" : "Inativo")
</span>
```

`Propriedade` continua útil mesmo com `Template`: é o que vai no `ordenarPor` quando a coluna é ordenável.

### `AcaoTabela<TItem>` — botão por linha

```csharp
new AcaoTabela<SeguroResponse>
{
    Titulo = "Excluir",         // vira o `title` do botão
    Icone = "bi-trash",         // classe do Bootstrap Icons
    Cor = "danger",             // sufixo de `btn-outline-*`
    Acao = async (s) => await ExcluirSeguro(s)
}
```

### `AcaoEmMassa` — item do menu de selecionados

```csharp
new AcaoEmMassa
{
    Titulo = "Excluir selecionados",
    Icone = "bi-trash",
    Cor = "danger",
    Acao = async (ids) => await ExcluirSelecionados(ids),   // ids são string
    MostrarQuantidade = true
}
```

Declare `acoes` e `acoesEmMassa` como **propriedade calculada** (`=> new()`), não campo. Elas capturam métodos da página; como campo inicializado, congelam a captura antes da tela estar pronta.

## Parâmetros

**Obrigatórios**

| Parâmetro | Tipo | O que é |
|---|---|---|
| `Items` | `List<TItem>` | a página de dados já carregada |
| `TotalItens` | `int` | total no servidor — é ele que define quantas páginas existem |
| `Colunas` | `List<ColunaTabela<TItem>>` | definição das colunas |

**Estado da consulta** (sempre com `@bind-`)

| Parâmetro | Padrão | Observação |
|---|---|---|
| `PaginaAtual` | `1` | 1-based, igual ao da Api |
| `ItensPorPagina` | `10` | o setter já volta para a página 1 e dispara `OnCarregarDados` |
| `OrdenacaoPropriedade` | `null` | recebe o `Propriedade` da coluna clicada |
| `OrdenacaoDirecao` | `"asc"` | alterna sozinho ao clicar na mesma coluna |
| `TermoBusca` | `""` | atualizado depois do debounce |

**Comportamento**

| Parâmetro | Padrão | Observação |
|---|---|---|
| `OnCarregarDados` | — | `EventCallback` que a página usa para recarregar; sem ele a tabela não faz nada |
| `Carregando` | `false` | troca a tabela pelo spinner |
| `ObterId` | `null` | `Func<TItem,string>` para a seleção — **passe sempre** |
| `SelecaoHabilitada` | `true` | coluna de checkbox |
| `DelayBusca` | `500` | ms de debounce da busca |
| `MostrarSeletorItensPorPagina` | `true` | 10/25/50/100 |
| `MostrarFiltros` / `MostrarBuscaGlobal` | `true` | desliga a barra inteira ou só a busca |

**Aparência e slots**

`Titulo`, `PlaceholderBusca`, `ClasseTabela`, `LarguraColunaAcoes`, `IconeVazio`, `MensagemVazio`, `MensagemVazioDetalhe`, `MensagemCarregando` — e os `RenderFragment`: `AcoesCabecalho` (botões no topo do card), `FiltrosAdicionais` (selects próprios da tela), `AcaoVazio` (call to action no estado vazio), `AcoesTemplate` (substitui `Acoes` quando os botões precisam de HTML próprio).

## Molde da página de listagem

```razor
@page "/seguros"
@using Locadora_Auto.Front.Components.UI      @* onde TabelaGenerica declara o namespace *@
@using Locadora_Auto.Front.Models.Tabelas
@inject ISeguroService _seguroService
@inject INotificationService NotificacaoService
@inject IConfirmDialogService ConfirmService
@inject NavigationManager Navegacao
@rendermode InteractiveServer

<ConfirmDialog @ref="confirmDialog" />

<TabelaGenerica TItem="SeguroResponse"
                @bind-PaginaAtual="paginaAtual"
                @bind-ItensPorPagina="itensPorPagina"
                @bind-OrdenacaoPropriedade="ordenacaoPropriedade"
                @bind-OrdenacaoDirecao="ordenacaoDirecao"
                @bind-TermoBusca="termoBuscaGlobal"
                Items="seguros"
                TotalItens="totalItems"
                Colunas="colunas"
                Carregando="carregando"
                OnCarregarDados="CarregarSeguros"
                Acoes="acoes"
                AcoesEmMassa="acoesEmMassa"
                ObterId="@(s => s.IdSeguro.ToString())"
                Titulo="Seguros">
    <AcoesCabecalho>
        <a href="/seguros/novo" class="btn btn-primary btn-sm">
            <i class="bi bi-plus-circle me-1"></i>Novo Seguro
        </a>
    </AcoesCabecalho>
</TabelaGenerica>

@code {
    private ConfirmDialog? confirmDialog;

    private List<SeguroResponse> seguros = new();
    private bool carregando;
    private int totalItems;
    private int paginaAtual = 1;
    private int itensPorPagina = 10;
    private string? ordenacaoPropriedade;
    private string ordenacaoDirecao = "asc";
    private string termoBuscaGlobal = string.Empty;

    private List<ColunaTabela<SeguroResponse>> colunas = new() { /* ... */ };
    private List<AcaoTabela<SeguroResponse>> acoes => new() { /* ... */ };
    private List<AcaoEmMassa> acoesEmMassa => new() { /* ... */ };

    protected override async Task OnInitializedAsync() => await CarregarSeguros();

    private async Task CarregarSeguros()
    {
        carregando = true;
        StateHasChanged();

        try
        {
            var resultado = await _seguroService.ObterTodos(
                termo: string.IsNullOrWhiteSpace(termoBuscaGlobal) ? null : termoBuscaGlobal.Trim(),
                pagina: paginaAtual,
                itensPorPagina: itensPorPagina,
                ordenarPor: ordenacaoPropriedade,
                direcao: ordenacaoDirecao);

            seguros = resultado?.Items?.ToList() ?? new();
            totalItems = resultado?.Total ?? 0;
        }
        catch (Exception ex)   // só falha de transporte chega aqui
        {
            NotificacaoService.ShowError($"Erro ao carregar seguros: {ex.Message}");
            seguros = new();
            totalItems = 0;
        }
        finally
        {
            carregando = false;
            StateHasChanged();
        }
    }
}
```

Os dois `StateHasChanged` não são supérfluos: o primeiro pinta o spinner antes do `await`, o segundo garante o render final quando a continuação volta fora do ciclo de vida.

## Filtros próprios da tela

Vão no slot `FiltrosAdicionais`, e o handler zera a página antes de recarregar — senão o usuário filtra estando na página 7 e recebe uma tela vazia:

```razor
<FiltrosAdicionais>
    <select class="form-select" value="@filtroSituacao" @onchange="AlterarFiltroSituacao">
        <option value="">Todas as situações</option>
        <option value="true">Somente ativos</option>
        <option value="false">Somente inativos</option>
    </select>
</FiltrosAdicionais>
```

```csharp
private async Task AlterarFiltroSituacao(ChangeEventArgs e)
{
    filtroSituacao = e.Value?.ToString() ?? string.Empty;
    paginaAtual = 1;
    await CarregarSeguros();
}
```

Use `value` + `@onchange` (não `@bind`) quando o mesmo evento precisa disparar a recarga — `@bind` consome o `onchange`.

## Ações em massa

Os ids chegam como `List<string>` (a seleção é indexada por string). O padrão é: guarda de lista vazia → confirmação única → laço contando sucessos e erros → um toast só no fim → recarrega.

```csharp
private async Task ExcluirSelecionados(List<string> ids)
{
    if (!ids.Any())
    {
        NotificacaoService.ShowWarning("Nenhum seguro selecionado");
        return;
    }

    if (!await ConfirmService.ConfirmAsync($"Excluir {ids.Count} seguro(s)?", "Confirmar Exclusão"))
        return;

    var sucessos = 0; var erros = 0;

    foreach (var id in ids)
    {
        if (!int.TryParse(id, out var idSeguro)) { erros++; continue; }
        try { if (await _seguroService.Excluir(idSeguro)) sucessos++; else erros++; }
        catch { erros++; }
    }

    if (erros == 0)
        NotificacaoService.ShowSuccess($"{sucessos} seguro(s) excluído(s) com sucesso!");
    else
        NotificacaoService.ShowWarning($"{sucessos} excluído(s), {erros} falha(s)");

    await CarregarSeguros();
}
```

Um toast por item transformaria uma exclusão de 30 itens em 30 toasts empilhados — o `NotificationDisplay` mostra um de cada vez e o usuário veria só o último.

## Armadilhas

- **`ObterId` ausente.** O fallback procura uma propriedade `Id` e, se não achar, gera `Guid` novo a cada render: os checkboxes desmarcam sozinhos. Passe sempre.
- **A seleção é por página.** `OnParametersSet` reconstrói o dicionário a partir dos itens visíveis, então trocar de página perde as marcas. Se a tela precisa de seleção entre páginas, isso é trabalho novo — não é comportamento existente.
- **`ordenarPor` que a Api não conhece** volta na ordem padrão, sem erro. Confira o `OrdenacaoDeConsulta<T>` do lado da Api antes de marcar a coluna como `Ordenavel`.
- **`Formato` no `Locadora_Auto` só é aplicado** quando o nome da propriedade contém "data" ou "valor" — nos demais casos ele é ignorado em silêncio, então formate em `Valor`. A versão de `assets/` aplica o formato a qualquer valor `IFormattable`.
- **Excluir o último item da página** deixa a tela vazia com a paginação apontando para uma página que não existe mais. O padrão é decrementar antes de recarregar:
  ```csharp
  if (seguros.Count == 1 && paginaAtual > 1) paginaAtual--;
  ```
- **`Items` nunca deve ser `null`.** A tabela chama `.Any()` direto; devolva `new()` no lugar do nulo.
- **Ordenar a lista local para "ajudar"** dessincroniza o que a tabela mostra do que a Api paginou. Todo estado de consulta é enviado, nada é reordenado em memória.
