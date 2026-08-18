# 09 — Backlog

> **Este documento não é especificação nem descrição do sistema.** É a fila de trabalho: o que
> está aberto, em que ordem faz sentido pegar e onde encostar a mão. Cada item aponta a origem —
> uma RN das especificações `07`/`08`, uma armadilha registrada no `CLAUDE.md`, ou o próprio
> código. Quando um item for concluído, risque a linha aqui e atualize o documento de origem.

Estado da base em 18/08/2026: a especificação `08` (invariante do ativo) está **implantada
inteira** — RN-35 a RN-56 mais os seis indicadores da seção 12. O que sobrou dela não é regra, é
tela: nenhum dos endpoints do ativo tem consumidor no front (`F5`, `F6`, e agora também bloqueio,
transferência e desmobilização).

Da `07` (fechamento financeiro) está de pé **o ciclo de vida do contrato** (`A1`) — devolução e
fechamento deixaram de ser o mesmo ato —, mas nenhuma apuração: `Fechar` continua recebendo
`valorFinal` pronto de quem chama. **É o buraco funcional do sistema, e agora é o único bloco de
regra aberto na Api.**

**Tamanhos:** `P` = uma sessão · `M` = duas a três · `G` = fatiar antes de começar.

## Por onde pegar

Três frentes independentes, para escolher pelo tempo disponível e não pela ordem da lista:

| Frente | Primeiro item | Por quê |
|---|---|---|
| Entrega visível rápida | **F3** (Adicionais) → **F7** (liberar preparação) → **F6** (trilha) | Api já pronta; é só front consumindo endpoint existente. Com o bloco B fechado, esta frente cresceu: bloqueio, transferência e desmobilização também são só tela |
| Fio principal | **A2**/**A3** (dados e parâmetros) → **A4**–**A10** (fechamento) | É o buraco funcional do sistema: hoje o valor da devolução é digitado. O `A1` já abriu o caminho, e agora é o único bloco de regra aberto |
| Dívida que trava outras | **C3** (locações paginadas) → **C1**/**C2** (multa) → **C8** (leitura de vistoria) | Cada um destrava uma tela do front |

O **F1** (módulo de locações no front) é o maior item da lista inteira e depende de `C3`. Não
comece por ele num dia curto.

---

# API

## Bloco A — fechamento financeiro (doc `07`, nada implementado)

Ordem obrigatória: `A1` antes de tudo, `A4` antes de `A5`–`A10`.

~~**A1 · Estados de locação de verdade** — `M`~~ **feito.**
`StatusLocacao` passou a ser `Criada → EmAndamento → Devolvida → Fechada → Finalizada`, com
`Atrasada` e `ComSaldoResidual` nos ramos e `Cancelada` como estado próprio. `Pendente` saiu: era a
`Reserva` com outro nome, já que `Criar` compromete a placa. As RN novas estão no doc `05` §1, com
teste por regra em `LocacaoTests` — **menos a RN-62**, que virou o `C12`.

Três decisões que valem para quem for pegar o resto do bloco A:

- **`Finalizar()` virou dois atos.** `RegistrarDevolucao` encerra a posse e para em `Devolvida`;
  `Fechar(valorFinal)` apura e vai para `Fechada`; `LiquidarSaldo()` decide entre `Finalizada` e
  `ComSaldoResidual`. A porta da Api continua sendo uma só — `FinalizarAsync` chama os três em
  sequência — e é isso que o `A11` separa. Quando a apuração real (`A5`–`A10`) entrar, quem muda é
  `Fechar`, não o ciclo de vida.
- **Devolver exige o par de vistorias** (RN-57), e a vistoria de retirada é o que promove o
  contrato a `EmAndamento`. Isso muda o processo de quem opera, não só o código: contrato sem
  vistoria de retirada não fecha. A decisão foi **bloquear**, não alçada.
- **`StatusTerminais = { Finalizada, Cancelada }`.** `Devolvida`, `Fechada` e `ComSaldoResidual`
  ficam de fora de propósito — o carro rodou naquele período e a `DataFimReal` já encolheu o
  intervalo, então protegem o histórico sem travar contrato novo. O predicado da constraint foi
  reescrito na migration `EstadosDeLocacao`, e o `SobreposicaoDeContratoTests` agora acha por
  reflexão a **migration mais recente** que define a constraint — mexer no predicado é sempre
  migration nova, nunca editar a anterior.

