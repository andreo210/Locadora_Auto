# Testar o domínio

A entidade é a camada mais barata de testar: não tem dependência, não precisa de fake, não precisa de `async`. É também onde o teste rende mais, porque a invariante que ela protege vale para todos os serviços que a usam — provar a regra uma vez cobre todo mundo que passa por lá.

## O que testar

Derive os testes da própria superfície da entidade. Para cada uma destas coisas existe pelo menos um teste:

| No código | No teste |
|---|---|
| `Criar` com argumentos válidos | nasce no estado inicial certo (status, `Ativo`, campos copiados) |
| cada `if`/`throw` dentro de `Criar` | um teste que dispara aquela recusa |
| cada método de transição (`Cancelar`, `Finalizar`, `Ativar`) | leva ao estado esperado |
| cada transição inválida | é recusada (cancelar duas vezes, finalizar o que já finalizou) |
| método idempotente ou condicional (`Expirar`) | age quando deve **e** não age quando não deve |
| comparação de data | o limite exato (o "mesmo dia" conta ou não?) |

O caso que mais escapa é o último par: quase todo mundo testa `Expirar` expirando, quase ninguém testa `Expirar` não mexendo em reserva já cancelada. É justamente aí que o bug mora.

## O formato

Sem fake, sem `async`, três linhas visíveis:

```csharp
[Fact]
public void Cancelar_encerra_a_reserva()
{
    var (_, reserva) = Fabrica.ClienteComReserva();

    reserva.Cancelar();

    Assert.Equal(StatusReserva.Cancelado, reserva.Status);
    Assert.False(reserva.Ativo);
}
```

Status **e** `Ativo`: o par existe porque são dois campos que precisam andar juntos, e é comum um método novo mexer só num deles.

## Recusa: `Assert.Throws`

A entidade recusa lançando. Afirme o tipo que ela realmente lança — não uniformize no teste o que está diverso no código:

```csharp
var erro = Assert.Throws<InvalidOperationException>(() =>
    cliente.ReservarVeiculo(1, Fabrica.DaquiADias(-1), Fabrica.DaquiADias(5), 1, 1));

Assert.Contains("inicio", erro.Message);
```

O `Assert.Contains` na mensagem só entra quando o mesmo tipo de exceção cobre duas regras diferentes e o teste precisa distinguir qual delas disparou. Se há um `throw` só naquele caminho, o tipo basta — verificar a frase inteira transforma reescrita de texto em quebra de teste.

Neste repositório convivem `DomainException` e `InvalidOperationException` (`Reserva` usa os dois). Isso é o estado do código, não uma escolha a corrigir dentro do teste: use o tipo que o método lança de fato.

## Agregado: teste pela raiz

Quando `Criar` é `internal`, a entidade é de agregado e a raiz é a única porta de entrada. O projeto de teste é outro assembly, então **não consegue** chamar `Reserva.Criar` — e isso é a garantia funcionando, não um obstáculo.

```csharp
var cliente = Fabrica.Cliente();
cliente.ReservarVeiculo(cliente.IdCliente, Fabrica.DaquiADias(3), Fabrica.DaquiADias(6), idFilial, idCategoria);

var reserva = cliente.Reservas.Single();
```

**Não adicione `InternalsVisibleTo` para furar isso.** Testar por dentro do agregado prova que a entidade funciona por um caminho que a aplicação não usa — e deixa passar exatamente a regra que a raiz aplica antes de criar o filho.

`Fabrica.ClienteComReserva()` já devolve as duas pontas prontas, com ids definidos.

## Datas

As entidades comparam com `DateTime.UtcNow` (nunca `DateTime.Now` — o banco é `timestamp with time zone` e o driver só aceita `Kind=Utc`). No teste, isso tem duas consequências:

- data futura/passada vem de `Fabrica.DaquiADias(n)` / `Fabrica.DiasAtras(n)`. Literal (`new DateTime(2026, 1, 1)`) vira passado com o tempo e o teste quebra sozinho num dia qualquer;
- o limite se testa com a data que a própria entidade guarda, não com uma recalculada:

```csharp
// a comparação é por dia inteiro: no próprio dia do início a reserva continua valendo
reserva.Expirar(reserva.DataInicio);

Assert.Equal(StatusReserva.Reservado, reserva.Status);
```

Se a entidade recebe "agora" por parâmetro (`Expirar(DateTime referencia)`), o teste controla o relógio de graça — é o motivo de esse parâmetro existir. Entidade que lê `DateTime.UtcNow` por dentro só é testável no limite com tolerância, e aí o teste fica frouxo.

## Objeto de valor

Value object (`Endereco` e afins) segue as mesmas regras, com um teste a mais: igualdade. Se ele é `record` ou implementa `Equals`, dois valores iguais têm que se comparar iguais — é disso que o EF depende para detectar mudança em tipo owned.

## Fábrica

Todo teste de domínio parte de `Fabrica`. Ao criar um método novo lá:

```csharp
public static Adicional Adicional(string nome = "Cadeirinha", decimal valorDiaria = 25m)
    => Domain.Entidades.Adicional.Criar(nome, valorDiaria);
```

Padrão válido, parâmetro opcional só para o que varia nos testes. Quando a entidade ganha campo obrigatório novo, a fábrica é o único lugar a corrigir — e é por isso que ela não pode ficar sendo contornada "só neste teste".
