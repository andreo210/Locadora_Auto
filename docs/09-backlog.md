# 09 — Backlog

> **Este documento não é especificação nem descrição do sistema.** É a fila de trabalho: o que
> está aberto, em que ordem faz sentido pegar e onde encostar a mão. Cada item aponta a origem —
> uma RN das especificações `07`/`08`, uma armadilha registrada no `CLAUDE.md`, ou o próprio
> código. Quando um item for concluído, risque a linha aqui e atualize o documento de origem.

Estado da base em 18/08/2026: a especificação `08` (invariante do ativo) está **implantada
inteira** — RN-35 a RN-56 mais os seis indicadores da seção 12. O que sobrou dela não é regra, é
tela: nenhum dos endpoints do ativo tem consumidor no front (`F5`, `F6`, e agora também bloqueio,
transferência e desmobilização).

Da `07` (fechamento financeiro) estão de pé **o ciclo de vida do contrato** (`A1`) — devolução e
fechamento deixaram de ser o mesmo ato — e agora também **os dados que a apuração vai precisar**
(`A2`, `A3`): os valores congelados na abertura e os parâmetros da casa. Mas nenhuma apuração:
`Fechar` continua recebendo `valorFinal` pronto de quem chama. **É o buraco funcional do sistema, e
segue sendo o único bloco de regra aberto na Api.**

**Tamanhos:** `P` = uma sessão · `M` = duas a três · `G` = fatiar antes de começar.

## Por onde pegar

Três frentes independentes, para escolher pelo tempo disponível e não pela ordem da lista:

| Frente | Primeiro item | Por quê |
|---|---|---|
| Entrega visível rápida | **F3** (Adicionais) → **F7** (liberar preparação) → **F6** (trilha) | Api já pronta; é só front consumindo endpoint existente. Com o bloco B fechado, esta frente cresceu: bloqueio, transferência e desmobilização também são só tela |
| Fio principal | **A9** (avarias e multas) → **A10** (composição e caução) → **A11**/**A12** | É o buraco funcional do sistema: hoje o valor da devolução é digitado. Sete dos oito grupos de linha já são apurados; falta o que deu errado, a composição e a porta da Api |
| Dívida que trava outras | **C3** (locações paginadas) → **C1**/**C2** (multa) → **C8** (leitura de vistoria) | Cada um destrava uma tela do front |

O **F1** (módulo de locações no front) é o maior item da lista inteira e depende de `C3`. Não
comece por ele num dia curto.

---

# API

## Bloco A — fechamento financeiro (doc `07`)

`A1` a `A8` estão feitos: o ciclo de vida do contrato, os valores congelados, os parâmetros da casa,
a conta discriminada e a apuração de **período, quilometragem, combustível, proteção, acessórios e
taxas**. Falta o que deu errado — avaria e multa — e a composição com a caução.

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

~~**A2 · Campos congelados no contrato** — `M` · RN-06, RN-14, RN-18, RN-21, RN-22, RN-25~~ **feito.**
Entraram `Locacao.ValorDiariaContratada`, `LocacaoSeguro.ValorDiariaContratada` e
`FranquiaContratada`, `Veiculo.CapacidadeTanqueLitros`, `Filial.HabilitadaOneWay` e
`TaxaRetornoOneWay`, na migration `CamposCongeladosEParametrosDeFechamento`. Nenhum é lido por
cálculo nenhum ainda — é só o dado, como previsto.

~~**A3 · Parâmetros da casa** — `P` · RN-03, RN-04, RN-15, RN-23~~ **feito.**
`ToleranciaMinutos`, `PercentualHoraExcedente`, `PrecoLitroCombustivel`,
`TaxaServicoAbastecimento` e `ValorLimpezaEspecial`, na mesma migration.

