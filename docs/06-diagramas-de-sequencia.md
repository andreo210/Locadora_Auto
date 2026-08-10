# 06 — Diagramas de sequência

Fluxos ponta a ponta dos principais casos de uso, com os nomes reais de métodos e classes.

---

## 1. Autenticação

```mermaid
sequenceDiagram
    autonumber
    actor U as Usuário
    participant Front as Blazor — Login.razor
    participant LSvc as LoginService (Front.Services)
    participant Ctl as UsersController
    participant USvc as IUserService
    participant SM as SignInManager de User
    participant TSvc as TokenService
    participant Rsa as RsaKeyService
    participant Rep as ITokenRepository
    participant Db as PostgreSQL

    U->>Front: CPF + senha
    Front->>LSvc: Autenticar(loginDto)
    LSvc->>Ctl: POST api/v1/Users/autenticar
    Ctl->>Ctl: ModelState válido?
    Ctl->>USvc: LoginAsync(usuarioLogin)
    USvc->>SM: PasswordSignInAsync
    SM->>Db: SELECT asp_net_users
    Db-->>SM: usuário + hash
    SM-->>USvc: SignInResult

    alt Credenciais válidas
        Ctl->>TSvc: GerarToken(cpf)
        TSvc->>USvc: ObterPorCpf(cpf)
        TSvc->>TSvc: ObterClaims(user, access, id)
        TSvc->>Rsa: chave privada de Jwt:PrivateKeyPath
        Rsa-->>TSvc: RSA (cria a chave se não existir)
        TSvc->>TSvc: CriarToken — assinatura RS256
        TSvc->>Rep: grava refresh token
        Rep->>Db: INSERT refresh_tokens
        TSvc-->>Ctl: TokenDto
        Ctl-->>LSvc: 200 OK + accessToken/refreshToken
        LSvc->>Front: cria cookie e guarda os tokens<br/>em AuthenticationProperties
        Front-->>U: redireciona autenticado
    else Bloqueado por tentativas
        Ctl-->>LSvc: 400 + ValidationProblemDetails "BLOQUEADO"
        LSvc-->>U: mensagem de bloqueio
    else Credenciais inválidas
        Ctl-->>LSvc: 400 + "Usuário ou Senha incorretos"
        LSvc-->>U: mensagem de erro
    end
```

A API valida os próprios tokens com a chave pública publicada em `GET /.well-known/jwks.json`
(`JwksController`).

### 1.1 Renovação de sessão

```mermaid
sequenceDiagram
    autonumber
    participant Cli as Cliente HTTP
    participant Ctl as UsersController
    participant USvc as IUserService
    participant TSvc as TokenService
    participant Db as PostgreSQL

    Cli->>Ctl: POST api/v1/Users/renovar { refreshToken }
    Ctl->>Ctl: ObterIdToken(refreshToken)

    alt Token expirado
        Ctl-->>Cli: 400 + "token expirado"
    else Token válido
        Ctl->>USvc: DesativarToken(idRefreshToken)
        USvc->>Db: UPDATE refresh_tokens SET revogado = true
        USvc-->>Ctl: User
        Ctl->>TSvc: GerarToken(user.UserName)
        TSvc->>Db: INSERT novo refresh_tokens
        TSvc-->>Ctl: TokenDto
        Ctl-->>Cli: 200 OK + novo par de tokens
    end
```

---

## 2. Cadastrar cliente

Cadastrar um cliente cria simultaneamente o `User` (identidade) e o `Clientes` (perfil) — o EF
grava as duas tabelas na mesma operação porque `user.Cliente` já vem preenchido.

