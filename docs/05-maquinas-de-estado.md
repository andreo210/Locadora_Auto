# 05 — Máquinas de estado

Cada diagrama reflete literalmente as transições implementadas nos métodos das entidades em
`Locadora_Auto.Domain/Entidades/`. Estados declarados no enum mas nunca atribuídos aparecem
marcados como **inalcançável**.

---

## 1. Locação — `StatusLocacao`

```mermaid
stateDiagram-v2
    direction LR

    [*] --> Criada : Locacao.Criar(...)

    Criada --> EmAndamento : RegistrarVistoria(Retirada)
    Criada --> Cancelada : Cancelar()

    EmAndamento --> Atrasada : MarcarComoAtrasada(agora)<br/>agora > DataFimPrevista
    EmAndamento --> Devolvida : RegistrarDevolucao(dataFimReal,<br/>kmFinal, filialDevolucao)
    Atrasada --> Devolvida : RegistrarDevolucao(...)

    Devolvida --> Fechada : Fechar(valorFinal)

    Fechada --> Finalizada : LiquidarSaldo()<br/>saldo <= 0
    Fechada --> ComSaldoResidual : LiquidarSaldo()<br/>saldo > 0
    ComSaldoResidual --> Finalizada : ConfirmarPagamento()<br/>quando o saldo zera

    Finalizada --> [*]
    Cancelada --> [*]

    note right of Criada
        Placa comprometida, carro ainda
        no pátio. É a janela de check-out.
        Único ponto em que Cancelar() cabe.
    end note

    note right of EmAndamento
        Carro na rua. AtualizarDados(),
        seguro e adicional aceitam
        Criada e EmAndamento.
    end note

    note right of Devolvida
        Posse encerrada, conta em aberto.
        Vistoria ainda é aceita aqui;
        de Fechada em diante, não.
    end note
```

**As duas vidas do contrato.** A física vai de `Criada` a `Devolvida` e responde "onde está o
carro"; a financeira vai de `Fechada` a `Finalizada`/`ComSaldoResidual` e responde "quem deve o
quê". Antes da A1 existia só uma, e receber o carro gravava `Finalizada` — com o contrato já
constando concluído, tudo o que a apuração cobraria (km excedente, combustível, limpeza, avaria,
diária excedente) se perdia sem deixar rastro.

`RegistrarDevolucao` exige o **par de vistorias** (RN-57): sem base comparável entre retirada e
devolução não há cobrança que se sustente numa contestação.

Transições **proibidas**, todas cobertas por teste em `LocacaoTests`:

- `Criada → Devolvida` — devolver carro que nunca foi liberado.
- `EmAndamento → Cancelada` e `Atrasada → Cancelada` — carro que rodou se devolve, não se cancela;
  cancelar apagaria o contrato e o quilômetro rodado junto.
- `Fechada → Devolvida` — reabrir fechamento. Correção é lançamento novo (RN-31).
- `Finalizada → *` e `Cancelada → *`.
- `DevolverCaucao()` antes de `Fechada`.

### Regras

| RN | Regra | Porquê |
|---|---|---|
| **RN-57** | Contrato nasce em `Criada`; só vai a `EmAndamento` com vistoria de retirada registrada | Sem par de vistorias não há cobrança de avaria defensável |
| **RN-58** | A devolução grava `Devolvida`, nunca `Finalizada` | Devolução é vistoria; o contrato morre no fechamento |
| **RN-59** | `Cancelada` é estado próprio e só é alcançável a partir de `Criada` | Carro que rodou se devolve, não se cancela |
| **RN-60** | `Atrasada` aceita devolução e volta ao fluxo normal; o atraso conta por instante, não por data | Era estado sem saída, e hora é dado contratual |
| **RN-61** | Só `Finalizada` e `Cancelada` liberam o período do veículo na constraint de sobreposição | Cancelamento libera período retroativo; contrato cumprido protege o histórico |
| **RN-62** | Toda transição de status de contrato grava `HistoricoStatusLocacao` com autor e instante | **Ainda não implementada** — a entidade existe e ninguém a alimenta |

### Efeitos colaterais no veículo e na reserva

```mermaid
stateDiagram-v2
    direction LR

    state "Locacao.Criar()" as Criar
    state "Locacao.Finalizar()" as Fin
    state "Locacao.Cancelar()" as Can

    [*] --> Criar
    Criar --> V1 : Veiculo.Indisponibilizar()
    Criar --> R1 : Reserva.Finalizar()<br/>se veio de uma reserva
    Fin --> V2 : Veiculo.Disponibilizar()
    Can --> V2 : Veiculo.Disponibilizar()

    state "Veiculo.Disponivel = false" as V1
    state "Veiculo.Disponivel = true" as V2
    state "Reserva.Status = Finalizado" as R1
```

---

## 2. Veículo

O veículo carrega **três indicadores independentes**. Só `Disponivel` e `Status` mudam por
comportamento; `Ativo` é uma flag administrativa.

### 2.1 Flag `Disponivel`

