# 03 — Modelo entidade-relacionamento

Banco **PostgreSQL** acessado via Npgsql. O modelo do EF Core é a fonte de verdade: o schema é
gerado pelas migrations em `Infra/Data/Migrations/` e o arquivo `db_postgres.sql` na raiz é a
saída de `dotnet ef migrations script --idempotent`. Tabelas e colunas seguem **snake_case**.

> O `db.sql` na raiz é o schema MySQL antigo, mantido apenas como referência histórica. Ele
> **não** corresponde ao modelo atual.

---

## 1. Visão geral — todas as tabelas

```mermaid
erDiagram
    asp_net_users     ||--o| tb_cliente : "perfil cliente"
    asp_net_users     ||--o| tb_funcionario : "perfil funcionário"
    asp_net_users     ||--o{ refresh_tokens : "emite"
    asp_net_users     ||--o{ asp_net_user_roles : "tem papel"
    asp_net_roles     ||--o{ asp_net_user_roles : "atribuído a"
    asp_net_users     ||--o{ asp_net_user_claims : "claims"
    asp_net_users     ||--o{ asp_net_user_logins : "logins externos"
    asp_net_users     ||--o{ asp_net_user_tokens : "tokens"
    asp_net_roles     ||--o{ asp_net_role_claims : "claims"

    tb_cliente        ||--o| tb_endereco : "reside em"
    tb_endereco       ||--o| tb_filial : "localiza"
    tb_filial         ||--o{ tb_foto_filial : "possui"

    tb_categoria_veiculo ||--o{ tb_foto_categoria_veiculo : "possui"
    tb_categoria_veiculo ||--o{ tb_veiculo : "classifica"
    tb_filial            ||--o{ tb_veiculo : "abriga"
    tb_veiculo           ||--o{ tb_manutencao : "sofre"

    tb_cliente           ||--o{ tb_reserva : "solicita"
    tb_filial            ||--o{ tb_reserva : "atende"
    tb_categoria_veiculo ||--o{ tb_reserva : "é reservada"

    tb_cliente        ||--o{ tb_locacao : "aluga"
    tb_veiculo        ||--o{ tb_locacao : "é alugado"
    tb_funcionario    ||--o{ tb_locacao : "registra"
    tb_filial         ||--o{ tb_locacao : "retirada e devolução"

    tb_locacao        ||--o{ tb_pagamento : "recebe"
    tb_locacao        ||--o{ tb_caucao : "retém"
    tb_locacao        ||--o{ tb_multa : "gera"
    tb_locacao        ||--o{ tb_locacao_seguro : "contrata"
    tb_locacao        ||--o{ tb_locacao_adicional : "inclui"
    tb_locacao        ||--o{ tb_vistoria : "é vistoriada"
    tb_locacao        ||--o{ historico_status_locacao : "trilha de status"
    tb_locacao        ||--o| tb_fechamento_locacao : "apura a conta"
    tb_fechamento_locacao ||--o{ tb_linha_fechamento : "discrimina"

    tb_seguro         ||..o{ tb_locacao_seguro : "sem FK no banco"
    tb_adicional      ||--o{ tb_locacao_adicional : "é contratado"

    tb_vistoria       ||--o{ tb_dano : "constata"
    tb_vistoria       ||--o{ tb_foto_vistoria : "documenta"
    tb_funcionario    ||--o{ tb_vistoria : "executa"
    tb_funcionario    ||--o{ historico_status_locacao : "registra"

    tb_cliente        ||..o{ tb_cliente_historico : "auditoria temporal"
    asp_net_users     ||..o{ tb_user_historico : "auditoria temporal"
```

Relações tracejadas (`..`) não têm chave estrangeira no banco: as tabelas de histórico são
preenchidas por reflexão no `SaveChangesAsync` e `tb_locacao_seguro.id_seguro` não foi mapeado
com `HasOne<Seguro>`.

---

## 2. Identidade e pessoas