A decisão pendente era por filial ou global, e ficou **por filial**, junto com os dois do one-way:
é onde o `TempoPreparacaoMinutos` já mora, pelo mesmo motivo — preço de litro e custo de limpeza
variam de praça para praça, e quem conhece o número é quem opera. Tolerância e percentual de hora
excedente na prática se repetem na rede inteira, e para isso existe o default. Todos entram por
`Filial.DefinirParametrosFinanceiros`, onde **nulo mantém o valor atual**, mesma escolha do tempo de
preparação: o Front de hoje não conhece esses campos, e sem isso uma edição de nome de filial
zeraria o preço do litro da praça.

Quatro decisões que valem para quem for pegar o resto do bloco A:

- **Onde não há padrão da casa, o default é zero — e zero significa "não configurado", não "de
  graça".** Tolerância nasce em 30 min e hora excedente em 1/3 da diária (doc `07` §9), porque
  esses números são conhecidos. Preço do litro, taxa de abastecimento, limpeza e taxa de one-way
  nascem zerados, e é o `A6`/`A8` que decide se avisa ao cobrar zero. Vale aqui a mesma escolha da
  RN-14 sobre tanque não cadastrado: melhor perder a cobrança que inventar número.
- **`HabilitadaOneWay` nasce `true`.** O sistema aceita devolução em qualquer filial hoje —
  `IdFilialDevolucao` sempre foi livre. Nascer `false` faria o `A8`, ao entrar, bloquear no balcão
  um serviço que a casa vende hoje na rede inteira, até alguém habilitar filial por filial.
- **De qual filial a apuração lê cada parâmetro** (não implementado, é do `A5`–`A8`): termo de
  contrato — tolerância e hora excedente — sai da filial de **retirada**, que vendeu; custo de
  execução — combustível, limpeza, one-way — sai da filial de **devolução**, que gastou. A taxa de
  one-way é do destino porque é ele que fica com um carro que não vendeu.
- **A diária congelada é parâmetro explícito de `Locacao.Criar`, não `veiculo.Categoria.ValorDiaria`
  lido lá dentro.** A navegação chega nula em qualquer chamador que não peça o `Include`, e o
  contrato nasceria com diária zero — defeito que só apareceria no fechamento, semanas depois. Por
  isso o `LocacaoService` ganhou o `ICategoriaVeiculosRepository`: busca por repositório funciona
  no `RepositorioFake`, `Include` não.

Fica um débito conhecido: **tolerância e percentual de hora excedente não são congelados no
contrato**, só a diária é. Os dois são termo contratual tanto quanto o preço, e mudar a política
com contrato aberto muda a conta de quem já assinou. O corte atual aceita isso porque contrato fecha
em dias e esses parâmetros quase não mudam — mas é decisão a revisitar no `A5`, que é quem vai lê-los.

~~**A4 · Entidades `FechamentoLocacao` e `LinhaFechamento`** — `M` · RN-31, RN-33~~ **feito.**
Entraram as duas entidades, os enums `TipoLinhaFechamento` e `NaturezaLinhaFechamento`, e a
migration `FechamentoDiscriminado`. O ciclo é
`AbrirFechamento` → `LancarNoFechamento`* → `SelarFechamento` → `CorrigirFechamento`*, tudo pela
`Locacao` — as entidades têm `Criar`/`Lancar` `internal`, pela convenção do agregado.

**Nada calcula.** O A4 é o livro em que a apuração vai escrever; o que ele garante é a forma.

Cinco decisões que valem para quem for pegar o `A5` em diante:

- **Crédito é tipo de linha, não valor negativo.** `Total` é sempre positivo e o sinal sai da
  `Natureza`, derivada do `Tipo` (`PagamentoAbatido` e `Isencao` abatem; o resto cobra). Guardar
  valor negativo faria a mesma informação existir em dois lugares — o sinal e o tipo —, que é como
  um extrato passa a cobrar o que deveria devolver. `Natureza` **não tem coluna** pelo mesmo
  motivo, e há teste de modelo fixando isso.