```mermaid
stateDiagram-v2
    direction LR

    [*] --> Disponivel : Criar()<br/>Disponivel = true

    Disponivel --> Indisponivel : Indisponibilizar()<br/>chamado por Locacao.Criar()
    Indisponivel --> Disponivel : Disponibilizar()<br/>chamado por Finalizar() e Cancelar()

    state "Disponivel = true" as Disponivel
    state "Disponivel = false" as Indisponivel
```

### 2.2 Enum `StatusVeiculo`

```mermaid
stateDiagram-v2
    direction LR

    [*] --> SemStatus : Criar()<br/>não atribui Status → fica 0

    SemStatus --> EmManutencao : IniciarManutencao(tipo, descricao)
    EmManutencao --> Disponivel : TerminaManutencao(custo, idManutencao)
    EmManutencao --> Disponivel : CancelarManutencao(idManutencao)
    EmManutencao --> Disponivel : AtualizarDescricaoManutencao(...)
    Disponivel --> EmManutencao : IniciarManutencao(tipo, descricao)

    Locado : Locado — inalcançável
    Indisponivel : Indisponivel — inalcançável

    state "valor 0, fora do enum" as SemStatus
```

Os quatro métodos de manutenção recusam a operação quando `Status == Locado`, mas nenhum
método atribui `Locado` — a guarda nunca dispara. `AtualizarDescricaoManutencao` também
devolve o veículo para `Disponivel`, mesmo sendo apenas uma edição de texto.

---

## 3. Reserva — `StatusReserva`

```mermaid
stateDiagram-v2
    direction LR

    [*] --> Reservado : Reserva.Criar(...)<br/>Ativo = true

    Reservado --> Cancelado : Cancelar()<br/>Ativo = false
    Reservado --> Finalizado : Finalizar()<br/>Ativo = false<br/>chamado por Locacao.Criar()
    Reservado --> Expirado : Expirar(agora)<br/>agora > DataInicio<br/>Ativo = false

    Cancelado --> [*]
    Finalizado --> [*]
    Expirado --> [*]
```

`Criar` valida que `inicio` e `fim` são futuros e que `fim > inicio` — comparando com
`DateTime.Now` (local), enquanto o restante do sistema trabalha em UTC.

---

## 4. Pagamento — `StatusPagamento`

```mermaid
stateDiagram-v2
    direction LR

    [*] --> Pendente : new Pagamento(valor, formaPagamento)<br/>DataPagamento = UtcNow

    Pendente --> Pago : Confirmar()<br/>atualiza DataPagamento
    Pendente --> Falhou : MarcarComoFalhou()
    Pendente --> Cancelado : Cancelar(motivo)
    Falhou --> Cancelado : Cancelar(motivo)

    Pago --> [*]
    Cancelado --> [*]
```

`Cancelar(motivo)` só recusa quando o status é `Pago`; o parâmetro `motivo` é recebido mas não
é armazenado em lugar nenhum.

---

## 5. Caução — `Caucao.StatusCaucao`

```mermaid
stateDiagram-v2
    direction LR

    [*] --> Pendente : Criar(valor)<br/>valor deve ser > 0

    Pendente --> Bloqueada : Bloquear()
    Pendente --> Devolvida : Devolver()
    Pendente --> Bloqueada : Deduzir(valor)<br/>quando o saldo zera
    Pendente --> Pendente : Deduzir(valor)<br/>saldo parcial

    Utilizada : Utilizada — inalcançável

    Bloqueada --> [*]
    Devolvida --> [*]
```

`Deduzir` subtrai de `Valor` e, se o saldo chegar a zero, muda para `Bloqueada` — não para
`Utilizada`, que nunca é atribuída.

---

## 6. Multa — `StatusMulta`

```mermaid
stateDiagram-v2
    direction LR

    [*] --> Pendente : Criar(valor, tipo)<br/>só com a locação Finalizada

    Pendente --> Paga : MarcarComoPaga()
    Pendente --> CompensadaCaucao : CompensarComCaucao()<br/>exige soma das cauções >= valor
    Pendente --> Cancelada : Cancelar()
    CompensadaCaucao --> Cancelada : Cancelar()

    Paga --> [*]
    Cancelada --> [*]
```

---

## 7. Dano — `StatusDano`

```mermaid
stateDiagram-v2
    direction LR

    [*] --> Registrado : Criar(...)<br/>via Vistoria.RegistrarDano()<br/>só em vistoria de Devolucao

    Registrado --> EmAnalise : ColocarEmAnalise()
    Registrado --> Aprovado : Aprovar()
    Aprovado --> Cobrado : MarcarComoCobrado()
    Cobrado --> Pago : MarcarComoPago()

    Registrado --> Isento : Isentar()
    Aprovado --> Isento : Isentar()
    Cobrado --> Isento : Isentar()

    Registrado --> Cancelado : Cancelar()
    Aprovado --> Cancelado : Cancelar()
    Cobrado --> Cancelado : Cancelar()

    Pago --> [*]
    Isento --> [*]
    Cancelado --> [*]
```