**A2 · Campos congelados no contrato** — `M` · RN-06, RN-14, RN-18, RN-21, RN-22, RN-25
`Locacao.ValorDiariaContratada`; `LocacaoSeguro.ValorDiariaContratada` e `FranquiaContratada`;
`Veiculo.CapacidadeTanqueLitros`; `Filial.HabilitadaOneWay` e `TaxaRetornoOneWay`. Migration na
mesma mudança. Sem cálculo ainda — só o dado e o preenchimento na abertura do contrato. Sem isso
alterar uma categoria reescreveria contratos passados.

**A3 · Parâmetros da casa** — `P` · RN-03, RN-04, RN-15, RN-23
`ToleranciaMinutos`, `PercentualHoraExcedente`, `PrecoLitroCombustivel`,
`TaxaServicoAbastecimento`, `ValorLimpezaEspecial`.
**Decisão pendente:** por filial ou global. O doc `07` §9 recomenda 30 min de tolerância, hora
excedente a 1/3 da diária com teto de 1 diária, e full-to-full no combustível.

**A4 · Entidades `FechamentoLocacao` e `LinhaFechamento`** — `M` · RN-31, RN-33
Linha discriminada (tipo, base de cálculo, quantidade, valor unitário, total), **imutável** após o
fechamento, arredondamento a 2 casas por linha com `MidpointRounding.AwayFromZero`. Correção é
lançamento novo com autor e motivo, nunca edição.

**A5 · Apuração do período** — `M` · RN-01 a RN-07
Diária = ciclo de 24h a partir de `DataInicio` (nunca calendário), mínimo 1; tolerância; hora
excedente por hora iniciada; **teto de 1 diária** substituindo as horas. É domínio puro — testa
sem banco, e os quatro primeiros cenários gherkin do doc `07` §10 já servem de teste.

**A6 · Quilometragem e combustível** — `M` · RN-08 a RN-16
Franquia = `LimiteKm × diárias cobradas`; excedente sobre isso. `KmAtual` já avança na devolução
(RN-12, feito no bloco anterior). Combustível full-to-full pelo enum `NivelCombustivel` ×
`CapacidadeTanqueLitros` + taxa de serviço cobrada uma vez; devolver com mais não gera crédito.
Bloqueios: `KmFinal < KmInicial`; `LimiteKm` preenchido com `ValorKmExcedente` nulo. Tanque não
cadastrado **notifica e não cobra** — melhor perder a cobrança que inventar número.

**A7 · Proteções e acessórios** — `M` · RN-17 a RN-20
Recalcular `LocacaoAdicional` pelas diárias **efetivas** — hoje `Dias` congela a previsão e erra em
toda devolução antecipada ou atrasada. Proteção pelas diárias cobradas, pró-rata quando cancelada
no meio do contrato. Depende de `A2`.

**A8 · Taxas** — `P` · RN-21 a RN-23
One-way quando a filial de devolução difere da de retirada, só entre filiais habilitadas (não
habilitada bloqueia e exige alçada). Limpeza especial: valor fixo, só com registro na vistoria de
devolução **e ao menos uma foto**.

**A9 · Avarias e multas no fechamento** — `M` · RN-24 a RN-26
Só entram avarias em `Aprovado` ou `Cobrado`; `Registrado`/`EmAnalise` vão para o pós-contrato.
Havendo proteção, a cobrança ao cliente é limitada à franquia contratada **somando todas as
avarias**, não por avaria. Multa `Pendente` conhecida entra; recebida depois não reabre.

**A10 · Composição, caução e idempotência** — `G` · RN-27 a RN-34
Total, abatimento só de pagamento `Pago`, saldo negativo que **não** trunca para zero, caução
resolvida **depois** do saldo, apuração idempotente.
Inclui consertar a máquina da caução, que hoje está quebrada: `Caucao.Devolver()` só aceita status
`Pendente` — logo uma caução `Bloqueada`, que é o fluxo normal, nunca pode ser devolvida —,
`Deduzir` zera o valor marcando `Bloqueada`, e `StatusCaucao.Utilizada` não é atribuído em lugar
nenhum.

