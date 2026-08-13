# Formulários, entradas e avisos

Conteúdo: `EditForm` + FluentValidation · onde o validador mora · página de criar · página de editar · inputs customizados · máscaras com JS · upload · toasts · confirmação.

## `EditForm` + FluentValidation

```razor
<EditForm Model="@seguro" OnValidSubmit="@ManipularEnvio">
    <FluentValidationValidator />

    <div class="mb-3">
        <label class="form-label">Nome <span class="text-danger">*</span></label>
        <InputText @bind-Value="seguro.Nome" class="form-control" />
        <ValidationMessage For="@(() => seguro.Nome)" />
    </div>

    <button type="submit" class="btn btn-primary" disabled="@processando">
        @if (processando)
        {
            <span class="spinner-border spinner-border-sm me-1"></span> <span>Salvando...</span>
        }
        else
        {
            <i class="bi bi-check-circle me-1"></i> <span>Salvar</span>
        }
    </button>
</EditForm>
```

`OnValidSubmit` só dispara quando o modelo passa na validação — é o que dá o `disabled` correto e o erro embaixo do campo sem uma linha de código de controle.

`<FluentValidationValidator />` (do pacote `Blazored.FluentValidation`) acha o `AbstractValidator<T>` do modelo pela DI (`AddValidatorsFromAssemblyContaining<Program>()` no `Program.cs`). Não use `<DataAnnotationsValidator />` junto: dois validadores no mesmo `EditContext` produzem mensagens duplicadas.

**Não revalide à mão dentro do `OnValidSubmit`.** Algumas telas do `Locadora_Auto` instanciam o validador de novo (`new SeguroValidator()`) para mandar os erros ao toast — mas o `OnValidSubmit` só é chamado quando já está tudo válido, então esse bloco nunca falha e vira código morto. Escolha um caminho:

- **erro embaixo do campo** (recomendado): `OnValidSubmit` + `ValidationMessage`, que é onde o usuário está olhando;
- **erro no toast**: troque para `OnSubmit` e valide à mão, aí sim mandando os erros para `ShowValidationErrors`.

## Onde o validador mora, e o que ele não valida

`Front.Models/Validadores/XxxValidator.cs`, um `AbstractValidator<T>` por `Request` (o de criar e o de editar costumam ser classes distintas no mesmo arquivo, porque os `Request` são diferentes).

```csharp
public class SeguroValidator : AbstractValidator<CriarSeguroRequest>
{
    public SeguroValidator()
    {
        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("O nome do seguro é obrigatório")
            .MaximumLength(45).WithMessage("O nome deve ter no máximo 45 caracteres");

        RuleFor(x => x.ValorDiaria)
            .GreaterThan(0).WithMessage("O valor da diária deve ser maior que zero");
    }
}
```

Valida **formato**: obrigatório, tamanho, faixa, data no futuro, e-mail bem formado. Não valida o que depende de estado — CPF já cadastrado, veículo disponível, filial ativa. Esse tipo de regra só existe onde estão os dados: no serviço da Api, que responde com notificação. Imitá-la aqui cria duas verdades, e a do front envelhece primeiro.

Limites de tamanho, por outro lado, **devem** espelhar a configuração da entidade na Api. Quando divergem, o usuário digita 60 caracteres, o front aceita e a Api recusa — o pior dos dois mundos.

## Página de criar

```csharp
private CriarSeguroRequest seguro = new();
private bool processando;

private async Task ManipularEnvio()
{
    processando = true;
    StateHasChanged();

    try
    {
        var criado = await _seguroService.Inserir(seguro);

        if (criado != null)
        {
            NotificacaoService.ShowSuccess("Seguro cadastrado com sucesso!", "Cadastro Realizado");
            await Task.Delay(1500);              // o toast precisa aparecer antes de a rota trocar
            Navegacao.NavigateTo("/seguros");
        }
        // recusa da Api já foi notificada pelo ApiHttpService — não invente mensagem aqui
    }
    catch (Exception ex)                          // só falha de transporte
    {
        NotificacaoService.ShowError($"Erro ao cadastrar seguro: {ex.Message}");
    }
    finally
    {
        processando = false;
        StateHasChanged();
    }
}
```

O `processando` protege contra duplo clique — que numa criação significa dois registros. `try/finally` garante que o botão volta mesmo quando algo estoura.

## Página de editar

Duas diferenças em relação à de criar: carrega antes de mostrar e trabalha com o `EditarXxxRequest`, não com o `Response`.

```csharp
[Parameter] public int Id { get; set; }

private EditarSeguroRequest? seguro;
private bool carregando = true;

protected override async Task OnInitializedAsync()
{
    var atual = await _seguroService.ObterPorId(Id);

    if (atual is null)
    {
        NotificacaoService.ShowWarning("Seguro não encontrado");
        Navegacao.NavigateTo("/seguros");
        return;
    }

    seguro = new EditarSeguroRequest
    {
        Nome = atual.Nome,
        ValorDiaria = atual.ValorDiaria,
        // ... campo a campo, de propósito
    };

    carregando = false;
}
```

