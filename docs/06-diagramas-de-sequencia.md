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
    participant Ctl as ReservaController
    participant Svc as ReservaService
    participant Rep as IClienteRepository
    participant Cliente as Clientes (raiz do agregado)
    participant Db as PostgreSQL

    Cli->>Ctl: POST api/v1/reservas<br/>{ idCliente, idFilial, idCategoriaVeiculo, início, fim }
    Ctl->>Svc: CriarAsync(dto, ct)
    Svc->>Svc: normaliza datas para UTC
    Svc->>Rep: ObterPrimeiroAsync(idCliente, rastreado: true)
    Rep->>Db: SELECT tb_cliente
    Db-->>Rep: cliente
    Note over Svc: valida cliente ativo, categoria e filial existentes,<br/>datas futuras e disponibilidade da frota no período
    Svc->>Cliente: ReservarVeiculo(idCliente, inicio, fim,<br/>idFilial, idCategoria)
    Cliente->>Cliente: Reserva.Criar(...) — internal, só a raiz cria
    Note over Cliente: valida idCliente, idCategoria,<br/>início e fim futuros, fim > início<br/>Status = Reservado, Ativo = true
    Svc->>Rep: SalvarAsync(ct)
    Rep->>Db: INSERT tb_reserva
    Svc-->>Ctl: ReservaDto
    Ctl-->>Cli: 201 Created
```

O cancelamento segue o mesmo caminho: `ReservaService.CancelarAsync` carrega o cliente dono da
reserva com `Include(Reservas)` rastreado e chama `Clientes.CancelarReservar(reserva)`.

Duas operações **não** passam pela raiz, por motivos que já existiam no projeto: `Finalizar` é
disparado por `Locacao.Criar` (outro agregado, ver §4) e a expiração em lote é uma varredura por
tempo sobre `tb_reserva`, que carregaria todos os clientes se fosse pela raiz.

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

## 5. Devolução e fechamento

Doc `07` §1: **DEVOLUÇÃO → FECHAMENTO → QUITAÇÃO** são atos distintos, e desde o backlog `A11` são
portas distintas. Antes havia uma só, `POST {id}/finalizar`, que recebia o `valorFinal` digitado por
quem chamava — nenhum cálculo, nenhum extrato.

### 5.1 Devolução — encerra a posse

```mermaid
sequenceDiagram
    autonumber
    actor At as Atendente
    participant Ctl as LocacoesController
    participant Svc as LocacaoService
    participant Loc as Locacao (agregado)
    participant Vei as Veiculo
    participant Db as PostgreSQL

    At->>Ctl: POST api/locacoes/{id}/devolucao<br/>{ dataFimReal, idFilialDevolucao }
    Ctl->>Svc: RegistrarDevolucaoAsync(id, dto, ct)
    Svc->>Db: SELECT locação + veículo + pagamentos + vistorias/danos
    Note over Svc: guardas repetidas como notificação:<br/>veículo locado, filial existe,<br/>par de vistorias (RN-57)

    Svc->>Loc: RegistrarDevolucao(dataFimReal, filialDevolucao)
    Note over Loc: RN-11: o hodômetro sai da<br/>vistoria de devolução, não do DTO
    Loc->>Loc: Status = Devolvida<br/>grava DataFimReal, KmFinal e filial
    Loc->>Vei: RegistrarDevolucao(km, filial, contrato)
    Vei->>Vei: Status = EmPreparacao<br/>KmAtual e FilialAtual avançam

    Svc->>Db: UPDATE tb_locacao, tb_veiculo
    Ctl-->>At: 200 OK
```

RN-58: receber o carro **não** fecha a conta. O contrato para em `Devolvida` e o veículo entra na
fila do pátio — não volta à oferta (RN-44).

### 5.2 Fechamento — apura, fecha e resolve a caução

```mermaid
sequenceDiagram
    autonumber
    actor At as Atendente
    participant Ctl as LocacoesController
    participant Svc as LocacaoService
    participant Loc as Locacao (agregado)
    participant Fec as FechamentoLocacao
    participant Cau as Caucao
    participant Db as PostgreSQL

    At->>Ctl: POST api/locacoes/{id}/fechamento<br/>{ idFuncionarioApuracao, alçada? }
    Ctl->>Svc: ApurarFechamentoAsync(id, dto, ct)

    Svc->>Db: SELECT com Include de vistorias, danos, fotos,<br/>categoria, seguros, adicionais,<br/>pagamentos, multas, cauções e filiais
    Note over Svc: falta de Include aqui faz a conta<br/>sair MENOR, sem erro e sem aviso

    Svc->>Loc: ApurarFechamento(veiculo, categoria,<br/>filialRetirada, filialDevolucao, idFuncionario)

    alt conta já selada (RN-32)
        Loc-->>Svc: mesma conta, JaEstavaApurado = true
    else primeira apuração
        Loc->>Fec: Abrir + Lancar (período, km, combustível,<br/>proteção, acessórios, taxas, avarias,<br/>multas, pagamentos)
        Note over Fec: uma linha por regra, imutável,<br/>arredondada a 2 casas (RN-31, RN-33)
        Loc->>Fec: Selar()
        Loc->>Loc: ValorFinal = Saldo<br/>Status = Fechada
        Loc->>Cau: Consumir(saldo) ou Devolver()
        Note over Cau: RN-30: só depois do saldo apurado
        Loc->>Loc: LiquidarSaldo()<br/>Finalizada ou ComSaldoResidual
        Svc->>Db: UPDATE + INSERT tb_fechamento_locacao,<br/>tb_linha_fechamento
    end

    Svc-->>Ctl: ResultadoDaApuracaoDto<br/>extrato + saldo residual + avisos
    Ctl-->>At: 200 OK
```

A recusa de regra do domínio (`DomainException`) é **capturada pelo serviço** e vira notificação —
sem isso, uma filial não habilitada para one-way (RN-22) sairia como 500 no balcão. Os avisos que
acompanham o extrato — avaria em análise com prazo, multa recusada por redundância, combustível não
cobrado por falta de cadastro — são parte da resposta, não log: cada um é dinheiro ou prazo que
alguém precisa acompanhar.

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
