# Cadastro e tarifas

Base de tudo. Cadastro errado não aparece no dia do cadastro — aparece no sinistro, na cobrança contestada e na multa que não pôde ser indicada.

## Índice

- [Cliente PF](#cliente-pf)
- [Cliente PJ e contrato corporativo](#cliente-pj-e-contrato-corporativo)
- [Condutor adicional](#condutor-adicional)
- [CNH e documentação](#cnh-e-documentação)
- [Análise de crédito e risco](#análise-de-crédito-e-risco)
- [Veículo](#veículo)
- [Categoria, modelo e grupo tarifário](#categoria-modelo-e-grupo-tarifário)
- [Filial e agência](#filial-e-agência)
- [Parceiros, convênios e canais](#parceiros-convênios-e-canais)
- [Tarifário](#tarifário)
- [Combustível](#combustível)
- [Acessórios e adicionais](#acessórios-e-adicionais)

---

## Cliente PF

**Mínimo indispensável:** nome completo, CPF, data de nascimento, endereço completo, telefone celular, e-mail, CNH (número, categoria, validade, primeira habilitação, órgão emissor).

Endereço e telefone não são burocracia: são o que permite localizar o cliente e o veículo quando o contrato vira ocorrência. Locadora que aceita endereço incompleto descobre isso em apropriação indébita.

**Regras que quase toda locadora aplica** (política, não lei):

| Regra | Corte mais comum | Variações | Por quê |
|---|---|---|---|
| Idade mínima do condutor | 21 anos | 18 em algumas frotas econômicas; 25 em grupos premium, SUV grande e utilitário | sinistralidade cai fortemente com idade e experiência |
| Tempo mínimo de habilitação | 2 anos | 1 ano em grupo econômico | condutor recém-habilitado tem frequência de sinistro muito maior |
| CNH definitiva | exigida | permissão (PPD) raramente aceita | permissão indica condutor em período probatório |
| Taxa de condutor jovem | 18–24 anos | valor por diária | precifica o risco em vez de recusar a venda |

**Duplicidade de cadastro** é o defeito silencioso mais caro: o mesmo CPF entra três vezes e o histórico de inadimplência, de avaria e de bloqueio se perde. A chave natural é o CPF; nome não serve. Cadastro duplicado detectado deve ser mesclado, com trilha de qual registro absorveu qual.

**Status de cliente** que a operação realmente usa: habilitado, bloqueado por crédito (inadimplente), bloqueado por comportamento (avaria recorrente, contrato encerrado com irregularidade, fraude confirmada), inativo por tempo. Bloqueio precisa de motivo, autor e data — bloqueio anônimo ninguém tem coragem de remover, e o cliente fica preso para sempre.

## Cliente PJ e contrato corporativo

PJ não é PF com CNPJ. O que muda:

- **Quem contrata não dirige.** A empresa é a contratante; os condutores são funcionários autorizados. Cada condutor precisa estar vinculado ao contrato corporativo, com CNH validada individualmente.
- **Faturamento em vez de pagamento no ato.** Contrato corporativo costuma ter faturamento mensal, prazo de pagamento e limite de crédito. Isso muda o fluxo de caixa e exige análise de crédito de verdade.
- **Centro de custo.** A empresa quer a despesa rateada por área, projeto ou funcionário — informação capturada na abertura do contrato, não depois.
- **Tarifa negociada** por contrato, com vigência. Precisa vencer a tarifa de balcão automaticamente, senão o balcão erra o preço.
- **Alçada de autorização:** quem, na empresa cliente, pode autorizar retirada, upgrade, extensão e adicionais. Sem isso a locadora presta serviço que o financeiro do cliente depois recusa pagar.
- **Documentação:** CNPJ, contrato social ou estatuto, procuração de quem assina, comprovante de endereço, dados bancários.

**Locação de longo prazo / terceirização de frota** é outro negócio dentro do mesmo negócio: contrato de 12 a 48 meses, mensalidade fixa que embute depreciação, manutenção, seguro e documentação, franquia mensal de quilometragem e cobrança do excedente, veículo dedicado ao cliente e substituição em caso de manutenção. A conta é de TCO, não de diária — quem precifica longo prazo com tabela de diária destrói a margem.

## Condutor adicional

Ponto de vazamento clássico. O contrato é do titular, mas o carro roda com outra pessoa.

- Cada condutor adicional passa pela **mesma validação** do titular: CNH válida, idade mínima, tempo de habilitação, checagem de restrição.
- É **cobrado** na maioria das locadoras (taxa por diária ou taxa fixa por contrato) — não pela receita, mas porque cada condutor a mais aumenta a exposição.
- Precisa ser incluído **antes** de dirigir, com registro de data/hora. Inclusão retroativa depois do sinistro é fraude comum.
- Condutor não declarado dirigindo costuma **descaracterizar a proteção contratual** — a seguradora nega, e a locadora fica com o prejuízo integral. Essa é a razão de a regra ser dura.
- Cônjuge incluído gratuitamente é cortesia praticada por parte do mercado; é política, não padrão.

## CNH e documentação

**Validade da CNH** (Brasil, Lei 14.071/2020 — confirme a redação vigente): 10 anos para condutores até 50 anos, 5 anos entre 50 e 70, 3 anos acima de 70. O que interessa operacionalmente é a **validade na data da retirada**, não na data do cadastro nem na da reserva: reserva feita com CNH válida pode chegar ao balcão com CNH vencida.

Checagens que valem a pena no ato:

1. CNH dentro da validade **no dia da retirada**.
2. Categoria compatível com o veículo (`B` para passeio; `D`/`E` para veículos maiores).
3. CNH definitiva, não permissão.
4. Documento com foto conferido presencialmente contra o portador — o passo que mais evita fraude e o mais pulado quando a fila cresce.
5. Estrangeiro: CNH do país de origem dentro da validade, com tradução/PID conforme o caso, e passaporte. Regra sensível a acordo internacional — confirme antes de codificar.

**Retenção**: cópia ou foto de CNH é dado pessoal, e a foto do documento carrega dado biométrico. Guarde o que a operação precisa, pelo prazo definido, com base legal declarada (ver `indicadores-atendimento-compliance.md`).

## Análise de crédito e risco

Nem toda locadora faz, e a que não faz paga em perda. Camadas usuais, da mais barata para a mais cara:

1. **Lista interna** — cliente bloqueado, inadimplente, com histórico de avaria não paga ou de contrato encerrado com irregularidade. Custa zero e resolve boa parte.
2. **Consulta a bureau de crédito** — restrição, score. Custa por consulta; comum em contrato de longo prazo e em locação sem cartão de crédito.
3. **Antifraude documental** — validação de documento, prova de vida, cruzamento de dados. Comum em canal digital e em aeroporto.
4. **Cartão de crédito com limite disponível** — a garantia mais usada no varejo, porque resolve caução e identidade ao mesmo tempo.

Locação sem cartão de crédito (débito, PIX, dinheiro) é decisão de risco: amplia o mercado e aumenta muito a exposição, porque some a garantia. Quem aceita costuma compensar com caução em dinheiro, análise de crédito, limite de grupo de veículo e restrição de quilometragem.

## Veículo

**Identificação:** placa, chassi (VIN), Renavam, marca, modelo, versão, ano de fabricação e modelo, cor, combustível, câmbio, número de portas, capacidade de passageiros e de bagagem.

**Operacional:** filial de origem, filial atual, status, hodômetro atual, nível de combustível, data de entrada na frota, data prevista de desmobilização.

**Documental e fiscal:** proprietário (locadora, banco/arrendadora, fundo), gravame, licenciamento e IPVA por exercício, seguro, valor de aquisição, valor contábil, valor de mercado.

A leitura do **hodômetro** entra em toda vistoria e em toda ordem de serviço. É o dado que dispara manutenção preventiva, calcula custo por quilômetro e comprova quilometragem excedente. Hodômetro que anda para trás é erro de digitação ou fraude — vale bloquear.

## Categoria, modelo e grupo tarifário

Três coisas diferentes que a maioria dos sistemas mistura:

- **Modelo** — o carro específico (Onix 1.0 Manual).
- **Categoria/grupo** — o conjunto intercambiável que o cliente compra ("Econômico", "Intermediário Automático", "SUV"). É o que a reserva vende. Padrão internacional: código ACRISS/SIPP de 4 letras (tamanho, carroceria, câmbio/tração, combustível/ar).
- **Grupo tarifário** — o agrupamento usado para precificar, que pode não coincidir com a categoria (mesmo grupo, preço diferente por filial, período ou canal).

Regra que sustenta a operação: **o cliente compra o grupo, a locadora escolhe a placa**. Isso é o que permite operar com ocupação alta. Prometer modelo específico ("ou similar" some do anúncio) transforma cada indisponibilidade em reclamação legítima.

Hierarquia de **upgrade** precisa estar declarada: qual grupo pode substituir qual, sem custo para o cliente. Sem a hierarquia, o balcão improvisa e entrega SUV no preço de econômico.

## Filial e agência

- **Filial** — unidade com pátio, frota própria, CNPJ ou inscrição própria, horário de funcionamento, equipe.
- **Agência/ponto de atendimento** — atende e entrega, mas pode não ter pátio próprio (balcão de aeroporto, hotel, concessionária parceira).

O que precisa estar cadastrado: horário por dia da semana e feriado (retirada fora do horário é taxa), taxa de aeroporto ou concession fee, se aceita devolução one-way e de quais filiais, raio de entrega/coleta e o custo disso.

**Horário da filial é regra de negócio, não decoração:** define se uma reserva pode ser aceita, quando a diária começa, e se cabe cobrar taxa de atendimento fora do expediente.

## Parceiros, convênios e canais

Cada canal de venda tem regra e custo diferentes, e a margem só aparece quando isso está separado:

| Canal | Como remunera | Cuidado |
|---|---|---|
| Balcão próprio | sem comissão | maior margem, menor volume |
| Site/app próprio | custo de mídia | melhor canal de margem escalável |
| Agência de viagem / OTA | comissão (% da locação) | comissão sobre o quê? locação apenas ou também acessórios |
| Broker / consolidador | tarifa net negociada | a locadora não controla o preço final |
| Convênio corporativo | tarifa negociada, sem comissão | exige controle de vigência |
| Convênio de benefício (clube, cartão, associação) | desconto por elegibilidade | validar elegibilidade na retirada, não só na reserva |
| Seguradora (carro reserva) | contrato de reposição, faturado à seguradora | prazo, grupo e franquia definidos por apólice, não pelo cliente |
| Concessionária (cortesia de revisão) | faturado à concessionária | quem paga não é quem dirige |

Quando quem paga não é quem dirige, **todo o desenho de cobrança muda**: caução, franquia, combustível e avaria precisam de destinatário explícito.

## Tarifário

O erro estrutural mais comum é guardar preço no contrato sem tabela versionada por trás. Sem versão, mudar preço reescreve o passado e o fechamento de mês não bate.

Dimensões que uma tabela de tarifas real precisa ter:

- **Grupo tarifário** e **filial** (ou região).
- **Vigência** (início e fim) — nunca sobrescrever, sempre criar nova vigência.
- **Canal** (balcão, site, OTA, corporativo, convênio).
- **Faixa de duração**: diária avulsa, semanal, quinzenal, mensal. O preço por dia cai conforme a duração, e a regra de "qual faixa aplicar" precisa ser explícita (fecha semana a partir de 7 diárias? arredonda para cima?).
- **Antecedência** e **sazonalidade** (alta temporada, feriado, evento).
- **Inclusões**: quilometragem livre ou franquia de km, proteção inclusa ou opcional.

Regras de precificação que valem discutir com o cliente:

- **Congelamento de tarifa na reserva.** Prática dominante: reserva garantida congela o preço; reserva não garantida não. Não congelar gera reclamação; congelar sem garantia expõe a locadora a especulação.
- **Tarifa mínima e teto de desconto por alçada** — evita que o balcão venda abaixo do custo.
- **Yield/gestão de receita**: subir preço conforme a ocupação do grupo cresce. Grandes fazem automaticamente; média faz por regra simples (acima de 85% de ocupação prevista, sobe faixa); pequena faz na mão. Sem nenhum mecanismo, a locadora vende barato justamente no dia em que faltaria carro.

## Combustível

Três políticas, e a escolha muda processo e reclamação:

| Política | Como funciona | Quem usa | Risco |
|---|---|---|---|
| **Full-to-full** | sai cheio, volta cheio; diferença cobrada com taxa de serviço | padrão do mercado | exige aferição confiável do nível |
| **Pré-pago (full-to-empty)** | cliente compra o tanque na saída, devolve como quiser | aeroporto, alta rotatividade | percepção de venda casada se mal comunicado |
| **Mesmo nível** | volta como saiu, em qualquer nível | frota pequena | aferição imprecisa gera discussão |

O **nível registrado nas duas vistorias** é o que sustenta a cobrança. Em oitavos ou em quartos, tanto faz — desde que seja a mesma escala nas duas pontas e que haja foto do painel. Cobrar combustível sem foto do painel na saída é cobrança indefensável.

A **taxa de serviço de abastecimento** é legítima (deslocamento, tempo, carro parado) e precisa estar comunicada antes, com valor. Cobrar preço de combustível muito acima do posto sem aviso prévio é o tipo de prática que vira processo e reportagem.

## Acessórios e adicionais

Receita acessória é onde a margem da locação realmente aparece — a diária paga o carro, o acessório paga a operação.

Catálogo típico: cadeirinha/bebê conforto/assento de elevação, GPS, suporte de bagagem, corrente de neve (regional), Wi-Fi, pedágio automático (tag), condutor adicional, entrega e coleta, retirada fora do horário, one-way, motorista.

Cada item precisa de: unidade de cobrança (por diária, por contrato, por evento), **teto** (cadeirinha por diária costuma ter teto para não superar o preço do item), estoque por filial, e o vínculo com o contrato para retorno/conferência na devolução.

Duas armadilhas:

- **Acessório é ativo com estoque.** Reservar cadeirinha sem controlar estoque por filial gera promessa que o balcão não cumpre — e cadeirinha é item com apelo legal e emocional forte.
- **Acessório não devolvido é cobrança**, e precisa aparecer na vistoria de devolução como conferência explícita, não como memória do atendente.

Item de segurança obrigatório (triângulo, macaco, chave de roda, estepe, extintor quando exigido) **não é acessório vendável** — é conferência de vistoria, e faltar é problema de conformidade do veículo.