```mermaid
sequenceDiagram
    autonumber
    actor Op as Administrador
    participant Front as CriarCliente.razor
    participant FSvc as ClienteService (Front)
    participant Ctl as ClientesController
    participant Svc as ClienteService (Application)
    participant Not as INotificadorService
    participant UM as UserManager de User
    participant Ctx as LocadoraDbContext
    participant Db as PostgreSQL

    Op->>Front: preenche nome, CPF, e-mail,<br/>telefone, CNH e endereço
    Front->>FSvc: CriarAsync(dto)
    FSvc->>Ctl: POST api/v1/Clientes
    Ctl->>Svc: CriarClienteAsync(clienteDto, ct)
    Svc->>Svc: ValidarCriacaoClienteAsync

    alt Validação falha
        Svc->>Not: Add("mensagem")
        Svc-->>Ctl: null
        Ctl->>Not: TemNotificacao() → true
        Ctl-->>FSvc: ProblemDetails
    else Validação ok
        Svc->>Svc: User.Criar(nome, cpf, telefone, email)
        Note over Svc: valida CPF, limpa máscara<br/>UserName = CPF
        Svc->>Svc: Clientes.Criar(numeroHabilitacao,<br/>validadeCnh, endereco)
        Note over Svc: Status = Habilitado, Ativo = true<br/>Endereco.Criar valida e limpa o CEP
        Svc->>UM: CreateAsync(user, senha)
        UM->>Ctx: SaveChangesAsync
        Ctx->>Ctx: AplicarAuditoria — DataCriacao,<br/>IdUsuarioCriacao no Clientes
        Ctx->>Db: INSERT asp_net_users
        Ctx->>Db: INSERT tb_cliente
        Ctx->>Db: INSERT tb_endereco
        UM-->>Svc: IdentityResult
        Svc->>Svc: ToDto + completa CPF, e-mail,<br/>telefone e nome vindos do User
        Svc-->>Ctl: ClienteDto
        Ctl-->>FSvc: 201 Created
        Front-->>Op: cliente cadastrado
    end
```

> Se `UserManager.CreateAsync` falhar, o serviço **lança** `InvalidOperationException` em vez
> de notificar — esse caso cai no `ExceptionMiddleware`, não no fluxo de `ProblemDetails` do
> notificador.

---

## 3. Reservar veículo

```mermaid
sequenceDiagram
    autonumber
    actor Cli as Cliente ou Atendente
    participant Ctl as ClientesController
    participant Svc as ClienteService
    participant Rep as IClienteRepository
    participant Cliente as Clientes (agregado)
    participant Db as PostgreSQL

    Cli->>Ctl: POST api/v1/Clientes/reserva<br/>{ idCliente, idFilial, idCategoria, início, fim }
    Ctl->>Svc: CriarReservaAsync(dto, ct)
    Svc->>Rep: ObterPrimeiroAsync(idCliente, rastreado: true)
    Rep->>Db: SELECT tb_cliente
    Db-->>Rep: cliente
    Svc->>Cliente: ReservarVeiculo(idCliente, inicio, fim,<br/>idFilial, idCategoria)
    Cliente->>Cliente: Reserva.Criar(...)
    Note over Cliente: valida idCliente, idCategoria,<br/>início e fim futuros, fim > início<br/>Status = Reservado, Ativo = true
    Svc->>Rep: AtualizarSalvarAsync(cliente, ct)
    Rep->>Db: INSERT tb_reserva
    Svc-->>Ctl: true
    Ctl-->>Cli: 200 OK
```

---

## 4. Abrir locação

O caso mais rico do sistema: valida a reserva (quando existe), carrega os quatro agregados
envolvidos e delega as invariantes para `Locacao.Criar`.

```mermaid
sequenceDiagram
    autonumber
    actor At as Atendente
    participant Ctl as LocacoesController
    participant Svc as LocacaoService
    participant RRep as IReservaRepository
    participant VRep as IVeiculosRepository
    participant CRep as IClienteRepository
    participant FRep as IFilialRepository
    participant FuRep as IFuncionarioRepository
    participant LRep as ILocacaoRepository
    participant Not as INotificadorService
    participant Db as PostgreSQL

    At->>Ctl: POST api/locacoes { CriarLocacaoDto }
    Ctl->>Svc: CriarAsync(dto, ct)

    Svc->>RRep: ObterPrimeiroAsync(idReserva, rastreado: true)
    RRep->>Db: SELECT tb_reserva
    Svc->>VRep: ObterPorIdAsync(idVeiculo)
    VRep->>Db: SELECT tb_veiculo

    opt Locação originada de reserva
        Svc->>Svc: reserva ativa?
        Svc->>Svc: reserva pertence ao cliente?
        Svc->>Svc: veículo é da categoria reservada?
        Svc->>Svc: datas conferem?
        Svc->>Svc: filial de retirada confere?
        Svc->>Svc: status == Reservado?
        alt Alguma verificação falhou
            Svc->>Not: Add(mensagens)
            Svc-->>Ctl: null
            Ctl-->>At: 400 + ProblemDetails
        else Tudo confere
            Svc->>Svc: sobrescreve datas, filial e cliente<br/>com os valores da reserva
        end
    end

    Svc->>CRep: ObterPorIdAsync(idCliente)
    Svc->>FRep: ObterPorIdAsync(idFilialRetirada)
    Svc->>FuRep: ObterPorIdAsync(idFuncionario)
    Db-->>Svc: cliente, filial, funcionário

    Svc->>Svc: existem? veículo disponível?<br/>dataFimPrevista > dataInicio?

    alt Notificações registradas
        Svc-->>Ctl: null
        Ctl-->>At: 400 + ProblemDetails
    else Válido
        Svc->>Svc: Locacao.Criar(cliente, veiculo, funcionario,<br/>reserva, filialRetirada, datas, km, valor)
        Note over Svc: Cliente.PodeLocar()<br/>Veiculo.Disponivel<br/>Status = Criada<br/>Reserva.Finalizar()<br/>Veiculo.Indisponibilizar()
        Svc->>LRep: InserirSalvarAsync(locacao, ct)
        LRep->>Db: INSERT tb_locacao<br/>UPDATE tb_veiculo, tb_reserva
        Svc-->>Ctl: LocacaoDto
        Ctl-->>At: 201 Created
    end
```