**A11 · Porta da Api** — `M`
`ILocacaoService.FinalizarAsync` deixa de receber `valorFinal`; entra endpoint de apuração e
leitura do fechamento discriminado (o extrato que o cliente recebe). **Quebra o contrato do
`CriarLocacaoDto`/`FinalizarLocacaoDto`** — combinar com `F1` na mesma entrega.

**A12 · Testes do fechamento** — `M`
Os 15 cenários gherkin do doc `07` §10, com `RepositorioFake` + `Fabrica`, no molde de
`LocacaoServiceTests`.

## Bloco B — o que resta do doc `08`

**Fechado.** A especificação `08` está implantada da RN-35 à RN-56, com os seis indicadores da
seção 12. O que resta do ativo é front, e está no bloco D.

~~**B1 · Liberação automática da preparação** — `M` · RN-45 (parte automática)~~ **feito.**
`LiberacaoPreparacaoBackgroundService` varre o pátio a cada 5 min e solta quem passou do
`TempoPreparacaoMinutos` da filial. A decisão do agendador foi **`BackgroundService`**, não
Hangfire — a varredura é idempotente, então não há trabalho que precise sobreviver a restart (o
porquê inteiro está no doc `08`). A liberação por prazo grava `TipoDocumentoOrigem.Prazo`, separada
da do pátio, para o tempo médio de preparação continuar medindo o pátio e não premiar quem nunca
declara nada.

~~**B1.1 · As outras duas varreduras** — `P`~~ **feito.**
`ExpiracaoReservaBackgroundService` (15 min) e `AtrasoLocacaoBackgroundService` (10 min), este
último sobre `LocacaoService.MarcarAtrasadasAsync`, que é o chamador que faltava para o
`MarcarComoAtrasada` da entidade.

A decisão foi **um `BackgroundService` por varredura**, e não um host com três métodos: cada uma
tem cadência própria (a preparação se mede em minutos, a reserva em horas), chave de configuração
própria (`Jobs:<Nome>:Habilitado` / `IntervaloSegundos`) e falha isolada — uma exceção que
escapasse do laço de uma não pode levar as outras duas junto. O custo aceito é a repetição do
laço/espera/escopo nos três.

Fica um débito conhecido: o atraso é marcado **sem tolerância**, no instante do fim previsto. O doc
`07` §9 recomenda 30 minutos, mas o parâmetro é o `A3`. O corte atual é o lado conservador — marca
cedo demais, nunca tarde demais — e `Atrasada` hoje não cobra nada, só torna o contrato visível.

Isso também **fecha a decisão do `C11`**: como cada varredura ganhou serviço próprio, o
`TarefaDiariaBackgroundService` não vira a casa de nada. Resta apagá-lo, e isso continua no `C11`.

~~**B2 · Bloqueio com prazo e responsável** — `M` · RN-52~~ **feito.**
`Indisponivel` virou `Bloqueado` (mesmo valor 2, nada a migrar) e ganhou documento: a entidade
`BloqueioVeiculo`, com motivo tipado, data prevista de liberação e **funcionário responsável** —
FK de verdade, não o autor da auditoria, que hoje grava `"SYSTEM"` para todo mundo.

Três decisões que valem para quem mexer nisso depois:

- **Desativar o veículo não é bloqueio da RN-52.** Os dois levam a `Bloqueado`, e a trilha os
  separa pelo `TipoDocumentoOrigem`. A desativação não é temporária, sai por `Ativar()` e aparece
  em qualquer filtro por `Ativo` — ela não é o carro que "some da oferta e ninguém percebe". Fica
  fora do indicador de bloqueios vencidos, e `Ativar()` **não** libera bloqueio.
- **Liberar devolve o carro ao `StatusAnterior`**, não à oferta. Bloqueio suspende a situação, não
  a apaga: carro bloqueado no pátio volta ao pátio, carro bloqueado por não devolução volta a
  `Locado`.
- **Um bloqueio aberto por vez**, senão a liberação fica sem resposta para "voltar para onde".