`EmAnalise` e `Cancelado` compartilham o valor `6` no enum. Depois de persistidos são o mesmo
número — e, como `Aprovar()` exige status `Registrado`, um dano colocado em análise não tem
como avançar.

`AtualizarValor(novoValor)` é recusado quando o status é `Pago` ou `Isento`.

---

## 8. Manutenção — `StatusManutencao`

```mermaid
stateDiagram-v2
    direction LR

    [*] --> Aberta : Criar(tipo, descricao)<br/>Custo = 0, DataInicio = UtcNow

    Aberta --> Finalizada : Encerrar(custo)<br/>DataFim = UtcNow
    Aberta --> Cancelada : Cancelar()

    EmAndamento : EmAndamento — inalcançável

    Finalizada --> [*]
    Cancelada --> [*]
```

Uma manutenção corretiva é aberta automaticamente pelo domínio quando uma vistoria registra
dano: `Locacao.RegistrarDanoVistoria` chama
`Veiculo.IniciarManutencao(TipoManutencao.Corretiva, "Manutenção gerada automaticamente por dano em vistoria")`.

---

## 9. Cliente — `StatusCliente`

```mermaid
stateDiagram-v2
    direction LR

    [*] --> Habilitado : Criar(numeroHabilitacao,<br/>validadeCnh, endereco)<br/>Ativo = true

    Habilitado --> Inadimplente : MarcarInadimplente()
    Habilitado --> Bloqueado : Bloquear()
    Inadimplente --> Habilitado : Regularizar()
    Inadimplente --> Bloqueado : Bloquear()
    Bloqueado --> Habilitado : Regularizar()

    note right of Habilitado
        PodeLocar() = true somente com
        Status == Habilitado E
        ValidadeHabilitacao >= hoje
    end note
```

`Atualizar(...)` recoloca o cliente em `Habilitado` e `Ativo = true` independentemente do
estado anterior — editar os dados de um cliente bloqueado o desbloqueia.

`Ativar()` / `Desativar()` mexem apenas na flag `Ativo` e não participam de `PodeLocar()`.

---

## 10. Visão consolidada — a jornada de uma locação

```mermaid
stateDiagram-v2
    direction TB

    [*] --> Reserva

    state Reserva {
        [*] --> Reservado
        Reservado --> Finalizado : locação aberta
        Reservado --> Cancelado
        Reservado --> Expirado
    }

    Reserva --> Retirada : Locacao.Criar()

    state Retirada {
        [*] --> LocacaoCriada
        LocacaoCriada --> VistoriaRetirada : RegistrarVistoria(Retirada)
        VistoriaRetirada --> Seguro : AdicionarSeguro()
        Seguro --> Adicionais : AdicionarAdicional()
        Adicionais --> Caucao : RegistrarCaucao()
        Caucao --> Pagamento : AdicionarPagamento()
    }

    Retirada --> Devolucao : carro volta ao balcão

    state Devolucao {
        [*] --> VistoriaDevolucao : RegistrarVistoria(Devolucao)
        VistoriaDevolucao --> PosseEncerrada : Locacao.RegistrarDevolucao()
        PosseEncerrada --> SemDano
        PosseEncerrada --> ComDano
        ComDano --> ManutencaoCorretiva : Veiculo.IniciarManutencao()
    }

    Devolucao --> Fechamento : Locacao.Fechar()

    state Fechamento {
        [*] --> ContaApurada
        ContaApurada --> Multa : AdicionarMulta()
        Multa --> CompensaCaucao : CompensarMultaComCaucao()
        ContaApurada --> DevolveCaucao : DevolverCaucao()
        ContaApurada --> Quitada : LiquidarSaldo()<br/>saldo <= 0
        ContaApurada --> SaldoResidual : LiquidarSaldo()<br/>saldo > 0
        SaldoResidual --> Quitada : ConfirmarPagamento()
    }

    Fechamento --> [*] : locação Finalizada<br/>veículo Disponivel
```

A vistoria de devolução vem **antes** do `RegistrarDevolucao`, e não depois: é ela que traz o
hodômetro e o nível do tanque, e sem ela a devolução é recusada (RN-57). A multa e a caução
moram no fechamento porque é lá que existe conta contra a qual compensar.

---

## Observações

Resumo dos estados sem transição de entrada em todo o código:

| Enum | Membro inalcançável |
|---|---|
| `StatusVeiculo` | `Locado`, `Indisponivel` |
| `StatusCaucao` | `Utilizada` |
| `StatusManutencao` | `EmAndamento` |

`StatusLocacao` saiu da lista: `EmAndamento` passou a ser atribuído pela vistoria de retirada e
`Pendente` foi removido — era a `Reserva` com outro nome, já que `Criar` compromete a placa.

E os pontos em que o comportamento diverge do nome:

- `Veiculo.AtualizarDescricaoManutencao()` também altera o `Status` do veículo.
- `Veiculo.Criar()` deixa `Status` em `0`, valor fora do intervalo do enum (que começa em `1`).
- `StatusDano.EmAnalise` e `StatusDano.Cancelado` valem ambos `6`.
- `Clientes.Atualizar()` reabilita cliente bloqueado ou inadimplente.
