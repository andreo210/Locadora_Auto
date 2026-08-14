# Reserva e contrato de locação

O coração da operação. Reserva é promessa; contrato é entrega. Quase todo defeito grave de sistema de locadora nasce da confusão entre os dois.

## Índice

- [O ciclo completo](#o-ciclo-completo)
- [Disponibilidade](#disponibilidade)
- [Overbooking](#overbooking)
- [Bloqueios](#bloqueios)
- [Upgrade e downgrade](#upgrade-e-downgrade)
- [Canais de reserva](#canais-de-reserva)
- [No-show e cancelamento](#no-show-e-cancelamento)
- [Abertura do contrato](#abertura-do-contrato)
- [Caução](#caução)
- [Proteções e franquia](#proteções-e-franquia)
- [Quilometragem](#quilometragem)
- [Diária, tolerância e hora excedente](#diária-tolerância-e-hora-excedente)
- [Durante o contrato](#durante-o-contrato)
- [Devolução](#devolução)
- [Fechamento financeiro](#fechamento-financeiro)
- [Pós-contrato](#pós-contrato)

---

## O ciclo completo

```
COTAÇÃO ──▶ RESERVA ──▶ RETIRADA (check-out) ──▶ POSSE ──▶ DEVOLUÇÃO (check-in)
                │                                    │            │
                │                                    │            ▼
                ├─ cancelada                         │      FECHAMENTO FINANCEIRO
                └─ expirada / no-show                │            │
                                                     │            ▼
                                    extensão, troca de veículo,   PÓS-CONTRATO
                                    devolução antecipada,         (multa de trânsito,
                                    assistência, sinistro          avaria em análise,
                                                                   cobrança residual)
```

O erro mais comum de modelagem é parar em "devolução". Devolução é a vistoria; o contrato só morre no fechamento, e a obrigação do cliente sobrevive semanas depois dele.

## Disponibilidade

**A reserva consome saldo de grupo; o contrato consome placa.** Guarde esta frase: ela resolve metade das discussões.

Cálculo de disponibilidade de um grupo, numa filial, num intervalo:

```
disponível = frota operacional do grupo na filial
           − veículos em contrato aberto que atravessam o intervalo
           − veículos em manutenção / sinistro / desmobilização no intervalo
           − reservas confirmadas que atravessam o intervalo
           − bloqueios (transferência, evento, cliente corporativo)
           + devoluções previstas dentro do intervalo, menos o tempo de preparação
           + transferências de entrada confirmadas
```

Três detalhes que separam um cálculo honesto de um otimista:

1. **Tempo de preparação.** O carro devolvido às 10h não está disponível às 10h: precisa de vistoria, limpeza, abastecimento e, às vezes, revisão. Uma a três horas é a faixa comum; grupo premium leva mais. Ignorar isso produz uma agenda que não fecha na prática.
2. **Devolução prevista não é devolução realizada.** Contar com o carro que "deveria" voltar é assumir risco — atraso e não devolução são rotina. Grandes tratam isso estatisticamente; pequenas devem ser conservadoras.
3. **Frota operacional ≠ frota total.** Carro aguardando peça, em processo de venda ou sinistrado não é oferta.

## Overbooking

Vender mais do que se tem é prática deliberada e legítima do setor, calibrada por estatística de no-show e devolução antecipada — **desde que exista uma escada de solução**. Sem a escada, é só promessa quebrada.

Escada padrão, do mais barato para o mais caro:

1. **Upgrade gratuito** para o grupo superior disponível.
2. **Antecipar o carro de outra reserva** menos crítica (cliente local, contrato curto).
3. **Transferência entre filiais próximas**, com entrega ao cliente.
4. **Cross-rent**: alugar de outra locadora e repassar, absorvendo a diferença.
5. **Reacomodação com compensação** (táxi/app, desconto, cortesia).
6. **Cancelamento com indenização** — último recurso, com custo reputacional.

Taxa de overbooking se calibra por grupo, filial e dia da semana, sobre histórico de no-show. Nunca aplicar overbooking em grupo de baixa oferta (carro grande, automático, adaptado) — quando falta, não há para onde escalar.

**Impacto financeiro:** overbooking bem calibrado aumenta receita alguns pontos percentuais; mal calibrado, cada falha custa upgrade, cross-rent, reclamação pública e cliente corporativo perdido.

## Bloqueios

Retirar um veículo ou um período da oferta, com motivo registrado. Tipos:

- **Manutenção programada** (revisão de km, recall).
- **Transferência entre filiais** — o carro sai de uma oferta antes de entrar na outra.
- **Reserva corporativa/evento** — frota dedicada a um cliente ou a um período (feira, congresso).
- **Desmobilização** — carro em processo de venda.
- **Bloqueio comercial** — segurar oferta para vender mais caro no pico (usar com parcimônia).
- **Bloqueio técnico** — pendência documental, licenciamento vencido, gravame.

Bloqueio sem prazo de término é buraco: carro some da oferta e ninguém percebe. Todo bloqueio precisa de data prevista de liberação e de alguém responsável.

## Upgrade e downgrade

- **Upgrade de cortesia** — a locadora não tem o grupo vendido e entrega o superior sem cobrar. Custo dela; precisa ser registrado como cortesia (com motivo) para virar indicador, não anedota.
- **Upgrade vendido** — oferecido no balcão, cobrado por diária. Receita acessória relevante e legítima.
- **Downgrade** — entregar grupo inferior. Só com **aceite do cliente** e **devolução da diferença**. Downgrade imposto é descumprimento de contrato; sem o aceite formal, a reclamação é ganha pelo cliente.

Regra prática: nenhum upgrade/downgrade acontece sem registro no contrato do grupo vendido **e** do grupo entregue. É o par de campos que permite medir quanto a cortesia custou no mês.

## Canais de reserva

| Canal | Característica | Regra específica |
|---|---|---|
| **Online (site/app)** | sem atendente, cliente digita tudo | validação documental fica para o balcão; risco de fraude maior; pagamento antecipado reduz no-show |
| **Presencial (walk-in)** | cliente já no balcão | vira contrato quase imediatamente; costuma ter tarifa mais alta |
| **Telefone/central** | atendente registra | precisa de confirmação por e-mail com os termos |
| **Corporativa** | tarifa e alçada do convênio | validar autorização de quem pediu |
| **OTA/agência** | comissionada, dados chegam por integração | dado incompleto é comum; conferir no balcão |
| **Seguradora (carro reserva)** | grupo e prazo definidos pela apólice | quem paga é a seguradora; extensão exige autorização dela |

**Reserva garantida x não garantida:** garantida tem cartão/pagamento vinculado, congela tarifa e sustenta política de no-show. Não garantida é intenção — não deveria consumir disponibilidade com o mesmo peso. Tratar as duas igual é o que faz a disponibilidade mentir.

**Reserva mensal / longo prazo** não é reserva diária longa: exige análise de crédito, contrato específico, franquia de km mensal, plano de manutenção e, muitas vezes, veículo dedicado.

## No-show e cancelamento

Política, não lei — mas com moldura de direito do consumidor (arrependimento em compra a distância dentro do prazo legal, informação prévia clara). O desenho mais comum:

| Situação | Prática dominante |
|---|---|
| Cancelamento com antecedência (48h+) | sem custo |
| Cancelamento em cima da hora | taxa ou retenção de parte do pré-pagamento |
| No-show em reserva não garantida | libera o carro após tolerância (1–2h), sem cobrança |
| No-show em reserva garantida/pré-paga | retém 1 diária ou o valor pré-pago, conforme o anunciado |
| Atraso na retirada | tolerância declarada; depois, a reserva cai |

A regra tem que estar visível **antes** do pagamento. Cobrança de no-show sem comunicação prévia clara é o caso clássico de estorno determinado por órgão de defesa do consumidor.

Impacto operacional: sem política de no-show, a filial segura carro parado no pico. Com política agressiva demais, o cliente vai ao concorrente. O meio-termo comum: tolerância generosa + liberação automática do carro para venda.

## Abertura do contrato

Sequência de balcão que sustenta tudo depois:

1. **Identificação** — documento com foto conferido presencialmente, CNH válida na data, titular presente.
2. **Condutores adicionais** — cadastrados e validados antes de sair.
3. **Crédito e bloqueio** — cliente sem restrição interna; caso contrário, alçada.
4. **Escolha da placa** dentro do grupo vendido, conforme disponibilidade.
5. **Oferta de proteções e adicionais**, com valores e franquias comunicados.
6. **Caução** — pré-autorização ou depósito.
7. **Vistoria de retirada** — com fotos, hodômetro, nível de combustível, avarias preexistentes marcadas, itens de série conferidos.
8. **Assinatura** do contrato e do laudo de vistoria (digital serve, desde que com prova de aceite).
9. **Entrega das chaves** — o contrato passa a valer daqui; a diária começa a contar deste momento.

Cada passo pulado vira prejuízo específico: sem (1) e (2), fraude e proteção descaracterizada; sem (6), avaria sem garantia; sem (7), nenhuma cobrança de avaria se sustenta.

## Caução

**Caução é garantia, não receita.** Formas usuais:

| Forma | Como funciona | Observação |
|---|---|---|
| Pré-autorização em cartão de crédito | bloqueia limite sem faturar | forma dominante; o prazo de bloqueio varia por emissor/adquirente — confirme no contrato da adquirente |
| Depósito em dinheiro/PIX | entra no caixa e precisa voltar | gera obrigação de devolução e conciliação; evite se puder |
| Isenção por perfil | cliente fidelizado, corporativo, seguradora | risco assumido conscientemente |

Valor: costuma variar por grupo de veículo e por perfil de risco, e cobre franquia de proteção, combustível, diárias excedentes e avarias pequenas.

Regras que evitam problema:

- Comunicar **antes** da reserva que haverá bloqueio e de quanto — a surpresa no balcão derruba a locação.
- **Liberar no fechamento**, imediatamente após apurada a conta. Caução retida além do necessário é a principal reclamação de locadora.
- Se for consumir a caução, **discriminar o que foi consumido**, com documento (vistoria, cupom de combustível, orçamento de avaria).
- Depósito em dinheiro que fica "esquecido" vira passivo e apontamento contábil.

## Proteções e franquia

Vocabulário importa: o que a locadora normalmente vende ao cliente é **proteção contratual** (limitação da responsabilidade do locatário), que só é "seguro" se houver apólice de seguradora por trás. Vender como seguro sem apólice é problema regulatório — confirme com jurídico e com a corretora antes de nomear o produto.

Produtos típicos:

- **Proteção ao veículo (colisão/roubo/furto)** — limita a responsabilidade do cliente a uma participação (franquia).
- **Proteção a terceiros (danos materiais e corporais)** — limite por evento.
- **Proteção a vidros, faróis, retrovisores e pneus** — itens de alta frequência, fora da cobertura principal.
- **Acidentes pessoais a passageiros**.
- **Isenção de participação** (produto premium que zera ou reduz a franquia).

Regras que a operação exige:

- **A franquia é comunicada em valor**, por escrito, antes da assinatura. "Você tem cobertura" sem número é a origem da maior parte dos conflitos de sinistro.
- **Condutor não cadastrado, direção sob efeito de álcool, uso fora da via adequada e transporte remunerado** costumam estar entre as exclusões — e é a locadora quem sofre quando a exclusão se aplica.
- A proteção **não** cobre multa de trânsito, combustível, limpeza nem itens perdidos.
- Recusa de proteção deve ser registrada com aceite explícito, porque muda completamente a exposição do cliente.

## Quilometragem

- **Km livre** — padrão no varejo de curta duração. Simplifica a venda e embute o custo médio na tarifa.
- **Km controlado** — franquia diária ou mensal (por exemplo, um teto por diária, ou uma franquia mensal em contrato de longo prazo) com valor por km excedente. Padrão em longo prazo e em grupos de custo alto.
- **Cobrança do excedente** exige hodômetro nas duas vistorias e valor por km comunicado no contrato.

Impacto: km livre com tarifa baixa em rota de longa distância corrói a margem via depreciação e manutenção. Por isso muita locadora limita km livre por grupo, por região ou por duração.

## Diária, tolerância e hora excedente

- **A diária é um ciclo de 24h contado da retirada.** Retirou às 14h de segunda, a segunda diária começa às 14h de terça. Contar por data de calendário é erro clássico e sempre aparece em contestação.
- **Tolerância** de 30 a 60 minutos na devolução é prática comum, comunicada no contrato.
- **Hora excedente** cobrada após a tolerância, com **teto** — o mais comum é o teto de uma diária: passou de N horas, cobra-se diária cheia. Sem teto, o cálculo produz valores absurdos e indefensáveis.
- **Devolução muito além do previsto sem contato** deixa de ser atraso e vira ocorrência (contato, notificação, e em caso extremo comunicação às autoridades). Ter um limiar declarado evita que o carro fique "atrasado" por semanas no sistema.

Exemplo de cálculo que serve como critério de aceite:

```
Retirada 10/03 09:00 · devolução prevista 12/03 09:00 · devolução real 12/03 11:30
Tolerância 30 min · hora excedente = 1/3 da diária · teto = 1 diária
→ 2 diárias + 2 horas excedentes (11:30 − 09:30 = 2h)
```

## Durante o contrato

- **Extensão** — o cliente pede mais dias. Precisa de: disponibilidade do veículo no novo período (pode estar reservado por outro), reavaliação de tarifa (a faixa de duração pode mudar), reforço da caução e novo limite de crédito. Extensão aceita sem checar disponibilidade é o gerador número um de falta de carro na filial.
- **Troca de veículo** — por defeito, sinistro, recall ou pedido do cliente. Regra dura: **vistoria de devolução do veículo antigo e vistoria de retirada do novo**, no mesmo ato. Sem isso, a avaria de um carro migra para a conta do outro. Hodômetro e combustível de ambos precisam ser fechados.
- **Substituição/carro reserva** — quando a locadora causa a troca (defeito), o custo é dela e a tarifa não muda; quando é sinistro com culpa do cliente, entram franquia e diárias de indisponibilidade.
- **Assistência 24h** — pane, pneu, bateria, guincho. O que é responsabilidade da locadora (defeito mecânico) e o que é do cliente (falta de combustível, chave trancada, pneu por mau uso) precisa estar explicitado no contrato, senão cada acionamento vira negociação.
- **Alteração contratual** (condutor adicional, adicionais, forma de pagamento) — cada alteração é um aditivo com data, autor e aceite. Alterar contrato sem versionar impede reconstruir o que valia no dia do sinistro.

## Devolução

**Devolução na filial de origem** é o caso simples. Os outros:

- **Devolução antecipada** — o cliente traz antes. A tarifa quase sempre **é recalculada pela faixa efetivamente utilizada** (7 dias contratados devolvidos em 4 podem sair da tarifa semanal e voltar para a diária, ficando mais caros por dia). Isso precisa estar claro na venda, ou o cliente entende que "vai receber de volta" e recebe uma cobrança. Algumas locadoras cobram taxa de devolução antecipada; outras absorvem em nome da experiência.
- **Devolução em outra filial (one-way)** — só entre filiais habilitadas, com taxa de retorno que cubra o deslocamento e o desequilíbrio de frota. A taxa varia com a distância e com a direção (rota de retorno "cheia" pode ser gratuita, e às vezes até incentivada).
- **Devolução fora do horário (after-hours)** — chave em cofre. Risco relevante: o veículo fica sem vistoria até a abertura. A prática é a responsabilidade do cliente terminar na entrega da chave, com o registro do horário, e a vistoria ser feita na abertura com o que houver de evidência (câmera, foto do cliente).

A **vistoria de devolução** é o momento da verdade: hodômetro, combustível, avarias novas comparadas ponto a ponto com a vistoria de retirada, limpeza, itens e acessórios, documentos do veículo. Sem o par de vistorias comparável, nenhuma cobrança de avaria se sustenta.

Cobranças que nascem aqui: quilometragem excedente, combustível + taxa de serviço, diárias/horas excedentes, **limpeza** (só quando foge do uso normal — areia, pelo de animal, odor, mancha; sujeira comum é custo da operação), avarias e itens faltantes.

## Fechamento financeiro

O contrato só morre aqui. A conta final soma:

```
diárias (tarifa aplicada × período efetivo)
+ horas excedentes
+ proteções contratadas
+ acessórios e condutor adicional
+ taxas (aeroporto, one-way, after-hours, entrega/coleta)
+ combustível + taxa de serviço
+ quilometragem excedente
+ limpeza especial
+ avarias apuradas
+ multas de trânsito já conhecidas
− pré-pagamentos e créditos
= saldo a cobrar (ou a devolver)
→ consome ou libera a caução
```

Regras: nada de liberar caução antes de fechar a conta; nada de cobrar item sem documento de suporte; discriminar cada linha na fatura, porque conta agregada é conta contestada.

## Pós-contrato

Existe porque nem toda obrigação vence no balcão:

- **Multa de trânsito** chega dias ou semanas depois. Fluxo: recebimento da notificação → identificação do contrato pela data/hora da infração → **indicação do condutor no prazo legal** (CTB; confirme a redação e o prazo vigentes) → notificação ao cliente → cobrança (taxa administrativa de indicação é praticada e precisa constar no contrato). Perder o prazo de indicação transforma a multa e a pontuação em problema da locadora.
- **Avaria em análise** — orçamento, aprovação, cobrança ou isenção. Precisa de prazo máximo declarado: avaria em análise por tempo indefinido é caução retida e cliente irritado.
- **Sinistro** aberto durante o contrato segue seu próprio processo (ver `frota-oficina-sinistros.md`).
- **Cobrança residual e inadimplência** — régua de cobrança, negativação, protesto (ver `financeiro-e-tributario.md`).
- **Objetos esquecidos** — política de guarda e prazo. Pequeno em valor, grande em reclamação.