```mermaid
erDiagram
    asp_net_users {
        text id PK
        text nome_completo "varchar(255)"
        text cpf
        boolean ativo
        timestamptz data_criacao
        text user_name "varchar(256)"
        text normalized_user_name "varchar(256)"
        text email "varchar(256)"
        text normalized_email "varchar(256)"
        boolean email_confirmed
        text password_hash
        text security_stamp
        text concurrency_stamp
        text phone_number
        boolean phone_number_confirmed
        boolean two_factor_enabled
        timestamptz lockout_end
        boolean lockout_enabled
        integer access_failed_count
    }

    tb_cliente {
        integer id_cliente PK
        text numero_habilitacao
        timestamptz validade_habilitacao
        boolean ativo
        integer total_locacoes
        text status "varchar(20) — StatusCliente"
        timestamptz data_criacao
        text id_usuario_criacao
        timestamptz data_modificacao
        text id_usuario_modificacao
        text id_asp_net_users FK
    }

    tb_funcionario {
        integer id_funcionario PK
        text matricula UK "varchar(20)"
        text cargo "varchar(50)"
        boolean status "propriedade Ativo"
        text id_user FK
    }

    tb_endereco {
        integer id_endereco PK
        integer id_cliente FK "único, anulável"
        text logradouro
        text numero
        text complemento
        text bairro
        text cidade
        text estado
        text cep
    }

    refresh_tokens {
        integer id PK
        text token UK
        timestamptz expira_em
        boolean revogado
        timestamptz criado_em
        text user_id FK
    }

    tb_cliente_historico {
        integer id_historico PK
        integer id_cliente
        timestamptz data_evento
        text acao "UPDATE ou DELETE"
        text usuario_evento
        text numero_habilitacao
        timestamptz validade_habilitacao
        integer total_locacoes
        text id_usuario_modificacao
    }

    tb_user_historico {
        integer id_historico PK
        text id
        text nome_completo
        text email
        text phone_number
        timestamptz data_evento
        text acao "UPDATE ou DELETE"
        text usuario_evento
    }

    asp_net_roles {
        text id PK
        text name "varchar(256)"
        text normalized_name "varchar(256)"
        text concurrency_stamp
    }

    asp_net_user_roles {
        text user_id PK "também FK"
        text role_id PK "também FK"
    }

    asp_net_users ||--o| tb_cliente : "1:0..1"
    asp_net_users ||--o| tb_funcionario : "1:0..1"
    asp_net_users ||--o{ refresh_tokens : "emite"
    asp_net_users ||--o{ asp_net_user_roles : "tem papel"
    asp_net_roles ||--o{ asp_net_user_roles : "atribuído a"
    tb_cliente    ||--o| tb_endereco : "1:0..1"
    tb_cliente    ||..o{ tb_cliente_historico : "sem FK"
    asp_net_users ||..o{ tb_user_historico : "sem FK"
```

`asp_net_user_claims`, `asp_net_user_logins`, `asp_net_user_tokens` e `asp_net_role_claims`
são tabelas padrão do ASP.NET Core Identity, renomeadas para snake_case no `OnModelCreating`.

---

## 3. Frota, filiais e reservas