A cópia campo a campo é o ponto: mandar de volta o `Response` inteiro envia dados que a Api não aceita em escrita (id, datas de auditoria, coleções) e, quando o contrato mudar, o erro aparece na compilação em vez de virar `400` em produção. É o mesmo raciocínio do mapper manual do lado da Api.

Enquanto `carregando`, renderize o spinner — `seguro` é nulo e o `EditForm` estoura com `Model` nulo.

## Inputs customizados

Herde `InputBase<TValue>` quando o campo precisa mostrar uma coisa e guardar outra (moeda, CPF, telefone, CEP). O contrato é: `CurrentValue` é o modelo, o `value` do `<input>` é a exibição, e `TryParseValueFromString` faz a ponte para a validação.

```razor
@typeparam TValue
@inherits InputBase<TValue>

<input type="text" class="form-control"
       value="@ValorExibicao"
       @oninput="OnInput"
       @onblur="OnBlur"
       @attributes="AdditionalAttributes" />
```

`InputMoeda` (em `assets/`) guarda `decimal`/`double` e exibe `pt-BR`: o usuário digita só dígitos e o componente divide por 100, então `3500` vira `R$ 35,00`. Isso evita o problema clássico da vírgula decimal — `@bind-Value` num `<input type="number">` usa cultura invariante e recusa `35,00`.

`ValidationMessage For="@(() => modelo.Campo)"` funciona igual em input customizado, desde que ele herde `InputBase<T>` — é a herança que registra o campo no `EditContext`.

## Máscaras com JS

Máscara puramente visual (CPF, CNPJ, telefone, CEP) é aplicada por `wwwroot/js/Mascaras.js` via interop, e o modelo guarda **só os dígitos**:

```csharp
protected override async Task OnAfterRenderAsync(bool primeiraRenderizacao)
{
    if (primeiraRenderizacao)
        await JS.InvokeVoidAsync("mascaras.aplicarMascaraCPF", _inputId);
}

public async ValueTask DisposeAsync()
{
    try { await JS.InvokeVoidAsync("mascaras.removerMascara", _inputId); }
    catch { /* circuito já caiu: JSDisconnectedException */ }
}
```

Três regras que essa ponte impõe:

- **Interop só depois do render.** Em `OnInitializedAsync` o elemento ainda não existe no DOM; a chamada falha em silêncio ou estoura, dependendo do script.
- **`_inputId` único por instância** (`$"cpf-{Guid.NewGuid():N}"`), senão duas instâncias do componente na mesma tela disputam o mesmo elemento.
- **`DisposeAsync` com `try/catch`.** Quando o usuário fecha a aba, o circuito cai antes do dispose e o `JSDisconnectedException` sobe no meio do teardown.

Guardar dígitos no modelo e formatar na exibição também evita o bug de enviar `123.456.789-00` para uma coluna que espera 11 caracteres.

## Upload

```csharp
await _api.PostMultipartAsync($"api/v1/categorias/{id}/fotos", arquivos, "fotos");
```

`InputFile` devolve `IBrowserFile`; o `OpenReadStream` tem limite padrão baixo (512 KB) e o `PostMultipartAsync` o eleva explicitamente. Valide tamanho e `ContentType` **antes** de enviar — em Blazor Server o arquivo trafega pelo SignalR até o servidor, então recusar cedo economiza o upload inteiro.

## Toasts

```csharp
NotificacaoService.ShowSuccess("Seguro cadastrado com sucesso!", "Cadastro Realizado");
NotificacaoService.ShowWarning("Nenhum seguro selecionado", "Atenção");
NotificacaoService.ShowError("Erro ao carregar: " + ex.Message, "Erro");
NotificacaoService.ShowValidationErrors(erros);   // Dictionary<string, string[]>
```

O `NotificationDisplay` fica montado **uma vez** no `MainLayout` e mostra **uma** notificação por vez — cada nova substitui a anterior. É o motivo de ação em massa juntar tudo num toast final em vez de notificar item a item.

As de sucesso/erro somem sozinhas (5s); a de validação fica até o usuário clicar em "Entendi", porque ela costuma listar vários campos.

Quem assina `OnNotification` precisa desassinar no `Dispose`, ou o componente antigo continua sendo chamado depois de destruído.

## Confirmação

```razor
<ConfirmDialog @ref="confirmDialog" />
```

```csharp
if (!await ConfirmService.ConfirmAsync("Excluir o seguro 'X'? Esta ação não pode ser desfeita.",
                                       "Confirmar Exclusão"))
    return;
```

O serviço dispara o evento, o componente aparece, e o `await` só volta quando o usuário responde — um `TaskCompletionSource` segura a continuação. Por isso funciona igual a um `confirm()` sem travar o circuito.

A mensagem deve dizer **o que** será afetado (nome do registro, quantidade) e se dá para desfazer. "Tem certeza?" sozinho não dá ao usuário o que ele precisa para decidir.
