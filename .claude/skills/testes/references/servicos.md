# Testar serviços

O serviço é o que a Api expõe: ele carrega pelo repositório, aplica a regra, chama o método de domínio e grava. O teste substitui só o repositório — notificador, entidade e mapper são os reais.

## Fake tipado: três linhas

As interfaces concretas não declaram nada além de `IRepositorioGlobal<T>`, então cada fake é só a amarração do genérico com a interface que o serviço pede:

```csharp
public class AdicionalRepositoryFake : RepositorioFake<Adicional>, IAdicionalRepository
{
    public AdicionalRepositoryFake(ArmazemFake? armazem = null) : base(armazem) { }
}
```

Todos ficam em `Fakes/RepositoriosFake.cs`, um embaixo do outro. Se algum dia uma interface ganhar método próprio, é nesse arquivo que a implementação entra.

## Cenário

Um helper estático por classe de teste. Com até três coisas para devolver, tupla nomeada:

```csharp
private static (AdicionalService service, AdicionalRepositoryFake repositorio, NotificadorService notificador)
    Montar(params Adicional[] jaCadastrados)
{
    var armazem = new ArmazemFake().Semear(jaCadastrados);
    var repositorio = new AdicionalRepositoryFake(armazem);
    var notificador = new NotificadorService();

    return (new AdicionalService(repositorio, notificador), repositorio, notificador);
}
```

Acima disso, uma classe `Cenario` com `required init` — a tupla de sete elementos vira ilegível na desconstrução:

```csharp
private sealed class Cenario
{
    public required ReservaService Service { get; init; }
    public required NotificadorService Notificador { get; init; }
    public required ClienteRepositoryFake Clientes { get; init; }
    public required Clientes Cliente { get; init; }
}
```

Os parâmetros do `Montar` são as variações que os testes precisam (`clienteAtivo: false`, `veiculosDisponiveis: 0`), não todos os dados do cenário. Cada parâmetro novo ali é um `if` a mais para quem lê.

## Um armazém para todos os fakes

Serviço com várias dependências precisa que os repositórios enxerguem o mesmo conjunto de dados. O `ArmazemFake` é criado uma vez e passado a todos:

```csharp
var armazem = new ArmazemFake();

var cliente = Fabrica.Cliente();
armazem.Semear(cliente);

var categoria = Fabrica.Categoria();
armazem.Semear(categoria);

var service = new ReservaService(
    new ReservaRepositoryFake(armazem),
    new ClienteRepositoryFake(armazem),
    new CategoriaVeiculosRepositoryFake(armazem),
    ...,
    notificador);
```

Fake construído sem armazém cria o próprio — dois fakes assim são dois bancos diferentes, e o serviço não acha o que o teste semeou. É a causa mais comum de "o teste diz que o cliente não existe".

`Semear` atribui id a quem entra com a chave zerada, imitando a sequência do banco. Quando o teste precisa de um id específico, `Fabrica.DefinirId(entidade, 7)` antes de semear.

## Cascade de agregado

Se a raiz cria o filho (`cliente.ReservarVeiculo(...)` monta a `Reserva` dentro da coleção) e o serviço grava só a raiz, o EF real persiste o filho junto. O fake não faz cascade — a `Reserva` ficaria só dentro do objeto `Clientes`, e a consulta seguinte do serviço não a encontraria.

Quem precisa disso sobrescreve `SalvarAsync` no fake da raiz:

```csharp
public class ClienteRepositoryFake : RepositorioFake<Clientes>, IClienteRepository
{
    public ClienteRepositoryFake(ArmazemFake? armazem = null) : base(armazem) { }

    public override Task<int> SalvarAsync(CancellationToken ct = default)
    {
        var reservas = Armazem.Tabela<Reserva>();

        foreach (var cliente in Tabela)
        {
            foreach (var reserva in cliente.Reservas)
            {
                if (reservas.Contains(reserva)) continue;

                ChavePrimaria.AtribuirSeVazia(reserva, Armazem.ProximoId<Reserva>());
                reservas.Add(reserva);
            }
        }

        return base.SalvarAsync(ct);
    }
}
```

Só a raiz cujo agregado o serviço realmente consulta depois precisa disso. Não replique em todos os fakes "por precaução".

## As três asserções

**Falha de regra de negócio** — as três juntas:

```csharp
Assert.Null(resultado);                        // recusou
Assert.True(notificador.TemNotificacao());     // e disse por quê
Assert.Equal(0, repositorio.Salvamentos);      // e não gravou
```

**Sucesso** — o notificador primeiro, senão a falha aponta para o sintoma em vez da causa:

```csharp
Assert.False(notificador.TemNotificacao());
Assert.NotNull(resultado);
Assert.Equal("GPS", resultado!.Nome);
Assert.Equal(1, repositorio.Salvamentos);
```