- **Os totais acumulam linha a linha, não por `Sum()` sobre a coleção.** As linhas só existem em
  memória se alguém pediu o `Include`, e um total recalculado sobre coleção parcialmente carregada
  sairia menor que o real — em silêncio, num campo de dinheiro. Só é correto porque a coleção é
  **append-only**: nenhuma linha muda de valor e nenhuma é removida.
- **`BaseCalculo` é texto obrigatório** ("franquia de 600 km sobre 3 diárias; rodados 750 km"). É o
  que sustenta a cobrança contestada, e o doc `07` §9 fecha com "não faça em cenário nenhum: cobrar
  linha sem documento de suporte".
- **A selagem é a fronteira.** Antes dela linha nova é cálculo; depois, é correção, e aí exige autor
  e motivo (RN-34). Selar exige ao menos uma linha — a RN-02 garante o mínimo de uma diária em
  qualquer contrato, então conta vazia só pode ser apuração que não rodou.
- **O A4 não encosta em `Fechar`, `Status` nem `ValorFinal`.** O fechamento discriminado e o ciclo
  de vida do contrato ainda correm em paralelo; juntá-los é o `A10`.

Duas armadilhas registradas:

- O `xmin` que o EF gerou nos dois `CreateTable` foi **removido à mão**, como nas migrations do
  bloco B — `xmin` é coluna de sistema do Postgres e declará-la no `CREATE TABLE` falha. Se a
  migration for regerada, apague de novo.
- **O `A10` vai precisar relaxar a guarda `valorFinal < 0` do `Locacao.Fechar`.** A RN-29 permite
  saldo negativo, que é crédito a devolver ao cliente; hoje `Fechar` o recusaria, e o
  `FechamentoLocacao` já sabe produzi-lo.

~~**A5 · Apuração do período** — `M` · RN-01 a RN-07~~ **feito.**
`ApuracaoDePeriodo.Calcular` faz a conta e `Locacao.ApurarPeriodo(filialRetirada)` escreve as
linhas — `Diaria`, `HoraExcedente` e, quando o teto entra, `DiariaPorTetoDeHoras`. Os quatro
cenários gherkin do doc `07` §10 estão em `ApuracaoDePeriodoTests`, literais.

Quatro decisões que valem para o `A6` em diante:

- **A tolerância é tempo livre, e as horas contam a partir dela** — não do fim do ciclo. 2h30 de
  sobra com 30 min de tolerância dão **2** horas excedentes, não 3, que é o que o cenário 3 do
  doc `07` §10 fixa. Era a única leitura das RN-03/RN-04 que fecha com os critérios de aceite.
- **A diária mínima da RN-02 cobre o primeiro ciclo inteiro.** Contrato de 22h é uma diária e nada
  mais; cobrar hora excedente sobre esse resto seria cobrar o mesmo período duas vezes.
- **O valor da hora excedente é arredondado a 2 casas antes de virar linha.** Não é enfeite: a
  coluna é `numeric(10,2)`, então um unitário com mais casas seria arredondado pelo banco e a linha
  gravada passaria a discordar do total que ela mesma declara. Com o padrão da casa é também o que
  faz a conta bater — 150 × 0,3333 = 49,995, que vira 50,00 e devolve o "1/3 da diária" prometido.
- **A divisão do período é sobre `Ticks`, não sobre `TotalHours`.** `TotalHours` é `double`, e um
  contrato de exatamente 48h pode sair como 47,999999999 — que viraria uma diária a menos na conta
  do cliente, sem ninguém entender por quê.

`ApurarPeriodo` recebe a **`Filial` de retirada em objeto**, e não os dois parâmetros soltos, por
dois motivos: `Locacao.FilialRetirada` chega nula em qualquer chamador que não peça o `Include`, e
dois `decimal` na assinatura são dois argumentos que alguém troca de ordem um dia sem o compilador
reclamar. A guarda de identidade recusa filial que não seja a de retirada do contrato.

