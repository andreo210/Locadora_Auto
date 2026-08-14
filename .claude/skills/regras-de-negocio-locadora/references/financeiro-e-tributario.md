# Financeiro e tributário

Locação é um negócio de capital intensivo com receita pulverizada: muita transação pequena de um lado, poucos desembolsos enormes do outro. O descasamento entre os dois é o que quebra locadora — não a falta de cliente.

**Aviso permanente:** tudo neste arquivo sobre tributo é o **desenho** da regra, para orientar modelagem e conversa. Alíquota, prazo, base e enquadramento mudam por município, estado, regime e ano. **Confirme com o contador da empresa e com a legislação vigente antes de implementar qualquer cálculo fiscal.**

## Índice

- [Contas a receber](#contas-a-receber)
- [Meios de pagamento](#meios-de-pagamento)
- [Pré-autorização e caução](#pré-autorização-e-caução)
- [Estorno e chargeback](#estorno-e-chargeback)
- [Faturamento e nota fiscal](#faturamento-e-nota-fiscal)
- [Inadimplência e cobrança](#inadimplência-e-cobrança)
- [Renegociação](#renegociação)
- [Contas a pagar](#contas-a-pagar)
- [Fluxo de caixa](#fluxo-de-caixa)
- [Centros de custo e rateio](#centros-de-custo-e-rateio)
- [DRE da locadora](#dre-da-locadora)
- [Tributos sobre a receita](#tributos-sobre-a-receita)
- [Regimes de tributação](#regimes-de-tributação)
- [Reforma tributária](#reforma-tributária)
- [Controles que a auditoria cobra](#controles-que-a-auditoria-cobra)

---

## Contas a receber

Origens de recebível numa locadora, cada uma com prazo e risco diferentes:

| Origem | Momento | Risco |
|---|---|---|
| Pré-pagamento de reserva | antes da retirada | baixo; gera obrigação de execução |
| Locação no ato (varejo) | na retirada ou no fechamento | baixo, com cartão |
| Fatura corporativa | mensal, com prazo | médio; depende de análise de crédito |
| Seguradora (carro reserva) | mensal, contra contrato | médio; glosa é comum |
| Agência/OTA | repasse líquido, com prazo | médio; conciliação trabalhosa |
| Cobrança pós-contrato (multa, avaria) | dias/semanas depois | alto; cliente já foi embora |
| Venda de seminovo | na transferência | baixo |

**A cobrança pós-contrato é a de pior recuperação** — o cliente já se desligou emocionalmente e frequentemente contesta. É exatamente por isso que a caução existe e que documentação de vistoria importa.

## Meios de pagamento

| Meio | Vantagem | Custo/risco |
|---|---|---|
| **Cartão de crédito** | permite pré-autorização (caução), identifica o cliente, garante recebimento | MDR (taxa da adquirente), prazo de repasse, chargeback |
| **Cartão de débito** | custo menor | **não serve como caução** — débito bloqueia dinheiro real, não limite |
| **PIX** | liquidação imediata, custo baixo | irreversível; devolução é operação manual; não garante caução |
| **Boleto** | usual em corporativo | prazo, inadimplência, custo por título |
| **Dinheiro** | imediato | risco de caixa, exige controle rígido, exposto a fraude interna |
| **Faturamento** | fideliza corporativo | crédito, prazo, risco concentrado |

Locação sem cartão de crédito é decisão de risco consciente, não uma facilidade neutra: some a garantia e, com ela, a capacidade de cobrar avaria, combustível e multa depois.

## Pré-autorização e caução

Mecanismo mal compreendido e origem de muita reclamação:

- **Pré-autorização não é cobrança.** Ela reserva limite no cartão; o dinheiro não sai. Se nada for consumido, o bloqueio cai — o prazo de queda varia por emissor e adquirente, e é isso que o cliente sente como "ainda não voltou".
- **Captura parcial** (cobrar menos do que foi pré-autorizado) e **cancelamento da pré-autorização** dependem do que a adquirente suporta. Isso muda o desenho do sistema: confirme o contrato da adquirente antes de prometer comportamento.
- **Comunicar antes** o valor a ser bloqueado. Cliente que descobre no balcão que precisa de limite disponível cancela a locação.
- **Liberar imediatamente após o fechamento.** Reter caução "por precaução" é a reclamação número um do setor e não tem defesa.
- Caução em **dinheiro** entra no caixa e vira obrigação de devolução: precisa aparecer no passivo, conciliar e ter prazo. Prefira evitar.

Contabilmente, **caução não é receita** enquanto não for consumida. Reconhecer como receita infla faturamento, distorce imposto e cria passivo oculto.

## Estorno e chargeback

- **Estorno** — a própria locadora devolve (cobrança indevida, cancelamento, devolução de caução). Precisa de motivo, autor e alçada, porque estorno é o caminho clássico de fraude interna.
- **Chargeback** — o cliente contesta com o emissor e o valor é retirado da locadora. Defesa depende de **documentação**: contrato assinado, vistoria com fotos datadas, comprovante de entrega, comunicação prévia dos valores. Quem não guarda evidência perde quase todo chargeback.
- Regra prática: cada linha cobrada precisa ter um documento de suporte recuperável em minutos. É isso que transforma contestação em vitória.

## Faturamento e nota fiscal

Momentos possíveis de faturar, e a escolha muda tudo:

- **No fechamento do contrato** (varejo) — padrão.
- **Mensalmente, por competência** (longo prazo, corporativo) — exige apuração de diárias, km e adicionais do período.
- **Por ciclo do cliente** (corporativo com data de corte).

O documento fiscal correto para locação de bem móvel **não é óbvio** e depende do enquadramento adotado pela empresa e do entendimento do município — locação pura de bem móvel tem tratamento diferente de prestação de serviço. Serviços acessórios (locação com motorista, gestão de frota, taxa de serviço, lavagem) frequentemente têm enquadramento próprio. **Isso se decide com o contador, não no código.** O que o sistema precisa suportar é a separação das receitas por natureza, porque é ela que determina o documento e o tributo.

Estrutura mínima que evita retrabalho: separar, na conta do contrato, **receita de locação**, **receita de proteção**, **receita de acessórios**, **taxas**, **reembolsos** (combustível, multa) e **indenizações** (avaria). Cada bloco pode ter tratamento fiscal diferente, e agrupar tudo em "valor total" torna impossível corrigir depois.

## Inadimplência e cobrança

Régua típica, com o custo subindo a cada degrau:

1. **D+1 a D+7** — lembrete automático (e-mail, SMS, WhatsApp). Barato e resolve o esquecimento, que é a maior fatia.
2. **D+8 a D+30** — contato ativo, oferta de parcelamento, bloqueio de novas locações.
3. **D+30 a D+60** — cobrança formal, negativação em bureau (respeitando a notificação prévia exigida).
4. **D+60+** — protesto, cobrança terceirizada, ação judicial conforme o valor.
5. **Baixa por perda** — depois de esgotado o esforço, com critério e aprovação.

Regras de negócio associadas:

- **Bloqueio automático do cliente inadimplente** para novas locações, inclusive com reserva paga. Liberar exige alçada e registro.
- **Encargos** (multa, juros, correção) conforme o contrato e os limites legais aplicáveis ao tipo de relação — confirme com o jurídico.
- **Custo de cobrar** precisa ser comparado ao valor: acionar cobrança terceirizada por valor pequeno consome mais do que recupera. Ter um piso de acionamento é gestão, não desleixo.
- **Provisão para perda** sobre a carteira vencida, senão o resultado do mês fica otimista.

## Renegociação

Sempre melhor que a perda total do crédito, desde que estruturada:

- Registrar o acordo com valor, entrada, parcelas, encargos e data.
- **Quebra de acordo restabelece o débito original**, com o pago abatido — isso precisa estar escrito.
- Desconto para quitação à vista tem alçada, e a alçada precisa ser respeitada por sistema, não por confiança.
- Renegociação não desbloqueia o cliente automaticamente: desbloqueio é decisão comercial separada.

## Contas a pagar

Onde o dinheiro sai, em ordem aproximada de peso:

- **Parcela de financiamento / aquisição de frota** — o maior desembolso, e o mais rígido.
- **Depreciação** — não sai do caixa, mas é o maior custo do resultado. Ignorá-la é como locadora quebra "lucrando".
- **Manutenção e peças**.
- **Seguro / prêmio de apólice** ou aporte no fundo de autosseguro.
- **IPVA e licenciamento** — sazonal e pesado, concentrado no início do ano. Provisionar mensalmente evita o aperto de janeiro.
- **Folha e encargos**.
- **Aluguel de pátio, aeroporto, concession fee** (percentual sobre receita em aeroporto é significativo).
- **Comissões de agência/OTA**.
- **Marketing e tecnologia**.
- **Tributos**.

## Fluxo de caixa

O ponto crítico da locadora: **paga-se o carro à vista (ou em parcelas grandes) e recebe-se em diárias pequenas.** O ciclo de recuperação do investimento é longo, e a venda do seminovo é parte essencial do caixa — não é receita "extra", é o fechamento do ciclo do ativo.

Consequências práticas para o negócio:

- Crescer frota consome caixa **antes** de gerar receita. Crescimento acelerado sem capital é o erro clássico.
- **Sazonalidade** (alta temporada, feriados, eventos) exige frota extra que fica ociosa depois — daí o uso de frota temporária e de contratos de recompra.
- **IPVA concentrado** e renovação de apólice pedem provisão.
- A **venda de seminovos** precisa de ritmo constante; parar de vender por dois meses aperta o caixa mesmo com a operação indo bem.

## Centros de custo e rateio

Estrutura mínima que permite decidir alguma coisa:

- Por **filial** — a unidade que ganha ou perde dinheiro.
- Por **grupo de veículo** — qual categoria realmente dá margem.
- Por **veículo** (placa) — receita e custo acumulados, base para desmobilizar.
- Por **canal** — balcão, digital, OTA, corporativo, seguradora.
- Por **cliente corporativo** — para renegociação de contrato.

Custos indiretos (sede, TI, marketing) precisam de critério de rateio declarado — número de veículos, receita ou diárias. Qualquer critério serve desde que seja estável; trocar critério a cada mês impede comparação e todo mundo desconfia do número.

## DRE da locadora

Formato que os gestores do setor efetivamente leem:

```
  Receita de locação (diárias)
+ Receita de proteções
+ Receita de acessórios e taxas
= RECEITA BRUTA DE LOCAÇÃO
− Deduções (tributos sobre receita, cancelamentos, descontos)
= RECEITA LÍQUIDA

− Custos diretos de frota
    depreciação
    manutenção e pneus
    seguro / sinistros retidos
    IPVA, licenciamento e documentação
    preparação, limpeza e transferências
= MARGEM DE CONTRIBUIÇÃO DA FROTA

− Despesas operacionais (filial, pátio, folha, aeroporto)
− Despesas comerciais (comissões, marketing)
− Despesas administrativas
= EBITDA

− Depreciação (se tratada abaixo do EBITDA) e amortização
− Resultado financeiro (juros do financiamento de frota)
+/− Resultado na venda de seminovos
= RESULTADO ANTES DOS IMPOSTOS
− IRPJ / CSLL
= RESULTADO LÍQUIDO
```

Duas observações que valem a discussão inteira:

1. **Onde entra a depreciação** muda o EBITDA radicalmente. Locadora costuma apresentar EBITDA antes da depreciação de frota, o que faz o número parecer excelente — e por isso o setor também olha o resultado depois dela.
2. **O resultado da venda de seminovos** não é acessório: em muitos exercícios é ele que define se o ano fechou no azul. Tratá-lo como "receita não operacional" esconde a natureza do negócio.

## Tributos sobre a receita

Desenho geral no Brasil, com as ressalvas do topo deste arquivo:

- **ISS** — imposto municipal sobre serviços. Historicamente, **locação pura de bem móvel não é prestação de serviço** para fins de ISS (entendimento consolidado do STF na Súmula Vinculante 31). Na prática, isso significa que a receita de locação pura tende a ficar fora do ISS, mas **serviços acessórios** (com motorista, gestão de frota, taxas de serviço) podem ser tributados. Municípios divergem e autuam; a posição da empresa se define com o jurídico tributário.
- **ICMS** — estadual, sobre circulação de mercadoria. Locação não transfere propriedade, então em regra não incide sobre a diária. Aparece na **compra** e na **venda** de veículos (com regras e eventuais benefícios específicos para locadora, condicionados a tempo de permanência do bem no ativo).
- **PIS e COFINS** — federais, sobre a receita. Alíquota e forma (cumulativo x não cumulativo) dependem do regime.
- **IRPJ e CSLL** — sobre o lucro, com base e alíquota conforme o regime.
- **Contribuição previdenciária** sobre a folha.
- **Tributos do veículo** — IPVA, licenciamento e taxas, por unidade e por exercício.

O que o **sistema** precisa fazer, independentemente da definição fiscal: separar as receitas por natureza, registrar o município de prestação (filial), controlar retenções em faturamento corporativo e guardar a base de cálculo de cada linha. Sem isso, qualquer decisão tributária futura vira reprocessamento manual.

## Regimes de tributação

| Regime | Quando costuma fazer sentido | Cuidado |
|---|---|---|
| **Simples Nacional** | faturamento pequeno, estrutura enxuta | há limite de receita; locação de bens móveis pode ter restrições e anexos específicos — verifique o enquadramento com o contador, não presuma |
| **Lucro Presumido** | margem real acima do percentual presumido, receita média | PIS/COFINS cumulativos: não há crédito sobre as compras, o que pesa em negócio com muito insumo |
| **Lucro Real** | margem apertada, prejuízo fiscal a compensar, muitos créditos | PIS/COFINS não cumulativos permitem créditos; obrigações acessórias bem mais pesadas |

A escolha de regime é **a decisão financeira anual mais relevante** de uma locadora de médio porte, e depende de simulação com números reais. Não recomende regime sem essa simulação; recomende fazer a simulação.

## Reforma tributária

O Brasil está em transição para o modelo de IVA dual (**CBS** federal e **IBS** estadual/municipal), substituindo PIS/COFINS, ICMS e ISS, com implantação escalonada ao longo dos próximos anos e regras de transição.

Para uma locadora, os pontos que precisam entrar na conversa desde já:

- A separação de receitas por natureza e a identificação de local de prestação/uso **ganham importância**, não perdem.
- Regime **não cumulativo amplo** tende a mudar o cálculo de créditos sobre aquisição de frota, manutenção e insumos — o que pode alterar significativamente a carga efetiva.
- Contratos longos (locação mensal, terceirização de frota) que atravessam a transição precisam de **cláusula de reequilíbrio tributário**.
- O sistema precisa comportar **dois modelos convivendo** durante a transição.

**Prazos, alíquotas e detalhes de transição mudam com frequência — cheque a legislação vigente e o contador antes de qualquer implementação.** O papel desta skill é garantir que o assunto entre na pauta cedo, não fornecer a alíquota.

## Controles que a auditoria cobra

Se estes não existirem, o parecer vem com ressalva:

- **Conciliação diária de caixa e de adquirente** — venda registrada x repasse recebido.
- **Nenhum desconto, isenção ou cortesia sem motivo, autor e alçada.**
- **Estorno com aprovação separada de quem cobrou.**
- **Caução conciliada**: tudo que foi bloqueado ou depositado tem destino documentado.
- **Contrato encerrado sem fechamento financeiro** é exceção monitorada, não rotina.
- **Sequência e integridade de documentos fiscais.**
- **Inventário físico de frota** periódico, batendo com o sistema — carro que "existe" só no sistema é achado grave.
- **Trilha de auditoria** de quem alterou tarifa, contrato, vistoria e status de veículo.
