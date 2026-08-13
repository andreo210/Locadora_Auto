---
name: testes
description: Como escrever e rodar os testes desta arquitetura (.NET + xUnit, sem biblioteca de mock) — `RepositorioFake`/`ArmazemFake` no lugar do banco, `NotificadorService` real como asserção de regra de negócio, fábrica de entidades válidas e teste do modelo do EF sem conexão. Use ao criar ou alterar teste de entidade, serviço, consulta paginada ou resposta de erro; ao decidir o que testar de uma funcionalidade nova; ao investigar teste quebrado ou "gravei e o armazém não mudou"; ao rodar a suíte (`dotnet test`); ou ao montar o projeto de testes num repositório novo (os arquivos base genéricos estão em `assets/`). Vale também para perguntas do tipo "como testo isso sem banco", "por que não usar Moq aqui", "como verifico que o serviço notificou em vez de lançar", "por que o `incluir` não funciona no teste" e "isso é teste de unidade ou de integração".
---

# Testes

Esta skill descreve como se testa a arquitetura descrita em `arquitetura-api` — vale para o `Locadora_Auto` e para qualquer projeto que reuse a mesma espinha. Os arquivos genéricos (fake de repositório, armazém em memória, fábrica, asserções) estão em `assets/` prontos para copiar (veja `references/bootstrap.md`).

Escopo: **teste de unidade sobre Domain e Application**, mais dois vizinhos que rodam sem infraestrutura — a configuração do modelo do EF e a tradução de exceção em `ProblemDetails`. Teste de integração com banco de verdade não existe aqui; `references/infra-e-api.md` delimita o que fica de fora.

Código, nomes de teste e comentários em **português**.

## O que cada camada pede

| Camada | Como se testa | Pasta |
|---|---|---|
| Entidade / agregado | direto, sem fake nenhum — só `new`/`Criar` e os métodos de transição | `Dominio/` |
| Serviço | `RepositorioFake` + `NotificadorService` real | `Servicos/` |
| Consulta paginada, ordenação, mapper | objeto puro, sem repositório | `Consultas/` |
| Configuração do EF (token de concorrência, colunas) | modelo do `DbContext`, sem abrir conexão | `Infra/` |
| Tradução de erro em `ProblemDetails` | `DefaultHttpContext` + a factory de problema | `Api/` |
| Controller, middleware, SQL de verdade | **não se testa aqui** — exigiria host/banco | — |

Infra de teste em `Fakes/` e `Fabricas/`. Um arquivo de teste por classe testada, nomeado `<ClasseTestada>Tests.cs`.

## As quatro decisões que definem a suíte

### 1. Nenhuma biblioteca de mock

O projeto de teste referencia `xunit` e mais nada. O dublê de repositório é uma classe escrita à mão (`RepositorioFake<T>`), e o motivo é a forma do contrato: `IRepositorioGlobal<T>` recebe `Expression<Func<T,bool>>` e `Func<IQueryable<T>, IOrderedQueryable<T>>`. Programar isso num mock exige devolver lista fixa por chamada — o teste passa a afirmar "o serviço chamou `ObterPrimeiroAsync`" em vez de "o serviço encontrou o cliente certo", e qualquer troca de filtro equivalente quebra o teste sem nada ter quebrado de fato.

O fake executa o filtro de verdade em memória, com LINQ to Objects. O teste então descreve **comportamento**, não sequência de chamadas.

### 2. A asserção de regra de negócio é o notificador — e ele é o real

Serviço não lança exceção para regra de negócio, ele chama `_notificador.Add(...)`. Quem decide entre 2xx e `ProblemDetails` é o `CustomResponse` consultando essa mesma instância. Logo, `notificador.TemNotificacao()` no teste é literalmente o que vai acontecer na Api — por isso se usa o `NotificadorService` de produção, não um dublê.

```csharp
Assert.Null(resultado);                        // o serviço recusou
Assert.True(notificador.TemNotificacao());     // e reportou o motivo
Assert.Equal(0, repositorio.Salvamentos);      // e não gravou nada
```

Esses três asserts juntos são o formato padrão do caso de falha. Sozinho, o primeiro não distingue "recusou com mensagem" de "engoliu o erro e devolveu nulo".

Quando a mensagem importa, verifique o trecho em vez do texto inteiro — assim reescrever a frase não quebra o teste:

```csharp
Assert.Contains(notificador.ObterNotificacoes(), n => n.Mensagem.Contains("inativo"));
```