`Locacao.Criar` lança exceção (`ArgumentNullException` / `InvalidOperationException`) quando
uma invariante do domínio é violada — diferente das validações do serviço, que usam o
notificador. As duas formas de sinalizar erro coexistem neste fluxo.

---

## 5. Finalizar locação (devolução)

```mermaid
sequenceDiagram
    autonumber
    actor At as Atendente
    participant Ctl as LocacoesController
    participant Svc as LocacaoService
    participant Rep as ILocacaoRepository
    participant Loc as Locacao (agregado)
    participant Vei as Veiculo
    participant Db as PostgreSQL

    At->>Ctl: POST api/locacoes/{id}/finalizar<br/>{ dataFimReal, kmFinal, valorFinal, filialDevolucao }
    Ctl->>Svc: FinalizarAsync(id, ..., ct)
    Svc->>Rep: ObterPrimeiroAsync(id, incluir: Veiculo, rastreado: true)
    Rep->>Db: SELECT tb_locacao JOIN tb_veiculo
    Db-->>Svc: locação com veículo

    Svc->>Loc: Finalizar(dataFimReal, kmFinal,<br/>valorFinal, filialDevolucao)
    Note over Loc: Status deve ser Criada<br/>dataFimReal >= DataInicio<br/>kmFinal >= KmInicial
    Loc->>Loc: Status = Finalizada<br/>grava km, valor e filial de devolução
    Loc->>Vei: Disponibilizar()
    Vei->>Vei: Disponivel = true

    Svc->>Rep: AtualizarSalvarAsync(locacao, ct)
    Rep->>Db: UPDATE tb_locacao, tb_veiculo
    Svc-->>Ctl: true
    Ctl-->>At: 200 OK
```

---

## 6. Vistoria de devolução com dano

Registrar um dano na vistoria dispara automaticamente a abertura de uma manutenção corretiva
no veículo.

```mermaid
sequenceDiagram
    autonumber
    actor At as Atendente
    participant Ctl as LocacoesController
    participant Svc as LocacaoService
    participant Loc as Locacao
    participant Vis as Vistoria
    participant Vei as Veiculo
    participant Up as IUploadDownloadFileService
    participant Db as PostgreSQL

    At->>Ctl: POST api/locacoes/{id}/vistoria<br/>{ tipo: Devolucao, combustível, km, observações }
    Ctl->>Svc: RegistrarVistoriaAsync(id, dto, ct)
    Svc->>Loc: RegistrarVistoria(idFuncionario, tipo,<br/>combustivel, km, observacoes)
    Note over Loc: recusa se Status == Finalizada
    Loc->>Vis: Vistoria.Criar(...)
    Svc->>Db: INSERT tb_vistoria

    At->>Ctl: POST api/locacoes/{id}/vistoria/enviar-fotos
    Ctl->>Svc: RegistrarFotoVistoriaAsync(id, dto, ct)
    Svc->>Up: grava os arquivos em disco
    Up-->>Svc: caminho, extensão, tamanho
    Svc->>Loc: RegistrarFoto(fotos, idVistoria)
    Loc->>Vis: AdicionarFoto(foto)
    Svc->>Db: INSERT tb_foto_vistoria

    At->>Ctl: POST api/locacoes/{id}/vistoria/registrar-dano<br/>{ idVistoria, descrição, tipo, valor }
    Ctl->>Svc: RegistrarDanoVistoriaAsync(id, dto, ct)
    Svc->>Loc: RegistrarDanoVistoria(idVistoria,<br/>descricao, tipo, valor)
    Loc->>Vis: RegistrarDano(descricao, tipo, valor)
    Note over Vis: só em vistoria de Devolucao
    Vis->>Vis: Dano.Criar → Status = Registrado

    Loc->>Vei: IniciarManutencao(Corretiva,<br/>"Manutenção gerada automaticamente por dano em vistoria")
    Note over Vei: recusa se Status == Locado
    Vei->>Vei: Status = EmManutencao

    Svc->>Db: INSERT tb_dano, tb_manutencao<br/>UPDATE tb_veiculo
    Ctl-->>At: 200 OK
```

