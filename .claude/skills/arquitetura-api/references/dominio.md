# Domain

O `Domain` é o único projeto sem referências — nem EF, nem ASP.NET, nem pacote de terceiros. Ele contém: entidades, enums de status, exceção de domínio, interfaces de repositório e os contratos de auditoria.

Se você precisou de um `using` de framework aqui, a regra está na camada errada.

## Anatomia de uma entidade

```csharp
public class Adicional
{
    public int IdAdicional { get; private set; }
    public string Nome { get; private set; } = null!;
    public decimal ValorDiaria { get; private set; }
    public bool Ativo { get; private set; }

    protected Adicional() { }                      // exigido pelo EF; ninguém mais usa

    public static Adicional Criar(string nome, decimal valorDiaria)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new DomainException("Nome é obrigatório");
        if (valorDiaria < 0)
            throw new DomainException("Valor inválido");

        return new Adicional { Nome = nome, ValorDiaria = valorDiaria, Ativo = true };
    }

    public void Atualizar(string nome, decimal valorDiaria) { /* mesmas validações */ }

    public void Ativar()    => Ativo = true;
    public void Desativar() => Ativo = false;
}
```

Elementos obrigatórios e o porquê de cada um:

- **`private set` em tudo** — fecha o único caminho pelo qual estado inválido entraria. Com `set` público, cada serviço novo teria que lembrar de validar; com `private set`, quem esquece não compila.
- **Construtor sem parâmetros `protected`/`private`** — o EF materializa por reflexão e precisa dele, mas ele fica fechado para o código de aplicação, que só tem `Criar`.
- **`static Criar`** — um único ponto onde a entidade nasce válida. Nome do método sempre em português.
- **Um método por transição de estado** — `Ativar`, `Desativar`, `Cancelar`, `Finalizar`, `Expirar`. A regra de "de qual estado posso ir para qual" vive dentro do método, não espalhada nos serviços:

```csharp
public void Cancelar()
{
    if (Status != StatusReserva.Reservado)
        throw new DomainException("Somente reservas ativas podem ser canceladas");

    Status = StatusReserva.Cancelado;
    Ativo = false;
}
```

- **Enum de status** ao lado da entidade, no mesmo arquivo (`StatusReserva`, `StatusLocacao`). Persistido com `HasConversion<int>` na configuração — em consulta, compare como enum, não com cast dentro da `Expression`.

### Datas

A entidade grava e compara em UTC. `DateTime.UtcNow` sempre:

```csharp
// as datas são gravadas em UTC (timestamp with time zone): comparar com Now erraria pelo fuso
if (inicio <= DateTime.UtcNow)
    throw new InvalidOperationException("Data de início não pode ser menor que a data atual");
```

## Agregado e raiz

Um agregado é o conjunto de entidades que precisam mudar juntas para o estado continuar coerente. A raiz é a única porta de entrada.

**A convenção de visibilidade diz quem é o quê:**

| Assinatura | Significado |
|---|---|
| `public static Criar` | raiz de agregado — pode ser criada diretamente por um serviço |
| `internal static Criar` | entidade **interna** ao agregado — só a raiz cria |

`Reserva.Criar` é `internal`: quem cria é `Clientes.ReservarVeiculo(...)`. `Multa`, `Vistoria` e `Caucao` seguem o mesmo padrão dentro de `Locacao`.

**Ter repositório próprio não faz de uma entidade uma raiz.** O repositório existe para consulta, e leitura não atravessa invariante — `IReservaRepository` lista e filtra reservas sem passar pelo cliente, e isso é correto. O que precisa passar pela raiz é a **escrita** que muda o invariante do agregado.

A raiz expõe as coleções internas encapsuladas:

```csharp
private readonly List<Reserva> _reserva = new();
public IReadOnlyCollection<Reserva> Reservas => _reserva;

public void ReservarVeiculo(int idCliente, DateTime inicio, DateTime fim, int idFilial, int idCategoria)
    => _reserva.Add(Reserva.Criar(idCliente, inicio, idFilial, fim, idCategoria));
```

`IReadOnlyCollection` para fora, `List` privada para dentro: ninguém adiciona item por fora sem passar pelo método que valida.

### Quando um agregado toca o outro

Acontece e é legítimo — só precisa ser deliberado e documentado num comentário XML na classe. Dois casos reais:

- `Locacao.Criar` chama `reserva.Finalizar()` e `veiculo.Indisponibilizar()`: a locação nascendo é o evento que fecha a reserva e trava o veículo.
- Expiração em lote varre reservas por tempo em vez de carregar todos os clientes — é uma varredura, não uma operação de negócio de um cliente específico.

Se você estiver criando um caso novo desses, escreva no comentário **por que** a exceção existe. Sem isso, o próximo leitor assume que foi descuido e "conserta".

## Interfaces de repositório

Ficam em `IRepositorio/`, no Domain, porque quem define o que precisa ser buscado é o domínio; a Infra só implementa. Na maioria dos casos a interface é vazia:

```csharp
public interface IAdicionalRepository : IRepositorioGlobal<Adicional> { }
```

Declare método próprio só quando a consulta for específica de verdade e não couber nos parâmetros `filtro` / `ordenarPor` / `incluir` do `RepositorioGlobal`.

## Auditoria — quem criou e quem alterou

Implemente `IAuditoria` na entidade e as quatro propriedades são preenchidas sozinhas no `SaveChangesAsync`:

```csharp
public class Clientes : IAuditoria
{
    // ... estado da entidade com private set ...

    // auditoria — set público de propósito: quem escreve é a infraestrutura
    public DateTime DataCriacao { get; set; }
    public string? IdUsuarioCriacao { get; set; }
    public DateTime? DataModificacao { get; set; }
    public string? IdUsuarioModificacao { get; set; }
}
```

O `set` público aqui é a exceção consciente à regra do `private set`: esses campos não são estado de negócio, são metadados escritos pelo `DbContext`. O usuário vem do `ICurrentUser` (`"SYSTEM"` quando não há requisição autenticada — job em background, seed, migration).

## Histórico temporal — como o registro era antes

Para guardar a versão anterior a cada `UPDATE`/`DELETE`, a entidade declara `ITemporalEntity<THistory>` e existe uma classe de histórico espelhando as colunas que interessam:

```csharp
public class Clientes : IAuditoria, ITemporalEntity<ClienteHistorico> { /* ... */ }

public class ClienteHistorico : ITemporalHistory
{
    public int IdHistorico { get; set; }
    public int IdCliente { get; set; }
    public DateTime DataEvento { get; set; }     // exigidos por ITemporalHistory
    public string? Acao { get; set; }            // "UPDATE" ou "DELETE"
    public string? UsuarioEvento { get; set; }
    // + as colunas que você quer preservar, com o mesmo nome da entidade
}
```

O `SaveChangesAsync` detecta a interface, instancia o histórico, copia os **valores originais** por nome de propriedade e insere junto na mesma transação. Consequências práticas:

- a classe de histórico precisa de construtor sem parâmetros (`new()`) e `set` público — ela é um espelho, não uma entidade de negócio;
- o nome da propriedade tem que bater com o da entidade, senão o campo simplesmente não é copiado (falha silenciosa);
- copie só o que você vai consultar depois; histórico não é backup.

O mecanismo é opcional por entidade: sem a interface, nada acontece.