### 3. Entidade válida sai da fábrica

`Fabrica` (`Fabricas/`) devolve entidade que passa em todas as validações de `Criar`, com parâmetro opcional só para o que os testes precisam variar (`Fabrica.Cliente(ativo: false)`). Nenhum teste monta entidade à mão.

O ganho aparece quando entra validação nova na entidade: corrige-se a fábrica, e não trinta chamadas espalhadas. Duas regras que a fábrica carrega:

- **data futura vem de `Fabrica.DaquiADias(n)`**, nunca literal — as entidades comparam com `DateTime.UtcNow` e um literal envelhece;
- **filho de agregado nasce pela raiz** (`Fabrica.ClienteComReserva()` chama `cliente.ReservarVeiculo(...)`), porque `Reserva.Criar` é `internal` de propósito. Testar pela porta que a aplicação usa é o que mantém a invariante honesta.

### 4. O que se verifica é `Salvamentos`, não o conteúdo do armazém

O fake não tem change tracking: as entidades no armazém são as mesmas instâncias que o teste criou, então alterar a entidade já "persiste" mesmo sem gravar. Verificar o armazém daria verde num serviço que esqueceu o `SalvarAsync`.

`RepositorioFake.Salvamentos` conta quantas vezes o serviço mandou gravar — é essa a asserção de "chegou a persistir". `Assert.Equal(0, ...)` no caminho de falha, `Assert.Equal(1, ...)` no de sucesso.

## Anatomia de um teste de serviço

Um helper `Montar(...)` por classe de teste, devolvendo o que os asserts vão precisar. Ele concentra a construção do cenário; o corpo do teste fica com três blocos visíveis (arranjo, ação, verificação) sem precisar de comentário `// Arrange`.

```csharp
private static (AdicionalService service, AdicionalRepositoryFake repositorio, NotificadorService notificador)
    Montar(params Adicional[] jaCadastrados)
{
    var armazem = new ArmazemFake().Semear(jaCadastrados);
    var repositorio = new AdicionalRepositoryFake(armazem);
    var notificador = new NotificadorService();

    return (new AdicionalService(repositorio, notificador), repositorio, notificador);
}

[Fact]
public async Task Criar_com_nome_repetido_notifica_e_nao_grava()
{
    var (service, repositorio, notificador) = Montar(Fabrica.Adicional("Cadeirinha"));

    var resultado = await service.CriarAsync(new CriarAtualizarAdicionalDto
    {
        Nome = "Cadeirinha",
        ValorDiaria = 30m
    });

    Assert.Null(resultado);
    Assert.True(notificador.TemNotificacao());
    Assert.Equal(0, repositorio.Salvamentos);
}
```

Quando o serviço depende de vários repositórios, **um único `ArmazemFake` vai para todos** — é o que faz o serviço de reserva enxergar o cliente que o de veículo semeou. Com mais de três dependências, troque a tupla por uma classe `Cenario` com `required init`. O formato completo está em `references/servicos.md`.

## Nome do teste

`Acao_condicao_resultado`, em português, com underscore:

```
Criar_com_nome_repetido_notifica_e_nao_grava
Cancelar_duas_vezes_notifica_em_vez_de_estourar_a_excecao_do_dominio
Expirar_no_mesmo_dia_do_inicio_nao_expira
ItensPorPagina_fica_dentro_do_limite
```

O nome tem que dizer a regra, porque é ele que aparece no relatório de falha. `Teste1`, `CriarAsync_Deve_Funcionar` e `Test_Create_Ok` não dizem nada.

Use `[Theory]` + `[InlineData]` quando a regra é a mesma e só o valor muda (limites, normalização); `[Fact]` quando o cenário é outro.

## O que o fake não cobre

Limites conscientes — se o teste depende de um destes, ele está afirmando algo que o fake não sabe responder:

- **`incluir` é ignorado.** `Include`/`ThenInclude` só existem sobre provider do EF. Em memória a navegação já vem montada pelo objeto que o teste criou: se o serviço lê `reserva.Cliente`, o teste preenche isso ao construir a entidade. Um serviço que esqueceu o `incluir` **passa** no teste e quebra em produção com navegação nula.
- **Tradução de `Expression` para SQL.** O filtro roda em LINQ to Objects, que aceita coisas que o Npgsql recusa (chamada de método próprio, por exemplo). Passar aqui não garante que a consulta traduz.
- **Tracking e `AsNoTracking`.** O fake não tem estados de entidade; `rastreado: true` não muda nada.
- **Auditoria, histórico temporal e token de concorrência.** Tudo isso mora no `SaveChangesAsync` sobrescrito, que o fake não executa.