---

## 7. Multa compensada com caução

```mermaid
sequenceDiagram
    autonumber
    actor At as Atendente
    participant Ctl as LocacoesController
    participant Svc as LocacaoService
    participant Loc as Locacao
    participant Mul as Multa
    participant Cau as Caucao
    participant Db as PostgreSQL

    Note over At,Db: a locação já está Finalizada

    At->>Ctl: POST api/locacoes/{id}/caucao/{valor}
    Ctl->>Svc: AdicionarCalcaoAsync(id, valor, ct)
    Svc->>Loc: RegistrarCaucao(valor)
    Loc->>Cau: Caucao.Criar(valor) → Pendente
    Svc->>Db: INSERT tb_caucao

    At->>Ctl: POST api/locacoes/{id}/multas<br/>{ tipo, valor }
    Ctl->>Svc: AdicionarMultaAsync(id, dto, ct)
    Svc->>Loc: AdicionarMulta(tipo, valor)
    Note over Loc: exige Status == Finalizada<br/>e valor > 0
    Loc->>Mul: Multa.Criar(valor, tipo) → Pendente
    Svc->>Db: INSERT tb_multa

    At->>Ctl: POST api/locacoes/{id}/multas/{idMulta}/compensar
    Ctl->>Svc: CompensarMultaAsync(id, idMulta, ct)
    Svc->>Loc: CompensarMultaComCaucao(idMulta)
    Loc->>Loc: soma das cauções >= valor da multa?
    loop cada caução com saldo
        Loc->>Cau: Deduzir(min(saldo, valorRestante))
    end
    Loc->>Mul: CompensarComCaucao() → CompensadaCaucao
    Svc->>Db: UPDATE tb_caucao, tb_multa
    Ctl-->>At: 200 OK
```

---

## 8. Upload e leitura de fotos

```mermaid
sequenceDiagram
    autonumber
    actor Op as Administrador
    participant Front as UploadFotos.razor
    participant Ctl as CategoriaVeiculosController
    participant Svc as CategoriaVeiculosService
    participant Val as IValidadorArquivoService
    participant Up as IUploadDownloadFileService
    participant Cat as CategoriaVeiculo
    participant Fs as Sistema de arquivos
    participant Db as PostgreSQL

    Op->>Front: seleciona os arquivos
    Front->>Ctl: POST api/v1/categorias-veiculos/{id}/registrar-foto<br/>multipart/form-data
    Ctl->>Svc: RegistarFotoCategoriaAsync(id, fotos, ct)
    Svc->>Val: valida extensão e tamanho
    Svc->>Up: grava os arquivos
    Up->>Fs: escreve em Raiz/Diretorio
    Up-->>Svc: nome, raiz, diretório, extensão, bytes
    Svc->>Cat: AdicionarFoto(fotos)
    Cat->>Cat: FotoCategoriaVeiculo.Criar(...)<br/>DataUpload = UtcNow
    Svc->>Db: INSERT tb_foto_categoria_veiculo
    Ctl-->>Front: 200 OK

    Op->>Front: abre a foto
    Front->>Ctl: GET .../{id}/fotos/{idFoto}?width=300
    Ctl->>Db: SELECT tb_foto_categoria_veiculo
    Ctl->>Fs: lê o arquivo
    Ctl->>Ctl: IImageService.RedimensionarAsync
    Ctl-->>Front: 200 OK + bytes da imagem
```

O mesmo desenho vale para `POST api/v1/filiais/{id}/registrar-foto` (`FotoFilial`) e para as
fotos de vistoria.

---

## 9. Auditoria e histórico temporal

Disparado a cada `SaveChangesAsync`, para qualquer entidade — não há chamada explícita nos
serviços.

