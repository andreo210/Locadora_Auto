# Persistência (Infra)

A Infra é a única camada que conhece EF Core e o banco. Ela implementa as interfaces declaradas no Domain e não é referenciada diretamente pela Application — a ligação acontece via injeção de dependência no startup.

Índice: [RepositorioGlobal](#repositorioglobal) · [Consultas](#consultas) · [Escritas](#escritas) · [Configuração de entidade](#configuração-de-entidade-ientitytypeconfiguration) · [DbContext](#dbcontext) · [UnitOfWork](#unitofwork--transação) · [Migrations](#migrations)

## RepositorioGlobal

`RepositorioGlobal<TEntity>` (Infra) implementa `IRepositorioGlobal<TEntity>` (Domain). O repositório concreto herda os dois:

```csharp
public class AdicionalRepository : RepositorioGlobal<Adicional>, IAdicionalRepository
{
    public AdicionalRepository(LocadoraDbContext ctx) : base(ctx) { }
}
```

Três parâmetros aparecem em quase todo método de consulta:

| Parâmetro | Tipo | Para quê |
|---|---|---|
| `filtro` | `Expression<Func<T, bool>>` | vira `WHERE` no banco |
| `ordenarPor` | `Func<IQueryable<T>, IOrderedQueryable<T>>` | `ORDER BY`; permite montar a ordenação dinamicamente |
| `incluir` | `Func<IQueryable<T>, IQueryable<T>>` | `Include`/`ThenInclude` encadeados |

Todos aceitam `CancellationToken ct` — propague sempre o token que chegou no controller.

### Consultas

```csharp
Task<IReadOnlyList<T>> ObterAsync(filtro?, ordenarPor?, incluir?, rastreado = false, ct)
Task<T?>               ObterPrimeiroAsync(filtro, incluir?, rastreado = false, ct)
Task<T>                ObterPorIdAsync(object id, rastreado = false, ct)
Task<bool>             ExisteAsync(filtro, ct)
Task<int>              ContarAsync(filtro?, ct)
IQueryable<T>          ObterTodos()                       // sem tracking; use com parcimônia

Task<IReadOnlyList<T>> ObterPaginadoAsync(filtro, pagina, itensPorPagina, ordenarPor?, ct)

Task<PaginatedResult<T>> ObterPaginadoComFiltroAsync(
        filtro?, ordenarPor?, incluir?,
        pagina?, itensPorPagina?,          // ambos nulos → traz tudo, sem paginar
        asNoTracking = true, asSplitQuery = false, ct)

Task<IReadOnlyList<T>> ObterComFiltroAsync(filtro?, ordenarPor?, incluir?, asNoTracking, asSplitQuery, ct)

Task<IReadOnlyList<TResult>> ObterComFiltroEProjecaoAsync<T, TResult>(
        projecao, filtro?, ordenarPor?, incluir?, asNoTracking, asSplitQuery, ct)
```

Notas de uso:

- **Tracking.** A leitura é `AsNoTracking` por padrão, porque a maioria das consultas só alimenta um DTO. Para carregar algo que você vai **alterar**, passe `rastreado: true` — senão o `SaveChangesAsync` não vê mudança nenhuma.
- **Ordenação obrigatória ao paginar.** `ObterPaginadoComFiltroAsync` descobre a chave primária por reflexão e ordena por ela quando você não passa `ordenarPor` — sem `ORDER BY`, `Skip/Take` no Postgres devolve páginas com itens repetidos ou faltando. Prefira passar a ordenação explicitamente.
- **`asSplitQuery: true`** quando houver mais de um `Include` de coleção; sem isso o EF faz produto cartesiano e a consulta explode em linhas.
- O `incluir` monta `Include`/`ThenInclude`, que são métodos de extensão do EF: o arquivo do **serviço** precisa de `using Microsoft.EntityFrameworkCore;`. É a única razão pela qual a Application referencia o EF Core.
- **Projeção** (`ObterComFiltroEProjecaoAsync`) aplica o `Select` antes de materializar — use quando a tela precisa de três colunas de uma entidade com vinte.
- Os métodos genéricos com `<TConsulta>` conseguem consultar **outra** entidade pelo mesmo contexto (`Context.Set<TConsulta>()`). Na prática o tipo é inferido do `filtro`, então a chamada fica igual às outras.

Exemplo real, com filtro composto e ordenação vinda da tela:

```csharp
// TermoNormalizado já vem aparado e em minúsculas: o LIKE do Postgres é sensível a maiúsculas
var busca = consulta.TermoNormalizado;

Expression<Func<Reserva, bool>> filtro = r =>
    (busca == null || r.Filial.Nome.ToLower().Contains(busca))
    && (idFilial == null || r.IdFilial == idFilial);

var pagina = await _reservaRepository.ObterPaginadoComFiltroAsync(
    filtro: filtro,
    ordenarPor: Ordenacoes.Montar(consulta),
    incluir: q => q.Include(r => r.Cliente).ThenInclude(c => c.Usuario)
                   .Include(r => r.Filial),
    pagina: consulta.Pagina,
    itensPorPagina: consulta.ItensPorPagina,
    ct: ct);

return pagina.ParaDto(ReservaMapper.ToDtoList);
```

`consulta` é o `ConsultaPaginadaRequest` e `Ordenacoes` é o mapa `OrdenacaoDeConsulta<T>` declarado uma vez como `static readonly` no serviço — ambos descritos em `aplicacao-api.md`.

### Escritas

```csharp
Task<T>       InserirSalvarAsync(entidade, ct)          // Add + SaveChanges
Task<List<T>> InserirSalvarListasAsync(entidades, ct)
Task          InserirAsync(entidade, ct)                // Add sem salvar
Task<bool>    AtualizarSalvarAsync(entidade, ct)        // true se algo mudou no banco
void          Atualizar(entidade)                       // marca sem salvar
Task          ExcluirSalvarAsync(entidade, ct)
Task          Excluir(entidade, ct)                     // marca sem salvar
Task<int>     SalvarAsync(ct)
```

O par "faz e salva" / "só marca" existe para o caso de várias escritas na mesma unidade de trabalho: use as versões sem `Salvar` e feche com `SalvarAsync` (ou com o `UnitOfWork`, se precisar de transação explícita).

`AtualizarSalvarAsync` funciona nos dois cenários — se a entidade já está rastreada ele copia os valores atuais; se chegou desanexada, anexa e marca como `Modified`, o que sobrescreve **todas** as colunas. Carregar com `rastreado: true` antes de alterar evita apagar campo que você não leu.

## Configuração de entidade (`IEntityTypeConfiguration`)

Um arquivo por entidade em `Data/Configuracao/`, aplicado automaticamente por `ApplyConfigurationsFromAssembly` — não precisa registrar nada:

```csharp
public class AdicionalConfig : IEntityTypeConfiguration<Adicional>
{
    public void Configure(EntityTypeBuilder<Adicional> builder)
    {
        builder.ToTable("tb_adicional");

        builder.HasKey(e => e.IdAdicional);
        builder.Property(e => e.IdAdicional).HasColumnName("id_adicional");

        builder.Property(e => e.Nome)
            .HasColumnName("nome").HasMaxLength(50).IsRequired();

        builder.Property(e => e.ValorDiaria)
            .HasColumnName("valor_diaria").HasPrecision(10, 2).IsRequired();
    }
}
```

Por que aqui e não com atributos na entidade: mapeamento é decisão de infraestrutura. Deixar `[Table]`/`[Column]` na entidade acopla o domínio ao banco e quebra a regra de "Domain sem dependências".

Convenções que importam:
- tabelas e colunas em **snake_case**. Vem de `UseSnakeCaseNamingConvention()` **somada** aos `ToTable`/`HasColumnName` explícitos: a convenção só nomeia o que não foi configurado à mão, então nome escrito à mão já tem que vir em snake_case.
- `decimal` sempre com `HasPrecision` — sem isso o Postgres cria `numeric` sem escala.
- enum persistido como int: `builder.Property(e => e.Status).HasConversion<int>()`.

## DbContext

O contexto sobrescreve **apenas o `SaveChangesAsync`** e faz três coisas antes de delegar para a base:

```csharp
public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
{
    AplicarAuditoria();          // preenche IAuditoria com data + ICurrentUser.UserId ?? "SYSTEM"
    CriarHistoricoTemporal();    // gera o registro de ITemporalEntity<> antes do UPDATE/DELETE
    return await base.SaveChangesAsync(ct);
}
```

Consequência que vale repetir: o `SaveChanges()` **síncrono não passa por aqui**. Toda escrita usa a via assíncrona.

No `OnModelCreating`, além de aplicar as configurações do assembly, um `ValueConverter` global normaliza `DateTime`/`DateTime?` para `Kind=Utc` na escrita e remarca como UTC na leitura — sem ele, data vinda de DTO chega como `Unspecified` e o Npgsql rejeita. O conversor salva a gravação, mas **não** conserta comparação em memória: continua valendo `DateTime.UtcNow`.

Se o contexto herda de `IdentityDbContext`, o loop do conversor precisa rodar **depois** do `base.OnModelCreating` para alcançar também as tabelas do Identity.

A implementação genérica desses três blocos está em `assets/Infra/AuditoriaExtensions.cs`, como métodos de extensão — assim funciona tanto num `DbContext` puro quanto num `IdentityDbContext`, que já gastou a herança.

### ICurrentUser

Abstrai "quem está logado" para o contexto conseguir auditar sem conhecer HTTP. Registrado como scoped. Retorna `null` fora de uma requisição (job, seed, design time) — daí o `?? "SYSTEM"` na auditoria.

Isso também explica a existência de um `IDesignTimeDbContextFactory`: o `CurrentUser` real depende de `HttpContext` e quebraria ao rodar `dotnet ef` na linha de comando.

## UnitOfWork / transação

Cada `InserirSalvarAsync`/`AtualizarSalvarAsync` já é uma transação implícita do EF. O `IUnitOfWork` é para quando **duas ou mais escritas precisam cair ou passar juntas**:

```csharp
await _unitOfWork.ExecuteTransactionAsync(async () =>
{
    await _locacaoRepository.InserirAsync(locacao, ct);   // sem salvar
    veiculo.Indisponibilizar();
    _veiculoRepository.Atualizar(veiculo);                // sem salvar
    return locacao;
}, ct);
```

`ExecuteTransactionAsync` abre, chama `SaveChangesAsync`, faz commit e, em caso de exceção, rollback + rethrow. Use as versões "só marca" dos métodos dentro do bloco: salvar no meio derrota o propósito.

`BeginTransactionAsync`/`CommitAsync`/`RollbackAsync` existem para controle manual, mas prefira o `ExecuteTransactionAsync` — ele não deixa transação aberta em caminho de erro.

## Concorrência otimista

Sem token de concorrência, dois usuários editando o mesmo registro terminam com o último salvando por cima — sem erro, sem aviso. No Postgres o conserto não custa coluna nova: `xmin` é coluna de sistema que já existe em toda tabela e muda a cada `UPDATE`.

```csharp
builder.Entity(tipo).UseXminAsConcurrencyToken();
```

O `UPDATE` passa a levar `WHERE ... AND xmin = @original`; se ninguém tocou na linha, uma linha é afetada e segue o jogo. Se alguém gravou no meio do caminho, zero linhas, e o EF lança `DbUpdateConcurrencyException`.

Aplique em bloco no `OnModelCreating`, como o conversor de UTC, pulando: tipos owned (gravam junto da raiz), entidades do Identity (já têm `ConcurrencyStamp`) e histórico temporal (só recebe insert). A implementação está em `assets/Infra/AuditoriaExtensions.cs`.

**Onde isso morde de verdade**, e vale saber antes de confiar demais:

- **Protege** quando a entidade chega rastreada da leitura original — o caminho `rastreado: true`. O `xmin` que o contexto guarda é o do momento em que o serviço leu, então o conflito aparece.
- **Não acusa nada** quando a entidade foi lida sem rastreio: o `AtualizarSalvarAsync` faz `FindAsync` antes de gravar, relê a linha e compara o token com o valor recém-lido. Não gera falso positivo, mas também não protege — é mais um motivo para carregar com `rastreado: true` antes de alterar.
- **Não cobre** o caso "usuário abriu a tela há cinco minutos": nesse intervalo não existe contexto vivo. Para isso a versão precisa ir e voltar pelo HTTP (campo no DTO ou `ETag`/`If-Match`), e o serviço compara na entrada.

### A migration é vazia — e tem que ser

O gerador do EF não sabe que `xmin` é coluna de sistema e produz um `AddColumn` por tabela. Esse script **falha** no Postgres com *"column name xmin conflicts with a system column name"*. Esvazie o `Up`/`Down` da migration deixando um comentário explicando: o banco não muda, o que mudou foi só o modelo, e a migration existe para o snapshot registrar isso e as próximas não gerarem tudo de novo.

### O conflito vira 409, não 500

`DbUpdateConcurrencyException` é condição esperada, não defeito. O tratamento fica num lugar só, no `ExceptionProblemFactory`, porque vale para qualquer entidade:

```csharp
if (exception is DbUpdateConcurrencyException)
    return ProblemFactory.Create(HttpStatusCode.Conflict,
        "O registro foi alterado por outro usuário enquanto você editava. Recarregue a tela e refaça a operação.");
```

Serviço que queira reagir de outro jeito (recarregar e reaplicar a alteração) captura a exceção antes e notifica normalmente.

## Migrations

O **modelo é a fonte de verdade**; o schema sai das migrations. Ao mexer em entidade ou em `*Config.cs`, gere a migration na mesma mudança:

```powershell
dotnet ef migrations add <Nome> --project <Projeto.Infra> --startup-project <Projeto.Api> --output-dir Data/Migrations
```

Migration é código versionado: revise o arquivo gerado antes de commitar, principalmente quando envolver `DROP`/`ALTER` de coluna com dado.