**Mensagem específica**, quando o teste precisa provar *qual* regra barrou:

```csharp
Assert.Contains(notificador.ObterNotificacoes(), n => n.Mensagem.Contains("inativo"));
```

Trecho, nunca a frase inteira. E `Contains` com `StringComparison.OrdinalIgnoreCase` quando a inicial da mensagem puder mudar.

### O caso que mais importa: a exceção que virou notificação

A entidade lança quando o estado é impossível; o serviço tem que barrar antes e notificar, para a Api responder 400 em vez de 500. Esse é o teste que fixa o contrato inteiro:

```csharp
[Fact]
public async Task Cancelar_duas_vezes_notifica_em_vez_de_estourar_a_excecao_do_dominio()
{
    var cenario = Montar();
    var criada = await cenario.Service.CriarAsync(Dto());
    await cenario.Service.CancelarAsync(criada!.IdReserva);

    var segunda = await cenario.Service.CancelarAsync(criada.IdReserva);

    Assert.False(segunda);
    Assert.Contains(cenario.Notificador.ObterNotificacoes(), n => n.Mensagem.Contains("canceladas"));
}
```

Se o serviço esquecer a checagem, o teste falha com `DomainException` não capturada — que é exatamente o 500 que o usuário veria.

## Estado da entidade

Como não há tracking, a entidade no armazém é a mesma instância do teste. Verificar o estado dela é legítimo e direto:

```csharp
Assert.Equal("Cadeirinha infantil", adicional.Nome);
Assert.Equal(StatusReserva.Cancelado, cenario.Cliente.Reservas.Single().Status);
```

O que isso **não** prova é persistência — a alteração aconteceria igual sem `SalvarAsync`. Por isso o `Salvamentos` anda junto.

## Listagem paginada

Três testes cobrem a maior parte dos defeitos:

```csharp
// metadados da página
var pagina = await service.ObterTodosPaginadoAsync(
    new ConsultaPaginadaRequest { Pagina = 1, ItensPorPagina = 2 });

Assert.Equal(3, pagina.Total);              // total é antes da paginação
Assert.Equal(2, pagina.Items.Count);
Assert.Equal(2, pagina.TotalPaginas);
Assert.True(pagina.TemProximaPagina);
Assert.All(pagina.Items, item => Assert.IsType<ReservaDto>(item));   // reprojetou

// ordenação pedida pela query string
var porData = await service.ObterTodosPaginadoAsync(
    new ConsultaPaginadaRequest { OrdenarPor = "datafim", Direcao = "asc" });

var datas = porData.Items.Select(r => r.DataFim).ToList();
Assert.Equal(datas.OrderBy(d => d), datas);

// teto de itens por página
var absurda = await service.ObterTodosPaginadoAsync(
    new ConsultaPaginadaRequest { ItensPorPagina = 500_000 });

Assert.Equal(ConsultaPaginadaRequest.MaximoItensPorPagina, absurda.ItensPorPagina);
```

O `Assert.IsType<XxxDto>` é o que pega o serviço devolvendo entidade crua. O teto é o que impede `?itensPorPagina=100000` virar varredura de tabela pela barra de endereço.

Ordenação e normalização de termo, isolados de qualquer serviço, ficam em `Consultas/` — `OrdenacaoDeConsulta` e `ConsultaPaginadaRequest` são objetos puros e se testam sem fake nenhum.

## Armadilhas

- **fakes com armazéns diferentes** — o serviço não acha o dado semeado (veja acima);
- **`incluir` ignorado** — o fake não faz `Include`. Um serviço que esqueceu de pedir a navegação passa no teste e quebra em produção; se o teste precisa de `reserva.Cliente`, monte a navegação ao construir a entidade;
- **`rastreado: true` não muda nada no fake** — o teste não prova que a entidade veio rastreada, e portanto não prova que o token de concorrência protege;
- **filtro que não traduz para SQL** — LINQ to Objects aceita chamada de método próprio, o Npgsql não. Passar aqui não garante que a consulta roda;
- **id zerado** — entidade criada e não semeada fica com id 0, e um filtro `x.Id == dto.Id` casa com ela por acidente;
- **`params` vazio** — `Montar()` sem argumentos é armazém vazio, o que é o cenário certo para testar "não encontrado";
- **estado estático entre testes** — o xUnit roda coleções em paralelo; armazém, fábrica ou contador em `static` mutável dá falha intermitente.

## Checklist de serviço novo

- caminho feliz de cada operação de escrita: devolve DTO, `Salvamentos == 1`, entidade no estado esperado
- uma falha por regra de negócio para cada `_notificador.Add(...)` do serviço
- "não encontrado" (id inexistente) para cada operação que carrega por id
- toda transição que a entidade recusaria com exceção, provando que o serviço barrou antes
- se há listagem paginada: metadados, ordenação e teto