```mermaid
erDiagram
    tb_filial {
        integer id_filial PK
        text nome "varchar(100)"
        text cidade "varchar(100)"
        boolean ativo
        integer id_endereco FK
        integer tempo_preparacao_minutos "default 120"
        boolean permite_transferencia "default true"
        boolean habilitada_one_way "default true"
        numeric taxa_retorno_one_way "10,2 — default 0"
        integer tolerancia_minutos "default 30"
        numeric percentual_hora_excedente "5,4 — default 0,3333"
        numeric preco_litro_combustivel "10,2 — default 0"
        numeric taxa_servico_abastecimento "10,2 — default 0"
        numeric valor_limpeza_especial "10,2 — default 0"
    }

    tb_endereco {
        integer id_endereco PK
        integer id_cliente FK
        text logradouro
        text numero
        text complemento
        text bairro
        text cidade
        text estado
        text cep
    }

    tb_categoria_veiculo {
        integer id_categoria PK
        text nome "varchar(50)"
        numeric valor_diaria "10,2"
        integer limite_km
        numeric valor_km_excedente "10,2"
    }

    tb_veiculo {
        integer id_veiculo PK
        text placa UK "varchar(10)"
        text marca
        text modelo
        integer ano
        text chassi UK "varchar(30)"
        integer id_categoria FK
        integer km_atual
        boolean ativo
        boolean disponivel
        integer id_filial_atual FK
        integer status "StatusVeiculo"
        numeric capacidade_tanque_litros "6,2 — anulável"
        text motivo_desmobilizacao "varchar(500) — anulável"
        timestamptz data_desmobilizacao "anulável"
        integer id_funcionario_desmobilizacao FK "anulável"
    }

    tb_manutencao {
        integer id_manutencao PK
        integer tipo_manutencao "TipoManutencao"
        text descricao
        numeric custo "10,2"
        timestamptz data_inicio
        timestamptz data_fim
        integer status_manutencao "StatusManutencao"
        integer id_veiculo FK "FK sombra"
    }

    tb_reserva {
        integer id_reserva PK
        integer id_cliente FK
        integer id_categoria_veiculo FK
        integer id_filial FK
        timestamptz data_inicio
        timestamptz data_fim
        integer status "StatusReserva"
        boolean ativo
    }

    tb_foto_filial {
        integer id_foto PK
        integer id_filial FK "FK sombra"
        text nome_arquivo
        text raiz
        text diretorio
        text extensao
        bigint quantidade_bytes
        timestamptz data_upload
    }

    tb_foto_categoria_veiculo {
        integer id_foto PK
        integer id_categoria_veiculo FK "FK sombra"
        text nome_arquivo
        text raiz
        text diretorio
        text extensao
        bigint quantidade_bytes
        timestamptz data_upload
    }

    tb_cliente {
        integer id_cliente PK
    }

    tb_endereco          ||--o| tb_filial : "1:1"
    tb_filial            ||--o{ tb_foto_filial : "possui"
    tb_categoria_veiculo ||--o{ tb_foto_categoria_veiculo : "possui"
    tb_categoria_veiculo ||--o{ tb_veiculo : "classifica"
    tb_filial            ||--o{ tb_veiculo : "filial atual"
    tb_veiculo           ||--o{ tb_manutencao : "sofre"
    tb_cliente           ||--o{ tb_reserva : "solicita"
    tb_filial            ||--o{ tb_reserva : "atende"
    tb_categoria_veiculo ||--o{ tb_reserva : "é reservada"
```

A reserva é feita por **categoria**, não por veículo específico — a escolha da placa acontece
só na abertura da locação.

---

## 4. Locação e financeiro