Um erro do próprio doc `07` foi corrigido: o cenário 4 do §10 dizia "1 diária cheia no lugar das
**3** horas excedentes", mas 4h de sobra menos 30 min de tolerância dão 3h30, que por hora iniciada
são **4** horas. O resultado que importa — 3 diárias, R$ 450,00 — não muda.

~~**A6 · Quilometragem e combustível** — `M` · RN-08 a RN-16~~ **feito.**
`ApuracaoDeQuilometragem` e `ApuracaoDeCombustivel` fazem as contas;
`Locacao.ApurarQuilometragem(veiculo, categoria, periodo)` e
`Locacao.ApurarCombustivel(veiculo, filialDevolucao)` escrevem as linhas. Os quatro cenários de km e
combustível do doc `07` §10 estão nos testes.

Cinco decisões que valem para o `A7` em diante:

- **O hodômetro e o nível saem da vistoria, não do contrato** (RN-11). `Locacao.KmInicial`/`KmFinal`
  guardam os mesmos números, mas quem os informou foi quem abriu e quem recebeu — a medição que
  sustenta a cobrança é a que foi feita com o carro à frente de quem assina. Há teste com os dois
  divergindo de propósito.
- **Falta de cadastro no combustível não bloqueia: vira linha de R$ 0,00 que se explica.** Tanque
  não cadastrado e preço do litro zerado produzem `SituacaoDoCombustivel` própria, e a base de
  cálculo da linha diz o motivo. É melhor que uma notificação, porque fica no extrato para sempre.
  A `Situacao` devolvida é o que permite a quem chama avisar alguém além disso.
- **Combustível e taxa de serviço são linhas separadas.** Litro é insumo e taxa é serviço — coisas
  diferentes na conta do cliente —, e o indicador de receita acessória do doc `07` §12 só fecha se
  puderem ser contadas à parte. O §10 falava em "a linha de combustível de R$ 188,80"; são duas,
  somando o mesmo.
- **A linha de km é escrita mesmo valendo zero**, em km livre ou dentro da franquia. A linha zerada
  diz ao cliente que a quilometragem foi apurada e não gerou cobrança; a ausência dela não diz.
- **Km bloqueia onde combustível não bloqueia.** Hodômetro menor na devolução e categoria com
  limite sem preço param a apuração, porque não há resposta segura — cobrar zero esconderia a
  adulteração, e cobrar sem preço inventaria número.

**Dois defeitos achados e corrigidos no caminho**, ambos na `CategoriaVeiculo`:

- **A quilometragem livre da RN-08 não era cadastrável.** As colunas sempre foram anuláveis e os
  DTOs também, mas `Criar`/`Atualizar` exigiam número e o serviço fazia `dto.LimiteKm.Value` por
  cima — quem omitisse o campo levava **500**. Agora `LimiteKm` nulo é km livre, `ValorKmExcedente`
  é descartado quando não há limite, e o serviço notifica em vez de deixar a entidade lançar.
- **`int.IsPositive(0)` é `true`**, então `limiteKm = 0` passava pela guarda antiga e criava
  categoria com franquia zero — toda a rodagem virando excedente. Trocado por comparação explícita,
  como o resto do repositório já faz.

Fica um débito para o `A11`: `Locacao.KmFinal` e o hodômetro da vistoria de devolução são hoje dois
registros do mesmo número, e nada garante que concordem. Ou `RegistrarDevolucao` deixa de receber
`kmFinal` e passa a lê-lo da vistoria, ou a divergência vira aviso — o que não pode é seguir sem
ninguém olhar.

~~**A7 · Proteções e acessórios** — `M` · RN-17 a RN-20~~ **feito.**
`ApuracaoDeProtecao` faz a conta da RN-18/RN-19; `Locacao.ApurarProtecoes(periodo)` e
`Locacao.ApurarAcessorios(periodo)` escrevem as linhas, **uma por proteção e uma por acessório**.
Migration `JanelaDaProtecao`.