```mermaid
sequenceDiagram
    autonumber
    participant Svc as Qualquer Service
    participant Rep as RepositorioGlobal
    participant Ctx as LocadoraDbContext
    participant Cur as ICurrentUser
    participant Db as PostgreSQL

    Svc->>Rep: AtualizarSalvarAsync(entidade, ct)
    Rep->>Ctx: SaveChangesAsync(ct)

    Ctx->>Ctx: AplicarAuditoria()
    loop entradas do ChangeTracker
        alt entidade implementa IAuditoria
            Ctx->>Cur: UserId
            Ctx->>Ctx: Added → DataCriacao, IdUsuarioCriacao<br/>Modified → DataModificacao, IdUsuarioModificacao
        end
    end

    Ctx->>Ctx: CriarHistoricoTemporal()
    loop entradas Modified ou Deleted
        alt entidade implementa ITemporalEntity de THistory
            Ctx->>Ctx: Activator.CreateInstance(THistory)
            Ctx->>Ctx: MapearValores — copia OriginalValues<br/>por reflexão para o histórico
            Ctx->>Cur: UserId ?? "SYSTEM"
            Ctx->>Ctx: DataEvento = UtcNow<br/>Acao = UPDATE ou DELETE
            Ctx->>Ctx: Add(history)
        end
    end

    Ctx->>Ctx: conversor UTC em todo DateTime
    Ctx->>Db: base.SaveChangesAsync<br/>UPDATE + INSERT no histórico
```

Hoje participam apenas `Clientes` (→ `tb_cliente_historico`) e `User` (→ `tb_user_historico`).
O `SaveChanges()` **síncrono** não passa por nada disso.

---

## 10. Erro de regra de negócio

```mermaid
sequenceDiagram
    autonumber
    actor Cli as Cliente HTTP
    participant Ctl as XxxController
    participant Svc as XxxService
    participant Not as INotificadorService (scoped)
    participant Map as NotificationProblemAdapterMapper

    Cli->>Ctl: POST /api/v1/recurso
    Ctl->>Svc: OperacaoAsync(dto, ct)
    Svc->>Svc: regra violada
    Svc->>Not: Add("Veículo não disponível")
    Svc-->>Ctl: null

    Ctl->>Ctl: CustomResponse(null, Created)
    Ctl->>Not: TemNotificacao()
    Not-->>Ctl: true
    Ctl->>Not: ObterNotificacoes()
    Not-->>Ctl: lista de Notificacao
    Ctl->>Map: ToProblemDetails(HttpContext, notificacoes)
    Map-->>Ctl: ProblemDetails (RFC 7807)
    Ctl-->>Cli: StatusCode(problem.Status) + ProblemDetails
```

Como o `INotificadorService` é *scoped*, as notificações acumuladas durante a requisição são
todas devolvidas juntas — vários `Add` no mesmo serviço viram um único `ProblemDetails` com a
lista completa (é o que acontece nas validações de reserva do fluxo 4).

---

## 11. Rotina diária

```mermaid
sequenceDiagram
    autonumber
    participant Host as .NET Generic Host
    participant Job as TarefaDiariaBackgroundService
    participant Sp as IServiceProvider
    participant Ctx as LocadoraDbContext
    participant Db as PostgreSQL

    Host->>Job: ExecuteAsync(stoppingToken)
    loop até o host parar
        Job->>Job: calcula a próxima execução — 03:00
        Job->>Job: await Task.Delay(delay, token)
        Job->>Sp: CreateScope()
        Sp-->>Job: escopo
        Job->>Ctx: GetRequiredService de LocadoraDbContext
        Note over Job,Ctx: corpo da rotina ainda vazio —<br/>a limpeza de logs está comentada
        Job->>Ctx: SaveChangesAsync(stoppingToken)
        Ctx->>Db: nenhuma alteração pendente
    end
```

---

## Observações

- O fluxo 4 mistura os dois estilos de sinalização de erro: validações do serviço usam o
  notificador; invariantes dentro de `Locacao.Criar` lançam exceção capturada pelo
  `ExceptionMiddleware`.
- `LocacaoService.CriarAsync` acessa `dto.idReserva.Value` sem checar `HasValue` — uma
  requisição sem reserva chega ao `.Value` de um `int?` nulo.
- Vários métodos de `LocacaoService` chamam `AtualizarSalvarAsync(locacao)` sem repassar o
  `CancellationToken` recebido.
- `TarefaDiariaBackgroundService` calcula o horário com `DateTime.Now` (local), diferente do
  UTC usado na persistência.