Cobrir isso exige teste de integração com banco — que ainda não existe no repositório. Não finja que existe escrevendo asserção sobre comportamento de EF em cima do fake.

## Rodar

```powershell
dotnet test Locadora_Auto.Tests\Locadora_Auto.Tests.csproj --nologo
```

Hoje: **105 testes, 0 falhas**. Os warnings do build são pré-existentes (~333) e não são regressão.

Uma classe só, enquanto se trabalha nela:

```powershell
dotnet test Locadora_Auto.Tests\Locadora_Auto.Tests.csproj --nologo --filter "FullyQualifiedName~ReservaServiceTests"
```

Um teste só: `--filter "FullyQualifiedName~ReservaServiceTests.Cancelar_reserva_ativa_encerra_e_grava"`.

## Roteiro para funcionalidade nova

Depois de atravessar as camadas (skill `nova-entidade`), o mínimo que entra em teste:

1. **Entidade** — `Criar` válido; cada validação de `Criar` recusando; cada transição de estado; cada transição inválida (`Cancelar` duas vezes, `Finalizar` o que já finalizou).
2. **Fábrica** — um método novo para a entidade, com os padrões que passam em `Criar`.
3. **Fake tipado** — três linhas em `Fakes/RepositoriosFake.cs` amarrando `RepositorioFake<T>` à interface concreta.
4. **Serviço** — o caminho feliz (grava e devolve DTO) e uma falha por regra de negócio (notifica e não grava). Se houver listagem paginada, um teste de metadados da página.
5. Rodar a suíte inteira, não só a classe nova.

Regra de corte: cada `_notificador.Add(...)` do serviço e cada `throw` da entidade merece um teste. É o que separa "a regra existe no código" de "a regra funciona".

## Anti-padrões

- introduzir Moq/NSubstitute para dublar `IRepositorioGlobal<T>` — o fake já executa o filtro de verdade
- dublar o `INotificadorService`: ele é o objeto sob teste, não uma dependência
- afirmar só `Assert.Null(resultado)` num caso de falha, sem conferir notificação e `Salvamentos`
- verificar o armazém para provar que gravou (não prova — não há tracking)
- entidade construída à mão no teste, em vez da fábrica
- `DateTime.Now` ou data literal em teste que compara com `UtcNow`
- comparar a mensagem de notificação inteira, em vez de um trecho
- teste que depende de `incluir` ter carregado navegação
- `Thread.Sleep` para "esperar" algo assíncrono
- estado compartilhado entre testes (campo estático mutável, armazém em `static`) — xUnit roda coleções em paralelo
- teste que só repete a implementação linha a linha, sem nomear uma regra

## Referências

Leia sob demanda:

- `references/dominio.md` — testar entidade, agregado, transição de estado, invariante e data.
- `references/servicos.md` — cenário com fakes, notificador, armazém compartilhado, cascade de agregado, paginação.
- `references/infra-e-api.md` — modelo do EF sem banco, `ProblemDetails`, e a fronteira do teste de integração.
- `references/bootstrap.md` — montar o projeto de testes num repositório novo a partir de `assets/`.

## Arquivos base

`assets/` traz a infra de teste genérica, com o namespace como `{{RootNamespace}}` para troca em massa:

```
assets/Fakes/ArmazemFake.cs        armazém em memória + descoberta de chave primária
assets/Fakes/RepositorioFake.cs    IRepositorioGlobal<T> em memória, com contador de gravações
assets/Fabricas/Fabrica.cs         esqueleto da fábrica, com os helpers de data e id
assets/Assercoes/AssercoesDeNotificacao.cs   DeveNotificar/NaoDeveNotificar com mensagem de falha útil
```

Duas diferenças em relação ao que está no `Locadora_Auto`, porque a versão de `assets/` é a portátil: a chave primária aceita `int`, `long` e `Guid` (a do repositório é só `int`), e o cache de reflexão é `ConcurrentDictionary` (xUnit executa coleções em paralelo). As asserções de notificação são adição desta skill — os testes do repositório afirmam direto com `Assert.True(notificador.TemNotificacao())`, o que continua correto.

Em projeto existente, **não** troque o arquivo em uso pelo de `assets/` sem pedido explícito.
