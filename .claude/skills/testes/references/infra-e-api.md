# Testar Infra e Api sem infraestrutura

Duas coisas fora de Domain e Application rodam num teste de unidade comum, porque nenhuma das duas precisa de conexão nem de host: **a configuração do modelo do EF** e **a tradução de exceção em `ProblemDetails`**.

## Modelo do EF sem banco

Construir o modelo não abre conexão. `DbContextOptionsBuilder.UseNpgsql("...")` só registra o provider — a string pode apontar para um banco que não existe, desde que o teste nunca consulte.

```csharp
private sealed class UsuarioFake : ICurrentUser
{
    public string? UserId => "teste";
    public bool IsAuthenticated => true;
}

private static LocadoraDbContext MontarContexto()
{
    var opcoes = new DbContextOptionsBuilder<LocadoraDbContext>()
        // string de conexão nunca usada: construir o modelo não abre conexão
        .UseNpgsql("Host=localhost;Database=modelo_para_teste;Username=postgres;Password=postgres")
        .UseSnakeCaseNamingConvention()
        .Options;

    return new LocadoraDbContext(opcoes, new UsuarioFake());
}
```

O `ICurrentUser` fake existe porque o real depende de `HttpContext` — a mesma razão do `LocadoraDbContextFactory` de design time.

**Importante:** o `UseSnakeCaseNamingConvention()` (e qualquer outra opção que afete o modelo) precisa estar aqui igual ao `Program.cs`. Se divergir, o teste valida um modelo que não é o da aplicação.

### O que se afirma sobre o modelo

Tudo que é decisão de mapeamento — e portanto tudo que uma migration errada quebraria em silêncio:

```csharp
var xmin = contexto.Model.FindEntityType(typeof(Veiculo))?.FindProperty("xmin");

Assert.NotNull(xmin);
Assert.True(xmin!.IsConcurrencyToken);
Assert.Equal(ValueGenerated.OnAddOrUpdate, xmin.ValueGenerated);
Assert.Equal("xmin", xmin.GetColumnName());   // aponta para a coluna de sistema, não cria outra
Assert.Equal("xid", xmin.GetColumnType());
```

Serve para: token de concorrência ligado onde deve **e desligado onde não faz sentido** (histórico temporal só recebe insert; Identity já tem `ConcurrencyStamp`), nome e tipo de coluna, conversores de valor, propriedade ignorada, filtro global, tipo owned.

`[Theory]` com `[InlineData(typeof(Xxx))]` por entidade é o formato natural — entidade nova entra como uma linha e o teste passa a cobri-la.

O que esses testes **não** provam: que o `UPDATE ... WHERE xmin = @token` acontece. Isso é comportamento do Postgres, não do modelo.

## Resposta de erro

`ExceptionProblemFactory` e `ProblemFactory` são estáticas e recebem o `HttpContext` — um `DefaultHttpContext` basta:

```csharp
private static HttpContext Contexto()
{
    var contexto = new DefaultHttpContext();
    contexto.Request.Path = "/api/v1/veiculos/7";
    return contexto;
}

[Fact]
public void Conflito_de_concorrencia_vira_409_e_nao_500()
{
    var problem = ExceptionProblemFactory.Create(
        Contexto(),
        new DbUpdateConcurrencyException("A linha foi alterada por outra transação"));

    Assert.Equal((int)HttpStatusCode.Conflict, problem.Status);
    Assert.Contains("outro usuário", problem.Detail);
}
```

O que vale fixar aqui:

- **cada exceção conhecida no seu status** — `DbUpdateConcurrencyException` → 409, `ProblemException` preservando o próprio status, o resto → 500. É o que permite o cliente distinguir "tente de novo" de "deu ruim no servidor";
- **a mensagem interna não vaza** — `Assert.DoesNotContain("row(s)", problem.Detail)`. Texto do EF em tela é ruído para o usuário e informação a mais para quem está sondando a Api;
- **`Instance` e `traceId` sempre presentes** — sem eles não dá para casar a reclamação com o log;
- **`ProblemFactory` recusa status de sucesso** — `Assert.Throws<ArgumentOutOfRangeException>(() => ProblemFactory.Create(HttpStatusCode.OK, "ok"))`. `ProblemDetails` só existe para erro; 200 ali é bug de quem chamou.

Isso exige o framework do ASP.NET no projeto de teste:

```xml
<ItemGroup>
  <!-- DefaultHttpContext e ProblemDetails nos testes de resposta de erro -->
  <FrameworkReference Include="Microsoft.AspNetCore.App" />
</ItemGroup>
```

## A fronteira

Fica de fora do que se testa aqui, e **não** dá para simular com fake:

| Não coberto | Por quê |
|---|---|
| tradução de `Expression` para SQL | o fake roda LINQ to Objects, que aceita mais coisas que o Npgsql |
| tracking, `AsNoTracking`, `Include` | não existe change tracker em memória |
| auditoria e histórico temporal | moram no `SaveChangesAsync` sobrescrito |
| conflito de `xmin` de verdade | é o Postgres comparando a coluna de sistema |
| `LIKE` sensível a maiúsculas/acentos | comportamento do collation do banco |
| migrations aplicando | precisa de banco |
| controller, rota, versionamento, middleware | precisa de host HTTP |

Cobrir isso é teste de **integração**: subir Postgres (Testcontainers ou banco local), aplicar as migrations e rodar contra ele; e `WebApplicationFactory<Program>` para o caminho HTTP. Nada disso existe neste repositório hoje.

Enquanto não existir, a regra é não fingir: teste que precisaria de banco para significar alguma coisa não deve ser escrito em cima do fake dando a impressão de que a área está coberta. Se a dúvida é "isso traduz para SQL?", a resposta vem de rodar a aplicação, não de um `Assert`.
