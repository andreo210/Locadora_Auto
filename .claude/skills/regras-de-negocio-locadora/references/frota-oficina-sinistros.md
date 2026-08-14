# Frota, oficina e sinistros

A frota é o ativo. Numa locadora, o resultado do exercício se decide muito mais na compra, na manutenção e na venda do carro do que na tarifa da diária.

## Índice

- [O ciclo de vida do ativo](#o-ciclo-de-vida-do-ativo)
- [Compra e entrada na frota](#compra-e-entrada-na-frota)
- [Depreciação](#depreciação)
- [Disponibilidade, utilização e giro](#disponibilidade-utilização-e-giro)
- [Idade média e vida útil](#idade-média-e-vida-útil)
- [Documentação obrigatória](#documentação-obrigatória)
- [Rastreamento e telemetria](#rastreamento-e-telemetria)
- [Desmobilização e venda](#desmobilização-e-venda)
- [Manutenção preventiva](#manutenção-preventiva)
- [Manutenção corretiva](#manutenção-corretiva)
- [Ordem de serviço](#ordem-de-serviço)
- [Peças, fornecedores e garantia](#peças-fornecedores-e-garantia)
- [Recall](#recall)
- [Sinistros](#sinistros)
- [Avaria x sinistro](#avaria-x-sinistro)

---

## O ciclo de vida do ativo

```
COMPRA ─▶ EMPLACAMENTO ─▶ PREPARAÇÃO ─▶ OPERAÇÃO ─▶ DESMOBILIZAÇÃO ─▶ VENDA
                                          │  ▲
                                          ▼  │
                              MANUTENÇÃO / SINISTRO / TRANSFERÊNCIA
```

Cada seta é um evento com data, custo e responsável. Um sistema que só conhece "disponível/locado" perde o controle do ativo justamente onde o dinheiro se decide.

**Status de veículo que uma operação real precisa distinguir** (o modelo deste repositório tem quatro; a lista abaixo é o que o mercado usa, e a diferença vale ser discutida):

disponível · reservado · locado · em preparação (limpeza/abastecimento) · em manutenção preventiva · em manutenção corretiva · aguardando peça · sinistrado · em transferência entre filiais · bloqueado (documental/comercial) · em desmobilização · vendido.

Agrupar tudo em "indisponível" é o que faz o indicador de utilização mentir: não se distingue carro parado por escolha (desmobilização) de carro parado por falha (aguardando peça), e o gestor não sabe onde atacar.

## Compra e entrada na frota

Formas de aquisição, cada uma com consequência contábil e de caixa diferente:

| Forma | Efeito | Quando faz sentido |
|---|---|---|
| Compra à vista | imobiliza caixa, ativo próprio | caixa sobrando, taxa alta |
| Financiamento com gravame | ativo próprio, dívida no passivo, alienação fiduciária | o mais comum |
| Consórcio | previsibilidade, sem juros, sem data | crescimento planejado |
| Leasing/arrendamento | ativo de terceiro, parcela como despesa | depende do tratamento contábil vigente |
| Compra direta de fábrica (venda direta a locadora) | desconto relevante, condições de recompra | frota média e grande |

Locadora grande negocia **volume + condição de recompra** com a montadora, o que muda o cálculo inteiro: a depreciação passa a ser quase contratual.

Entrada na frota exige: nota fiscal, emplacamento e Renavam, primeiro licenciamento, seguro/inclusão em apólice, instalação de rastreador, adesivagem, preparação e vistoria inicial. **O carro só entra na oferta quando termina isso** — contar com ele antes gera reserva que não se cumpre.

## Depreciação

O maior custo de uma locadora, e o menos visível porque não sai do caixa.

- **Depreciação contábil** — pelo método e vida útil adotados na contabilidade.
- **Depreciação fiscal** — pelas regras fiscais vigentes, que podem diferir da contábil (confirme com o contador; muda com a legislação).
- **Depreciação de mercado** — a que realmente importa para decidir quando vender: a diferença entre o valor de aquisição e o preço realizável no seminovo, dividida pelos meses de uso.

A conta que o gestor de frota olha todo mês:

```
depreciação mensal por unidade = (valor de aquisição − valor de venda estimado) ÷ meses em frota
```

Se a depreciação mensal por unidade for maior que a margem da locação daquele carro, o veículo está destruindo valor mesmo com utilização alta. É por isso que **giro de frota é decisão financeira, não operacional**.

Fatores que aceleram a depreciação: quilometragem alta, avarias reparadas, cor impopular, versão de entrada, mudança de geração do modelo, elevação de estoque no mercado de seminovos.

## Disponibilidade, utilização e giro

- **Frota total** — tudo que está no ativo.
- **Frota operacional** — o que pode ser alugado (exclui sinistro, desmobilização, bloqueio documental).
- **Frota disponível** — operacional e não comprometida agora.

```
utilização (%) = diárias faturadas ÷ (frota operacional × dias do período)
disponibilidade (%) = frota operacional ÷ frota total
```

Utilização calculada sobre frota total esconde carro parado; sobre frota operacional, mede a venda. Reporte as duas — a diferença entre elas é exatamente o custo do carro parado.

**Giro** é a velocidade com que a frota se renova (entradas e saídas no período). Giro alto reduz idade média, custo de manutenção e risco de quebra, mas aumenta a exposição ao mercado de seminovos e ao custo de aquisição.

## Idade média e vida útil

- **Idade média** em meses, ponderada por unidade. É o indicador que antecipa custo de manutenção: a curva de custo sobe visivelmente depois do fim da garantia de fábrica.
- **Vida útil operacional** na locação de varejo costuma ser curta — a decisão de venda quase sempre é tomada antes do fim da garantia ou perto dele, porque é quando o carro ainda vale bem no seminovo e ainda não custa manutenção pesada.
- **Quilometragem de saída** costuma ser o gatilho principal, junto com a idade.

Regra de gestão: definir a janela de desmobilização (por idade **e** por km) e monitorar mensalmente quantos carros estão fora da janela. Frota envelhecida é dívida técnica que aparece como custo de oficina e queda de tarifa.

## Documentação obrigatória

Controle com alerta de vencimento, porque o veículo com pendência não pode rodar e a multa é da locadora:

- Licenciamento anual (CRLV) e IPVA por exercício.
- Seguro obrigatório conforme legislação vigente.
- Apólice de frota / cobertura contratada.
- Gravame e quitação, quando financiado.
- Inspeções exigidas por regulação local, quando aplicável.
- Adaptações e acessórios homologados (engate, PCD).

Regra dura: **veículo com documento vencido sai da oferta automaticamente**. Depender de alguém lembrar é garantia de que um dia vai rodar irregular.

## Rastreamento e telemetria

Cada vez mais padrão, inclusive em frota média:

- **Rastreamento** — recuperação em roubo/furto, localização de veículo não devolvido, bloqueio remoto (com cuidado jurídico: bloquear veículo em movimento é risco grave; a prática é bloquear apenas a partida, e sempre com respaldo contratual).
- **Telemetria** — hodômetro automático, nível de combustível, alerta de manutenção, comportamento de condução, cerca eletrônica (uso fora da região contratada).
- **Ganhos concretos**: hodômetro sem digitação errada, preventiva disparada por km real, recuperação mais rápida, prova em disputa de multa.
- **Cuidado de LGPD**: telemetria é dado pessoal do condutor. Exige base legal, informação clara no contrato e limite de finalidade — monitorar posição é diferente de perfilar comportamento (ver `indicadores-atendimento-compliance.md`).

## Desmobilização e venda

Processo, não evento:

1. Seleção pelos critérios (idade, km, custo acumulado de manutenção, demanda do grupo).
2. Bloqueio da oferta e retirada da agenda.
3. Preparação para venda: laudo, reparo estético que se paga, limpeza, documentação.
4. Canal de venda: loja própria de seminovos, leilão, atacado (comprador de lote), varejo direto ao consumidor, funcionário.
5. Transferência de propriedade, baixa do ativo, apuração do resultado da venda.

O **canal muda muito o resultado**: varejo direto rende mais por unidade e demora mais (custo de estoque e de capital); atacado/leilão rende menos e libera caixa imediatamente. Locadora com pressão de caixa vende no atacado e não deveria confundir isso com "o carro valia pouco".

Tratamento fiscal da venda de veículo de locadora tem particularidades (inclusive benefícios condicionados a tempo mínimo de permanência no ativo). **Isso varia por estado e por norma vigente — confirme com o contador antes de assumir qualquer coisa no sistema.**

## Manutenção preventiva

Disparada por **quilometragem ou tempo, o que vier primeiro**. É o que preserva garantia de fábrica e valor de revenda.

Itens típicos: troca de óleo e filtros, revisão de fábrica no plano, pneus (rodízio, alinhamento, balanceamento, troca por sulco/idade), freios, fluidos, correia/corrente, bateria, ar-condicionado, palhetas.

Regras que fazem diferença:

- **Agendar preventiva em vale de demanda**, não no pico. Carro em revisão na alta temporada é receita perdida duas vezes.
- **Bloquear a oferta** para a janela da revisão, com data de retorno prevista.
- **Não perder a revisão de garantia** — atraso invalida cobertura de fábrica e joga custo de motor/câmbio para a locadora.
- **Pneu tem critério objetivo** (profundidade do sulco, idade, dano estrutural). Deixar a decisão para o feeling do borracheiro gera custo e risco.
- Registrar hodômetro em toda OS: é ele que dispara a próxima preventiva.

## Manutenção corretiva

Disparada por falha, avaria ou reprovação em vistoria. O que a operação precisa controlar:

- **Tempo de imobilização** (dias parado), que é o custo real — a peça custa menos que o carro parado.
- **Aguardando peça** como status próprio, porque a causa é externa e o tratamento é outro (fornecedor, estoque mínimo, peça paralela).
- **Reincidência**: mesmo defeito no mesmo veículo indica reparo malfeito ou problema estrutural. Sem contar reincidência, a oficina "resolve" duas vezes e cobra duas vezes.
- **Responsabilidade**: defeito mecânico é custo da locadora; dano por mau uso do cliente é cobrança do cliente. A definição precisa estar no laudo, não na conversa.

## Ordem de serviço

A OS é o documento que amarra custo, ativo e responsabilidade. Campos que precisam existir:

veículo · hodômetro na abertura · tipo (preventiva/corretiva/revisão/funilaria/pneu) · origem (preventiva agendada, vistoria, sinistro, reclamação do cliente) · sintoma relatado · diagnóstico · serviços executados · peças com quantidade e valor · mão de obra · fornecedor/oficina · datas de abertura, autorização, entrada, conclusão e liberação · valor orçado e valor final · aprovador · garantia do serviço.

Regras:

- **Orçamento acima de um limite exige aprovação** de alçada — sem isso, custo de oficina cresce sem controle.
- **Valor final divergente do orçado** precisa de justificativa registrada.
- **O veículo só volta à oferta com a OS fechada** e com vistoria de retorno. Carro liberado com OS aberta gera cliente recebendo o mesmo defeito.
- Custo da OS acumula no histórico do veículo — é isso que alimenta custo por km e a decisão de desmobilizar.

## Peças, fornecedores e garantia

- **Peça genuína x paralela**: genuína preserva garantia de fábrica e valor de revenda; paralela reduz custo imediato. Enquanto o carro está na garantia, a escolha é quase sempre genuína — o risco de perder cobertura de motor supera a economia.
- **Garantia do serviço e da peça** precisa ser registrada com prazo/km: reincidência dentro da garantia é retrabalho do fornecedor, não custo novo.
- **Fornecedor** precisa de avaliação objetiva: prazo médio, retrabalho, preço, disponibilidade de peça. Escolher oficina por relacionamento sem medir prazo é como o custo de imobilização cresce silenciosamente.
- **Estoque mínimo** de itens de alta rotação (filtro, palheta, lâmpada, pneu do grupo dominante) reduz "aguardando peça", que é o status mais caro da frota.

## Recall

Campanha de fábrica. Não é opcional:

- Identificar os veículos afetados por chassi assim que a campanha sai.
- **Bloquear a oferta** dos afetados quando o recall envolver risco de segurança — alugar veículo com recall de segurança pendente é exposição séria, civil e reputacional.
- Agendar com a concessionária e registrar a execução no histórico do veículo (o comprador do seminovo vai consultar).
- Recall pendente na venda derruba o preço e pode inviabilizar a transferência.

## Sinistros

Processo próprio, com prazos externos e outro conjunto de atores.

**Tipos e o que muda em cada um:**

| Tipo | Particularidade |
|---|---|
| Colisão sem terceiros | avaliação de reparo x perda total; franquia do cliente conforme proteção |
| Colisão com terceiros | entra responsabilidade civil, terceiro lesado, possível ação judicial |
| Roubo / furto | exige boletim de ocorrência; prazo de espera antes da indenização; rastreador é decisivo |
| Perda total | indenização pelo valor definido na apólice; baixa do ativo; documentação específica |
| Enchente / alagamento | frequentemente tratado à parte na apólice; risco de perda total por dano elétrico |
| Incêndio | perícia; documentação e prazo próprios |
| Danos da natureza (granizo) | volume alto e simultâneo; capacidade de oficina vira gargalo |

**Fluxo padrão:**

```
OCORRÊNCIA ─▶ ACIONAMENTO (assistência/guincho) ─▶ ABERTURA DO SINISTRO
   ─▶ BOLETIM DE OCORRÊNCIA (quando aplicável) ─▶ REGULAÇÃO PELA SEGURADORA
   ─▶ decisão: REPARO ou PERDA TOTAL
        REPARO ─▶ OS de funilaria ─▶ retorno à frota
        PERDA TOTAL ─▶ indenização ─▶ baixa do ativo ─▶ reposição
   ─▶ APURAÇÃO COM O CLIENTE (franquia, diárias de indisponibilidade, exclusões)
```

**Regras que costumam pegar:**

- **Boletim de ocorrência** é exigência comum da seguradora em roubo, furto, colisão com terceiro e incêndio. Sem ele, negativa de cobertura — e o cliente precisa saber disso no momento da ocorrência, não depois.
- **Condutor não cadastrado, álcool, uso indevido** entram nas exclusões típicas: a seguradora nega e a conta volta inteira para a locadora, que cobra do cliente e frequentemente não recebe. É por isso que a regra do condutor adicional é levada a sério.
- **Franquia** é a participação do cliente, comunicada em valor antes da locação.
- **Diárias de indisponibilidade** (o carro parado durante o reparo) são cobradas do responsável em muitos contratos, com teto. Precisa estar escrito, com o limite.
- **Sub-rogação e recuperação de terceiros**: quando o culpado é outro, existe processo de recuperação. Locadora que não estrutura isso deixa dinheiro na mesa todo mês.
- **Salvado** (o que sobra do veículo em perda total) tem valor e destino definidos pela apólice.

**Autosseguro** é comum em frota grande: em vez de apólice ampla, a locadora retém o risco de dano ao próprio veículo e contrata apenas responsabilidade civil e coberturas catastróficas. Vantagem: elimina o prêmio, que é caro em frota grande. Desvantagem: exige fundo de reserva, disciplina e capacidade de absorver evento grande. Não é recomendável para frota pequena — um evento sério quebra a empresa.

## Avaria x sinistro

Distinção que muitos sistemas erram, com consequência prática:

| | Avaria | Sinistro |
|---|---|---|
| O que é | dano de menor monta identificado na vistoria (risco, amassado, vidro trincado, pneu) | evento com acionamento de seguro/assistência |
| Quando aparece | na devolução, comparando vistorias | durante o contrato |
| Quem apura | vistoriador + oficina (orçamento) | regulador da seguradora |
| Como cobra | valor do reparo, com teto e desconto conforme proteção | franquia + diárias de indisponibilidade |
| Prazo | dias | semanas a meses |
| Documento | laudo de vistoria + fotos + orçamento | aviso de sinistro, BO, laudo de regulação |

Neste repositório, `Dano` cobre a **avaria**; o processo de sinistro ainda não existe como entidade própria — quando a conversa for sobre colisão, roubo ou perda total, diga isso explicitamente, porque forçar sinistro dentro de `Dano` produz um modelo que não fecha com a seguradora.