```mermaid
erDiagram
    tb_locacao {
        integer id_locacao PK
        integer id_cliente FK
        integer id_veiculo FK
        integer id_funcionario FK
        integer id_filial_retirada FK
        integer id_filial_devolucao FK "anulável"
        timestamptz data_inicio
        timestamptz data_fim_prevista
        timestamptz data_fim_real
        integer km_inicial
        integer km_final
        numeric valor_previsto "10,2"
        numeric valor_final "10,2"
        numeric valor_diaria_contratada "10,2 — congelada na abertura (RN-06)"
        text status "varchar(20) — StatusLocacao"
    }

    tb_pagamento {
        integer id_pagamento PK
        numeric valor "10,2"
        timestamptz data_pagamento
        text status "StatusPagamento"
        integer id_forma_pagamento "FormaPagamento"
        integer id_locacao FK
    }

    tb_caucao {
        integer id_caucao PK
        numeric valor "10,2"
        text status "varchar(20) — StatusCaucao"
        integer id_locacao FK "FK sombra"
    }

    tb_multa {
        integer id_multa PK
        text tipo "varchar(20) — TipoMulta"
        numeric valor "10,2"
        text status "varchar(20) — StatusMulta"
        integer id_locacao FK "FK sombra"
    }

    tb_seguro {
        integer id_seguro PK
        text nome "varchar(45)"
        text descricao "varchar(100)"
        numeric valor_diaria "10,2"
        numeric franquia
        text cobertura "varchar(200)"
        boolean ativo
    }

    tb_locacao_seguro {
        integer id_locacao_seguro PK
        integer id_locacao FK
        integer id_seguro "sem FK"
        boolean ativo
        numeric valor_diaria_contratada "10,2 — congelada na contratação (RN-18)"
        numeric franquia_contratada "10,2 — congelada na contratação (RN-25)"
        timestamptz data_contratacao "desde quando cobre (RN-19)"
        timestamptz data_cancelamento "anulável — até quando cobriu (RN-19)"
    }

    tb_fechamento_locacao {
        integer id_fechamento PK
        integer id_locacao FK "UK — um por contrato (RN-32)"
        timestamptz data_apuracao
        integer id_funcionario_apuracao FK
        timestamptz data_selagem "nulo enquanto a apuração corre"
        numeric total_debitos "10,2"
        numeric total_creditos "10,2"
        numeric saldo "10,2 — assinado; negativo é crédito a devolver (RN-29)"
    }

    tb_linha_fechamento {
        integer id_linha_fechamento PK
        integer id_fechamento FK
        integer tipo "TipoLinhaFechamento"
        text base_calculo "varchar(300) — como se chegou ao número (RN-31)"
        numeric quantidade "12,4 — fracionária para pró-rata (RN-19)"
        numeric valor_unitario "10,2"
        numeric total "10,2 — arredondado por linha, AwayFromZero (RN-33)"
        timestamptz data_lancamento
        boolean eh_correcao "default false"
        integer id_funcionario_lancamento FK "anulável; obrigatório em correção e isenção"
        text motivo "varchar(500) — anulável, pela mesma regra"
    }

    tb_adicional {
        integer id_adicional PK
        text nome "varchar(50)"
        numeric valor_diaria "10,2"
        boolean ativo
    }

    tb_locacao_adicional {
        integer id_locacao_adicional PK
        integer id_adicional FK
        integer id_locacao FK
        numeric valor_diaria "ValorDiariaContratada"
        numeric valor_total
        integer quantidade
        integer dias
    }

    tb_vistoria {
        integer id_vistoria PK
        integer id_locacao FK
        integer id_funcionario FK
        integer tipo "TipoVistoria"
        integer nivel_combustivel "NivelCombustivel"
        text observacoes
        timestamptz data_vistoria
        integer km_veiculo
    }

    tb_dano {
        integer id_dano PK
        integer id_vistoria FK
        text descricao "varchar(200)"
        integer tipo_dano "TipoDano"
        numeric valor_estimado "10,2"
        integer tipo_status "StatusDano"
        timestamptz data_registro
    }

    tb_foto_vistoria {
        integer id_foto PK
        integer id_vistoria FK "FK sombra"
        text nome_arquivo
        text raiz
        text diretorio
        text extensao
        bigint quantidade_bytes
        timestamptz data_upload
    }

    historico_status_locacao {
        integer id PK
        integer id_locacao "coluna solta, sem FK"
        integer locacao_id_locacao FK "FK gerada por convenção"
        text status "varchar(20)"
        timestamptz data_status
        integer id_funcionario FK
    }

    tb_cliente {
        integer id_cliente PK
    }

    tb_veiculo {
        integer id_veiculo PK
    }

    tb_funcionario {
        integer id_funcionario PK
    }

    tb_filial {
        integer id_filial PK
    }

    tb_cliente     ||--o{ tb_locacao : "aluga"
    tb_veiculo     ||--o{ tb_locacao : "é alugado"
    tb_funcionario ||--o{ tb_locacao : "registra"
    tb_filial      ||--o{ tb_locacao : "retirada e devolução"

    tb_locacao ||--o{ tb_pagamento : "recebe"
    tb_locacao ||--o{ tb_caucao : "retém"
    tb_locacao ||--o{ tb_multa : "gera"
    tb_locacao ||--o{ tb_locacao_seguro : "contrata"
    tb_locacao ||--o{ tb_locacao_adicional : "inclui"
    tb_locacao ||--o{ tb_vistoria : "é vistoriada"
    tb_locacao ||--o{ historico_status_locacao : "trilha"

    tb_seguro    ||..o{ tb_locacao_seguro : "sem FK"
    tb_adicional ||--o{ tb_locacao_adicional : "é contratado"

    tb_vistoria    ||--o{ tb_dano : "constata"
    tb_vistoria    ||--o{ tb_foto_vistoria : "documenta"
    tb_funcionario ||--o{ tb_vistoria : "executa"
    tb_funcionario ||--o{ historico_status_locacao : "registra"
```