~~**B3 · Transferência entre filiais** — `G` · RN-48, RN-49~~ **feito.**
`StatusVeiculo.EmTransferencia`, `TipoDocumentoOrigem.Transferencia`, `Filial.PermiteTransferencia`
(default `true`) e a entidade `TransferenciaVeiculo`, com envio e chegada como dois atos.

`FilialAtualId` **não** muda no envio, só na chegada: enquanto o carro roda, quem responde por ele é
a origem, e trocar antes faria o destino contá-lo como frota antes de ele existir lá. A RN-48
continua valendo — devolução one-way não passa por aqui.

Isso **destrava o recorte histórico por filial dos indicadores**? Não ainda: a trilha continua sem
guardar filial, e o `TransferenciaVeiculo` só sabe da viagem, não de onde o carro estava em cada
instante do período. O recorte segue sendo "onde o carro está hoje" — mas agora existe de onde tirar
o dado, então isso virou item de indicador e não de modelo.

~~**B4 · Unicidade de placa e chassi restrita aos ativos** — `P` · RN-55~~ **feito.**
Índice parcial (`WHERE ativo`) na migration `UnicidadeEntreVeiculosAtivos`. O serviço repete a regra
com o texto já normalizado (trim + maiúscula) — antes a minúscula passava pela checagem e estourava
no índice como 500 — e `AtivarAsync` ganhou a guarda: reativar é a única operação que pode colidir,
porque enquanto o veículo estava inativo nada impedia recadastrar a placa dele.

~~**B5 · Desmobilização** — `M` · RN-56~~ **feito.**
`Desmobilizado` como estado terminal, com motivo, data e responsável em colunas do próprio veículo —
não vira entidade porque acontece uma vez só e não tem duas pontas. A guarda do terminal mora no
`AplicarStatus`, que é a escrita única de status, então nenhuma transição nova pode ressuscitar
carro vendido.

A guarda que só o serviço faz é a do **contrato futuro**: o status é retrato de agora, e um carro
`Disponivel` hoje pode ter contrato vendido para a semana que vem. Desmobilizar continua sendo
recusado com contrato aberto **ou** futuro.

~~**B6 · Indicadores que faltam** — `M` · doc `08` §12~~ **feito.**
Os três entraram no `GET veiculos/indicadores`: `BloqueiosVencidos`, `TransicoesSemDocumento` (que
tem que dar zero) e `TentativasSobreposicaoRecusadas`, aberto em `RecusasPorFilial`.

O terceiro exigiu tabela nova, `RecusaSobreposicao`, gravada pelo `LocacaoService` nos dois
caminhos de recusa — a consulta do serviço e o `23P01` do banco —, contados à parte porque dizem
coisas diferentes: consulta é atendente escolhendo placa comprometida, banco é concorrência real
entre dois pontos de venda. A tabela **não tem FK** de propósito: a série histórica tem de
sobreviver ao veículo ser excluído ou desmobilizado.

O caminho do `23P01` não tem teste automatizado — o `RepositorioFake` não tem constraint, e
reproduzi-lo exigiria integração com Postgres, que não existe. Ele grava depois de um
`LimparRastreamento()`, porque a locação recusada continua `Added` no contexto e gravar sem limpar
mandaria os três de novo.

## Bloco C — dívida técnica e consistência

**C1 · `MultaService.ObterMultasPendentesAsync` lança `NotImplementedException`** — `P`
Não tem endpoint e não tem implementação. Implementar ou tirar da interface. Trava `F4`.

**C2 · `MultaController`** — `P`
Sem paginação; a rota `status-multa/{idTipo:int}` recebe status num parâmetro chamado de tipo;
o método é `ObterPorAtatus`; e o `MultaDto` não devolve status nem a locação de origem.

**C3 · `LocacoesController.ObterTodas` sem paginação nem filtro** — `M`
Migrar para `ConsultaPaginadaRequest` + `OrdenacaoDeConsulta<Locacao>` + `pagina.ParaDto(...)`,
como em `Reserva`/`Seguro`/`Veiculo`. É o que sustenta a listagem de `F1`.

**C4 · Padronizar as listagens antigas** — `M`
`Cliente` e `Funcionario` ainda usam `ordem`/`nome`/`cpf`/`cargo`/`pageNumber`/`pageSize`;
`Filial` e `CategoriaVeiculo` não aceitam ordenação. **Muda a query string** — o
`Locadora_Auto.Front.Services` correspondente entra na mesma mudança.