**A RN-19 exigia duas colunas que não existiam.** `LocacaoSeguro.Ativo = false` dizia que a proteção
foi cancelada, mas não quando — e sem o quando não há pró-rata, só a escolha entre cobrar o contrato
inteiro (o cliente reclama com razão) ou não cobrar nada (a casa perde o que cobriu). Entraram
`DataContratacao` e `DataCancelamento`.

Cinco decisões que valem para o `A8` em diante:

- **`DataContratacao` é a data de início do contrato quando a proteção é vendida no balcão**, e
  `UtcNow` quando é vendida com o carro já na rua. Usar sempre `UtcNow` faria a proteção de balcão
  nascer alguns segundos depois da retirada, e a pró-rata entraria onde não devia — cobrando
  2,9986 diárias num contrato de 3.
- **Cobertura integral cobra exatamente `DiariasCobradas`** (RN-18), sem passar pela conta
  proporcional. Só cobertura parcial é pró-rata, limitada por cima às diárias do contrato e por
  baixo a zero. A proteção também acompanha a diária que o teto da RN-05 acrescentou.
- **`Cancelar()` não aceita data**: a cobertura acaba agora, nunca retroativa — mesma decisão da
  liberação de bloqueio da RN-52. Datar para trás devolveria ao cliente dias em que ele esteve
  coberto.
- **Uma linha por proteção e por acessório, nunca uma soma.** Um contrato pode ter tido mais de uma
  proteção ao longo da vida (cancelar libera contratar outra), cada uma com sua janela; e o extrato
  precisa dizer o que é cadeirinha e o que é GPS, senão o cliente contesta o bloco inteiro.
- **Sem proteção ou sem acessório não há linha zerada**, ao contrário do km: nunca houve o que
  apurar, e "proteção: R$ 0,00" no extrato de quem não contratou proteção só confunde.

`LocacaoAdicional.Dias` **continua guardando o que foi vendido** — o fechamento recalcula pelas
diárias efetivas sem reescrever o registro da venda, que responde outra pergunta.

A migration não usa o `DEFAULT '0001-01-01'` que o EF gerou para a coluna obrigatória: ela nasce
anulável, é preenchida com `tb_locacao.data_inicio` e só então vira `NOT NULL`. Data de ano 1 num
`timestamptz` ficaria na definição da coluna para sempre.

**RN-20 não é implementada aqui.** "Proteção não cobre combustível, limpeza, multa nem km excedente"
é restrição sobre o `A9`: a franquia limita **avaria** (RN-25) e nada mais. As outras linhas já saem
sem consultar proteção nenhuma, então o que o `A9` não pode fazer é começar a consultar.

~~**A8 · Taxas** — `P` · RN-21 a RN-23~~ **feito.**
`Locacao.ApurarTaxaOneWay(filialDevolucao, idFuncionarioAlcada?, motivoAlcada?)` e
`Locacao.ApurarLimpezaEspecial(filialDevolucao)`. Migration `LimpezaEspecialNaVistoria`.

**Nenhuma das duas tem tipo de apuração próprio**, ao contrário do `A5`–`A7`: não há cálculo, o
valor sai pronto da filial de destino. O que existe aqui é decisão — quando cobrar, e o que fazer
quando a regra diz não.

**A RN-23 exigia um campo que não existia.** "Registro na vistoria de devolução" não tinha onde ser
gravado: entrou `Vistoria.RequerLimpezaEspecial`, com a declaração restrita à vistoria de devolução
(na retirada o carro sai limpo). O campo foi até o `CriarVistoriaDto` na mesma mudança — sem isso a
RN-23 ficaria inalcançável pela Api, como o km livre estava no `A6`.

Quatro decisões:

- **A alçada da RN-22 assina a linha.** Filial de destino não habilitada bloqueia o fechamento; a
  saída é `idFuncionarioAlcada` + `motivoAlcada`, que ficam gravados na própria linha. O carro já
  está no pátio dela — recusar para sempre não é opção, liberar sem quem responda também não. Alçada
  pela metade (só autor ou só motivo) continua bloqueando.