---

## 5. Dicionário de tabelas

| Tabela | Entidade | Papel | `ON DELETE` das FKs |
|---|---|---|---|
| `asp_net_users` | `User` | Identidade: login, CPF, nome, telefone | — |
| `asp_net_roles`, `asp_net_user_roles`, `asp_net_user_claims`, `asp_net_user_logins`, `asp_net_user_tokens`, `asp_net_role_claims` | Identity | Papéis e claims do ASP.NET Core Identity | `CASCADE` |
| `refresh_tokens` | `RefreshToken` | Tokens de renovação emitidos pela API | `CASCADE` |
| `tb_user_historico` | `UserHistorico` | Histórico temporal de `User` | sem FK |
| `tb_cliente` | `Clientes` | Perfil de cliente sobre um `User` (CNH, status, auditoria) | `CASCADE` |
| `tb_cliente_historico` | `ClienteHistorico` | Histórico temporal de `Clientes` | sem FK |
| `tb_funcionario` | `Funcionario` | Perfil de funcionário sobre um `User` (matrícula, cargo) | `CASCADE` |
| `tb_endereco` | `Endereco` | Endereço de cliente **ou** de filial | `CASCADE` |
| `tb_filial` | `Filial` | Unidade física da locadora | `CASCADE` |
| `tb_foto_filial` | `FotoFilial` | Fotos da filial | `CASCADE` |
| `tb_categoria_veiculo` | `CategoriaVeiculo` | Categoria tarifária (diária, limite de km, km excedente) | — |
| `tb_foto_categoria_veiculo` | `FotoCategoriaVeiculo` | Fotos da categoria | `CASCADE` |
| `tb_veiculo` | `Veiculo` | Veículo da frota | `RESTRICT` |
| `tb_manutencao` | `Manutencao` | Manutenções do veículo | `CASCADE` |
| `tb_reserva` | `Reserva` | Reserva de categoria em uma filial e período | `RESTRICT` (cliente/categoria), `CASCADE` (filial) |
| `tb_locacao` | `Locacao` | Contrato de locação — raiz do agregado | `RESTRICT` em todas |
| `tb_pagamento` | `Pagamento` | Pagamentos da locação | `RESTRICT` |
| `tb_caucao` | `Caucao` | Cauções retidas | `CASCADE` |
| `tb_multa` | `Multa` | Multas geradas após devolução | `CASCADE` |
| `tb_seguro` | `Seguro` | Catálogo de seguros | — |
| `tb_locacao_seguro` | `LocacaoSeguro` | Seguro contratado em uma locação | `CASCADE` (locação) |
| `tb_adicional` | `Adicional` | Catálogo de itens adicionais | — |
| `tb_locacao_adicional` | `LocacaoAdicional` | Adicional contratado, com quantidade e dias | `CASCADE` |
| `tb_vistoria` | `Vistoria` | Vistoria de retirada, devolução ou avaria | `CASCADE` |
| `tb_foto_vistoria` | `FotoVistoria` | Fotos da vistoria | `CASCADE` |
| `tb_dano` | `Dano` | Dano constatado em vistoria de devolução | `CASCADE` |
| `historico_status_locacao` | `HistoricoStatusLocacao` | Trilha de mudanças de status da locação | `CASCADE` |