**C5 · `DomainException` escapando vira 500** — `P`
Ela é `internal`, não deriva de `InvalidOperationException` e não está no `ExceptionProblemFactory`.
Hoje a proteção é os serviços repetirem as guardas do domínio antes de chamá-lo — e o bloco B
triplicou essa repetição: bloqueio, transferência e desmobilização têm cada um a guarda no domínio
e a cópia dela no serviço. Mapear para 400/409 ou fixar por teste que ela nunca escapa.

**C6 · Valores de negócio na rota e na query** — `P`
`POST {id}/caucao/{valor:decimal}`, `caucao/{idCaucao}/bloquear?motivo=`,
`pagamento/{id}/marcar-falha?motivo=` — passar para DTO no corpo.

**C7 · Reativar autenticação** — `M`
`AddApplicationAuthentication`, os `[Authorize]` dos controllers, CORS e health checks estão
comentados no `Program.cs`. Descomentar como está **não compila**: `AddHangFireConfig`/
`UseHangFireConfig` não existem. Como o `B1` decidiu ficar em `BackgroundService`, a saída é
**apagar as duas linhas do Hangfire** ao descomentar o resto, não escrever as extensions. Efeito
colateral bom: o autor do `MovimentoVeiculo` deixa de gravar `"SYSTEM"` sozinho.

**C8 · Leitura de vistoria** — `P`
Existem quatro `POST` de vistoria e **nenhum `GET`**. O fechamento (`A6`, `A9`) e a tela (`F8`)
precisam ler vistoria, fotos e danos.

**C9 · Rotas de versionamento inconsistentes** — `P`
Alguns controllers usam `api/v{version:apiVersion}/[controller]`, outros fixam `api/v1/<nome>`.
Escolher um e uniformizar — mexe na URL, portanto no `Front.Services` junto.

**C10 · Serviços sem teste nenhum** — `M`
`MultaService`, `SeguroService`, `ClienteService`, `FuncionarioService`, `FilialService`,
`CategoriaVeiculosService`.

**C11 · `TarefaDiariaBackgroundService` é template morto** — `P`
`Application/Jobs/JobsBackgroundService/TarefaDiariaBackgroundService.cs` acorda às 3h, abre escopo,
chama `SaveChangesAsync` sem ter alterado nada e tem a única linha de lógica comentada. E **não está
registrado no DI**: o `InjecaoDependenciaApplicationExtensions` só sobe o
`MessageSenderBackgroundService` e o `LiberacaoPreparacaoBackgroundService`, então isso nunca roda.
Ainda usa `DateTime.Now`, contra a regra do `CLAUDE.md`.

**Decidido no `B1.1`: ele sai.** Como cada varredura ganhou `BackgroundService` próprio — cadência
própria, configuração própria, falha isolada —, ele não vira a casa de coisa nenhuma. Resta apagar
o arquivo.

**C12 · `HistoricoStatusLocacao` não é alimentado por ninguém** — `M` · RN-62
A entidade, a configuração e o mapper existem; nenhuma transição de `Locacao` grava linha nela.
Com o ciclo de vida do `A1` no lugar são oito estados e nove transições, e a trilha continua
vazia — mesmo buraco de auditoria que a RN-37 fechou do lado do veículo, e o `MovimentoVeiculo`
serve de molde pronto. Depende do `C7` para o autor deixar de ser `"SYSTEM"`.

---

# Front

## Bloco D — o menu promete e a página não existe

Todo item aqui é rota que hoje devolve 404 a partir do menu ou da Home.

**F1 · Módulo de locações** — `G` · o maior item da lista
`/locacoes`, `/locacoes/nova`, `/locacoes/ativas`, `/locacoes/finalizadas`, visualizar. Não existe
`ILocacaoService` em `Front.Services`, nem `Request`/`Response`, nem validador — e o botão **"Nova
locação" da Home cai em 404**. É a tela do balcão: o sistema inteiro existe para ela.
Fatiar em quatro: (a) serviço + listagem na `TabelaGenerica`; (b) abertura, com reserva opcional;
(c) visualização; (d) finalizar e cancelar. Depende de `C3` para a listagem paginada, e de `A11`
se o fechamento chegar antes.

