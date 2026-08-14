---
name: rodar-app
description: Sobe a Api e o Front do Locadora_Auto fora do Visual Studio (dotnet run) e dirige a aplicação no navegador. Cobre as três armadilhas que quebram esse caminho — porta da Api divergente no Front, TLS do PowerShell 5.1 e dependência do PostgreSQL. Use ao rodar, testar ou tirar screenshot da aplicação real.
---

# Rodar a aplicação

O caminho normal do André é o Visual Studio (F5), que usa **IIS Express**. Por `dotnet run` o host é o **Kestrel**, e três coisas quebram. Este documento é o caminho verificado.

## Armadilhas (leia antes de rodar)

1. **A porta da Api no Front está errada para Kestrel.** `Locadora_Auto.Front/appsettings.Development.json` traz `ApiConfig:BaseUrlApiLocacao = https://localhost:44310/`, que é o `sslPort` do **IIS Express** em `Locadora_Auto.Api/Properties/launchSettings.json`. No Kestrel a Api sobe em `61977`/`61978` e o Front fala sozinho, sem erro visível — as telas só ficam vazias.
   **Não edite o arquivo commitado.** Sobrescreva por variável de ambiente ao subir o Front.

2. **`Invoke-WebRequest` do PowerShell 5.1 não negocia TLS com o certificado de dev.** Falha com *"A conexão subjacente estava fechada"*. Use o endpoint **HTTP** (`61978`) para smoke test. O navegador não tem esse problema e acessa o Front por HTTPS normalmente.

3. **PostgreSQL precisa estar de pé** em `localhost:5432` (`locadora_autos`, `admin`/`admin123`, conforme `Locadora_Auto.Api/appsettings.Development.json`). Sem ele a Api sobe mas todo endpoint estoura.

## Passos

1. **Confira o Postgres** e compile:
   ```powershell
   (Test-NetConnection -ComputerName localhost -Port 5432 -WarningAction SilentlyContinue).TcpTestSucceeded
   dotnet build Locadora_Auto-Api.sln -c Debug --nologo
   ```

2. **Suba a Api** em background (`run_in_background: true`):
   ```powershell
   dotnet run --project Locadora_Auto.Api\Locadora_Auto.Api.csproj --launch-profile "Locadora_Auto.Api" --no-build
   ```
   Espere `Now listening on: https://localhost:61977` no arquivo de saída da task.

3. **Suba o Front** em background, com a Api sobrescrita para HTTP:
   ```powershell
   $env:ApiConfig__BaseUrlApiLocacao = "http://localhost:61978/"
   $env:ASPNETCORE_ENVIRONMENT = "Development"
   dotnet run --project Locadora_Auto.Front\Locadora_Auto.Front.csproj --launch-profile "Locadora_Auto.Front" --no-build
   ```
   Espere `Now listening on: https://localhost:62259`.

4. **Smoke test da Api por HTTP** antes de abrir o navegador — separa "Api quebrada" de "tela quebrada":
   ```powershell
   Invoke-RestMethod -Uri "http://localhost:61978/api/v1/reservas?pagina=1&itensPorPagina=3" -TimeoutSec 20
   ```

5. **Dirija o Front** com as ferramentas `mcp__claude-in-chrome__*` em `https://localhost:62259`. Autenticação está comentada (ver `Program.cs`), então não há login. Use `read_page` com `filter: "interactive"` para pegar os `ref` e `form_input` para os `<select>` do Blazor — clicar em dropdown por coordenada é frágil.

6. **Ao terminar**, encerre e confirme que as portas caíram:
   ```powershell
   Get-Process -Name dotnet -ErrorAction SilentlyContinue | Stop-Process -Force
   ```
   Isso derruba **todos** os processos `dotnet`, inclusive os que o usuário tenha aberto no Visual Studio — avise antes se houver risco.

## Rotas úteis da Api

Nem toda rota segue o nome do controller. As confirmadas:

| Recurso | Rota |
|---|---|
| Reservas | `api/v1/reservas` |
| Clientes | `api/v1/clientes` |
| Filiais | `api/v1/filiais` |
| Categorias de veículo | `api/v1/categorias-veiculos` |
| Veículos | `api/v1/veiculos` |
| Seguros | `api/v1/seguros` |

## Dados para testar reserva

A reserva valida disponibilidade da frota: precisa existir veículo com `Disponivel = true` **na categoria e na filial escolhidas**, e `veiculosDisponiveis > locações em aberto + reservas com período sobreposto`. Confira antes de culpar a tela:

```powershell
Invoke-RestMethod -Uri "http://localhost:61978/api/v1/veiculos/1" -TimeoutSec 20
```

## Datas: 3 horas de diferença não é bug de gravação

A Api grava e devolve em **UTC** (`timestamp with time zone`). Uma retirada marcada às 09:00 em Brasília é persistida como `12:00Z` — isso está **correto**. Se a tela mostrar 12:00, o defeito é de exibição: falta converter para hora local antes de formatar. As telas de reserva já fazem isso (`FormatarDataLocal`); o resto do Front ainda imprime UTC cru.