### Índices únicos

| Tabela | Coluna(s) |
|---|---|
| `tb_veiculo` | `placa`, `chassi` (dois índices separados) |
| `tb_funcionario` | `matricula` |
| `tb_endereco` | `id_cliente` |
| `refresh_tokens` | `token` |

---

## 6. Como os enums são persistidos

Não há padrão único: parte dos enums é gravada como texto e parte como inteiro.

| Enum | Coluna | Tipo no banco |
|---|---|---|
| `StatusCliente` | `tb_cliente.status` | `varchar(20)` — texto |
| `StatusLocacao` | `tb_locacao.status` | `varchar(20)` — texto |
| `StatusCaucao` | `tb_caucao.status` | `varchar(20)` — texto |
| `TipoMulta` / `StatusMulta` | `tb_multa.tipo` / `.status` | `varchar(20)` — texto |
| `StatusPagamento` | `tb_pagamento.status` | `text` |
| `FormaPagamento` | `tb_pagamento.id_forma_pagamento` | `integer` |
| `StatusVeiculo` | `tb_veiculo.status` | `integer` |
| `StatusReserva` | `tb_reserva.status` | `integer` |
| `TipoVistoria` / `NivelCombustivel` | `tb_vistoria.tipo` / `.nivel_combustivel` | `integer` |
| `TipoDano` / `StatusDano` | `tb_dano.tipo_dano` / `.tipo_status` | `integer` |
| `TipoManutencao` / `StatusManutencao` | `tb_manutencao.tipo_manutencao` / `.status_manutencao` | `integer` |

---

## Observações

- **`tb_locacao_seguro.id_seguro`** não tem chave estrangeira: `LocacaoSeguroConfig` mapeia
  apenas o relacionamento com `Locacao`. A integridade referencial com `tb_seguro` fica por
  conta da aplicação.
- **`historico_status_locacao`** tem duas colunas para a mesma coisa: `id_locacao` (declarada
  na entidade, sem FK, porque o mapeamento explícito está comentado em
  `HistoricoStatusLocacaoConfig`) e `locacao_id_locacao` (`NOT NULL`, criada por convenção do EF
  a partir da navegação `Locacao`, essa sim com FK).
- **`tb_endereco`** serve a dois donos: `id_cliente` aponta para o cliente (anulável e único) e
  `tb_filial.id_endereco` aponta de volta para o endereço. Um endereço de filial fica com
  `id_cliente` nulo.
- **FKs sombra**: `tb_caucao.id_locacao`, `tb_multa.id_locacao`, `tb_manutencao.id_veiculo`,
  `tb_foto_filial.id_filial`, `tb_foto_categoria_veiculo.id_categoria_veiculo` e
  `tb_foto_vistoria.id_vistoria` existem só no banco — não há propriedade correspondente na
  classe. Todas são anuláveis, embora conceitualmente sejam obrigatórias.
- **`tb_funcionario.status`** é `boolean` e mapeia a propriedade `Ativo` — o nome da coluna
  sugere um enum de status, mas é uma flag.
- **`tb_seguro.franquia`** é `numeric` sem precisão definida, diferente dos demais valores
  monetários, que são `numeric(10,2)`.
- Buscas por texto precisam de `.ToLower()` dos dois lados: no PostgreSQL o `LIKE` distingue
  maiúsculas e acentos, ao contrário do `utf8mb4_unicode_ci` do MySQL antigo. Acento continua
  diferenciando mesmo com `ToLower()` — resolver exigiria a extensão `unaccent`.
