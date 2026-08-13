# Montar o projeto de testes num repositório novo

Roteiro para levar esta suíte para outro projeto que usa a arquitetura da skill `arquitetura-api`. Substitua `Meu.Projeto` pelo nome real em tudo que segue.

## 1. Projeto

```powershell
dotnet new xunit -n Meu.Projeto.Tests
dotnet sln add Meu.Projeto.Tests

dotnet add Meu.Projeto.Tests reference Meu.Projeto.Domain Meu.Projeto.Application Meu.Projeto.Infra
```

A referência à **Infra** só é necessária se for testar a configuração do modelo do EF (`references/infra-e-api.md`). Sem isso, Domain + Application bastam.

`.csproj` esperado:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup>
    <!-- só se for testar a tradução de erro: DefaultHttpContext e ProblemDetails -->
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Meu.Projeto.Domain\Meu.Projeto.Domain.csproj" />
    <ProjectReference Include="..\Meu.Projeto.Application\Meu.Projeto.Application.csproj" />
    <ProjectReference Include="..\Meu.Projeto.Infra\Meu.Projeto.Infra.csproj" />
  </ItemGroup>

</Project>
```

`ImplicitUsings` e `Nullable` habilitados: os arquivos de `assets/` contam com os dois.

**Nada de biblioteca de mock.** A lista de pacotes acima é a lista completa e é assim de propósito — o porquê está no `SKILL.md`.

## 2. Pastas

```
Meu.Projeto.Tests/
  Dominio/        um arquivo por entidade      → XxxTests.cs
  Servicos/       um arquivo por serviço       → XxxServiceTests.cs
  Consultas/      paginação, ordenação, mapper
  Infra/          configuração do modelo do EF
  Api/            tradução de erro em ProblemDetails
  Fakes/          ArmazemFake, RepositorioFake, RepositoriosFake
  Fabricas/       Fabrica
  Assercoes/      opcional
```

Namespace acompanhando a pasta: `Meu.Projeto.Tests.Servicos`.

## 3. Copiar os arquivos base

```
assets/Fakes/       →  Meu.Projeto.Tests/Fakes/
assets/Fabricas/    →  Meu.Projeto.Tests/Fabricas/
assets/Assercoes/   →  Meu.Projeto.Tests/Assercoes/   (opcional)
```

Troque `{{RootNamespace}}` por `Meu.Projeto` em todos eles (busca e substituição no editor resolve).

Se o projeto não usa o notificador da `arquitetura-api`, não copie `Assercoes/`.

## 4. Ajustar o fake ao contrato real

`RepositorioFake<T>` implementa `IRepositorioGlobal<T>` **como ele está em `arquitetura-api/assets/`**. Se o seu projeto divergiu — e projetos divergem, normalmente em `ObterPorIdAsync` (`bool` vs `bool?`, retorno anulável ou não) — o compilador acusa "não implementa o membro da interface". Corrija a assinatura no fake para bater com a sua interface; o corpo continua o mesmo.

Mesma coisa para métodos que a sua interface tenha a mais: implemente em memória, com a mesma semântica do repositório real.

## 5. Fakes tipados

Um por repositório que os serviços pedem, em `Fakes/RepositoriosFake.cs`:

```csharp
public class ClienteRepositoryFake : RepositorioFake<Cliente>, IClienteRepository
{
    public ClienteRepositoryFake(ArmazemFake? armazem = null) : base(armazem) { }
}
```

Se alguma raiz de agregado cria filhos que o serviço consulta depois, ela precisa do `SalvarAsync` sobrescrito propagando o cascade — o exemplo completo está em `references/servicos.md`.

## 6. Fábrica

`assets/Fabricas/Fabrica.cs` vem com os helpers genéricos (`DaquiADias`, `DiasAtras`, `DefinirId`) e um modelo comentado. Escreva um método por entidade, com padrões que passam em `Criar`.

## 7. Primeiro teste

Comece por uma entidade — não depende de nada e valida que a montagem está de pé:

```csharp
[Fact]
public void Criar_nasce_ativo()
{
    var cliente = Fabrica.Cliente();

    Assert.True(cliente.Ativo);
}
```

Depois um serviço, que é o que exercita fake + armazém + notificador juntos.

## 8. Rodar

```powershell
dotnet test Meu.Projeto.Tests\Meu.Projeto.Tests.csproj --nologo
```

Uma classe: `--filter "FullyQualifiedName~ClienteServiceTests"`.

## Paralelismo

O xUnit executa **coleções de teste em paralelo** (por padrão, uma coleção por classe). Duas consequências:

- nada de estado estático mutável compartilhado — armazém, contador, cache de fábrica. Cada teste monta o próprio cenário;
- a ordem de execução não é garantida e não deve importar. Teste que só passa depois de outro é teste quebrado.

Se algum dia for preciso serializar (recurso externo, por exemplo), aí sim `[Collection("nome")]` — e não antes.