- **One-way de cortesia ainda escreve linha.** Taxa zerada é decisão comercial, não ausência de
  evento: o carro foi devolvido longe de onde saiu, e o extrato registra. Já a devolução na
  **própria** filial não escreve nada — ali não houve one-way nenhum.
- **Limpeza exige declaração _e_ foto, as duas.** A declaração sozinha é a palavra do vistoriador
  contra a do cliente; a foto sozinha não diz que a sujeira era especial. Faltando qualquer uma, não
  há linha — sujeira comum é custo da operação.
- **Filial com `ValorLimpezaEspecial` zerado não cobra**, mesmo com declaração e foto: zero é "não
  parametrizado", como o preço do litro no `A6`.

**Um defeito do `A4` corrigido:** `LinhaFechamento` descartava o `IdFuncionarioLancamento` quando
ele não era obrigatório, então a assinatura da alçada sumiria em silêncio. Agora o autor é guardado
sempre que informado, e continua **exigido** em correção e isenção.

**A9 · Avarias e multas no fechamento** — `M` · RN-24 a RN-26 · **é por aqui que se começa**
Atenção à RN-20: a franquia limita **avaria** e nada mais. Combustível, limpeza, multa e km já saem
sem consultar proteção — o que não pode é o `A9` começar a consultar.
Só entram avarias em `Aprovado` ou `Cobrado`; `Registrado`/`EmAnalise` vão para o pós-contrato.
Havendo proteção, a cobrança ao cliente é limitada à franquia contratada **somando todas as
avarias**, não por avaria. Multa `Pendente` conhecida entra; recebida depois não reabre.

**A10 · Composição, caução e idempotência** — `G` · RN-27 a RN-34
Caução resolvida **depois** do saldo, e a ligação entre o fechamento discriminado e o ciclo de vida
do contrato: hoje `FechamentoLocacao.Saldo` e `Locacao.ValorFinal` correm em paralelo, e é aqui que
`Fechar` passa a ler o saldo em vez de recebê-lo. **Relaxar a guarda `valorFinal < 0` do `Fechar`
faz parte:** RN-29 permite saldo negativo.
O que o `A4` já entregou: total, natureza de crédito, saldo que não trunca, e a idempotência da
abertura (`AbrirFechamento` devolve a conta existente, com índice único no banco por trás).
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

**F14 · Campos do fechamento no cadastro de filial e de veículo** — `P` · nasceu do `A2`/`A3`
`CriarFilial.razor` e `EditarFilial.razor` não têm os sete parâmetros de fechamento
(one-way habilitado e taxa, tolerância, percentual de hora excedente, preço do litro, taxa de
abastecimento, limpeza especial); `CriarVeiculo.razor` e `EditarVeiculo.razor` não têm a capacidade
do tanque. **Hoje só dá para configurar isso pelo Swagger**, e enquanto for assim toda filial fica
com preço de litro zero e toda frota sem tanque cadastrado — o que faz o `A6`/`A8`, quando entrar,
não cobrar combustível nem limpeza de ninguém.

Os `Request` do front são anuláveis do lado da Api de propósito (ausente mantém o valor atual), então
a tela pode entrar aos poucos sem quebrar nada. Um agrupamento "Parâmetros de fechamento" recolhido
no formulário de filial resolve — não é campo de uso diário.

**F12 · Ações que faltam na reserva** — `P`
Finalizar e expirar-vencidas não têm botão na listagem, embora o `IReservaService` do front já
tenha os dois métodos. Falta também o atalho **reserva → abrir locação** (o `CriarLocacaoDto` já
aceita `idReserva`).

**F13 · Usuários e roles** — `M`
`UserDto` e `RoleResponse` existem sem serviço e sem tela. Só faz sentido depois de `C7`.