**F2 · Operação da locação aberta** — `G`
Abas de vistoria, adicionais, seguros, multas, pagamentos e caução. Todos os endpoints já existem
no `LocacoesController` — é front puro. Depende de `F1`.

**F3 · Módulo de adicionais** — `M`
`/adicionais` e `/adicionais/novo`, com ativar/desativar. A Api está 100% pronta (CRUD completo).
**Melhor relação esforço/entrega da lista inteira.**

**F4 · Módulo de multas** — `M`
`/multas` e `/multas/tipos`. Depende de `C1` e `C2`.

**F5 · Dashboard** — `M`
O menu tem "Dashboard → `/teste`", rota que não existe. Construir sobre
`GET api/v1/veiculos/indicadores`: utilização real, tempo médio de preparação, preparações em
aberto e tempo por situação. **Leia as duas ressalvas do doc `08` §Estado antes de rotular os
números** — a utilização é física e não comercial, e `VeiculosComTrilha` vem abaixo de
`VeiculosNoRecorte` enquanto a trilha for recente.

O endpoint cresceu com o `B6`: além da utilização e do tempo de preparação, ele agora devolve
bloqueios vencidos, transições sem documento e tentativas de sobreposição recusadas por filial. Os
três são de **controle**, não de operação, e pedem tratamento visual diferente — em especial
`TransicoesSemDocumento`, que tem que dar zero e portanto só merece destaque quando **não** dá.

**F6.1 · Bloqueio, transferência e desmobilização do veículo** — `M`
Nasceu com o bloco B: `POST veiculos/{id}/bloquear`, `PATCH .../bloqueios/{id}/liberar`,
`GET .../bloqueios`, `POST .../transferencias` (+ chegada e cancelar), `GET .../transferencias` e
`PATCH .../desmobilizar`. **Nenhum tem tela nem método no `IVeiculoService` do front**, então hoje
o gerente de frota não consegue bloquear nem transferir carro pelo sistema.

A lista de bloqueios cabe na `TabelaGenerica`, dentro de `VisualizarVeiculo`, ao lado da trilha do
`F6`. Os três formulários pedem funcionário responsável — e o front ainda não tem seletor de
funcionário.

**F6 · Trilha do ativo** — `M`
`GET veiculos/{id}/movimentos` na `TabelaGenerica`, dentro de `VisualizarVeiculo` — paginado, com
filtro por período e por tipo de documento. O endpoint existe e nenhuma tela o consome.

**F7 · Botão de liberar da preparação** — `P`
`PATCH veiculos/{id}/liberar-preparacao` não tem botão em lugar nenhum, e o `IVeiculoService` do
front não tem o método: **hoje o pátio não consegue devolver carro à oferta pelo sistema.** Item
pequeno com efeito operacional imediato.

**F8 · Vistoria de retirada e devolução** — `G`
Registro com hodômetro, nível de combustível, fotos e danos. É o que alimenta o fechamento do doc
`07` — sem vistoria nas duas pontas o fechamento bloqueia por regra. Depende de `C8`.

## Bloco E — correções e limpeza

**F9 · Porta da Api errada no front** — `P`
`ApiConfig:BaseUrlApiLocacao` em `Front/appsettings.Development.json` aponta para
`https://localhost:44310/`; a Api sobe em `https://localhost:61977` (`launchSettings.json`).

**F10 · Restos de template** — `P`
`Counter.razor`, `Weather.razor`, `Component.razor` e o arquivo
`Front.Services/Servicos/Novo(a) Documento de Texto.txt`.

**F11 · Confirmação em `CriarCategoria.razor:153`** — `P`
O `// TODO: Implementar diálogo de confirmação` continua lá, e o `ConfirmDialog.razor` já existe.

**F12 · Ações que faltam na reserva** — `P`
Finalizar e expirar-vencidas não têm botão na listagem, embora o `IReservaService` do front já
tenha os dois métodos. Falta também o atalho **reserva → abrir locação** (o `CriarLocacaoDto` já
aceita `idReserva`).

**F13 · Usuários e roles** — `M`
`UserDto` e `RoleResponse` existem sem serviço e sem tela. Só faz sentido depois de `C7`.
