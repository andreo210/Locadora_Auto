using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Locadora_Auto.Infra.Data.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "asp_net_roles",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    concurrency_stamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "asp_net_users",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    nome_completo = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    cpf = table.Column<string>(type: "text", nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    data_criacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    user_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_user_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    email_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: true),
                    security_stamp = table.Column<string>(type: "text", nullable: true),
                    concurrency_stamp = table.Column<string>(type: "text", nullable: true),
                    phone_number = table.Column<string>(type: "text", nullable: true),
                    phone_number_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    two_factor_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    lockout_end = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    lockout_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    access_failed_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tb_adicional",
                columns: table => new
                {
                    id_adicional = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nome = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    valor_diaria = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tb_adicional", x => x.id_adicional);
                });

            migrationBuilder.CreateTable(
                name: "tb_categoria_veiculo",
                columns: table => new
                {
                    id_categoria = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nome = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    valor_diaria = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    limite_km = table.Column<int>(type: "integer", nullable: true),
                    valor_km_excedente = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tb_categoria_veiculo", x => x.id_categoria);
                });

            migrationBuilder.CreateTable(
                name: "tb_cliente_historico",
                columns: table => new
                {
                    id_historico = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_cliente = table.Column<int>(type: "integer", nullable: false),
                    data_evento = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    acao = table.Column<string>(type: "text", nullable: true),
                    usuario_evento = table.Column<string>(type: "text", nullable: true),
                    numero_habilitacao = table.Column<string>(type: "text", nullable: true),
                    validade_habilitacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    total_locacoes = table.Column<int>(type: "integer", nullable: false),
                    id_usuario_modificacao = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tb_cliente_historico", x => x.id_historico);
                });

            migrationBuilder.CreateTable(
                name: "tb_seguro",
                columns: table => new
                {
                    id_seguro = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    descricao = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    nome = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    valor_diaria = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    franquia = table.Column<decimal>(type: "numeric", nullable: false),
                    cobertura = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tb_seguro", x => x.id_seguro);
                });

            migrationBuilder.CreateTable(
                name: "tb_user_historico",
                columns: table => new
                {
                    id_historico = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id = table.Column<string>(type: "text", nullable: true),
                    nome_completo = table.Column<string>(type: "text", nullable: true),
                    email = table.Column<string>(type: "text", nullable: true),
                    phone_number = table.Column<string>(type: "text", nullable: true),
                    data_evento = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    acao = table.Column<string>(type: "text", nullable: true),
                    usuario_evento = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tb_user_historico", x => x.id_historico);
                });

            migrationBuilder.CreateTable(
                name: "asp_net_role_claims",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    role_id = table.Column<string>(type: "text", nullable: false),
                    claim_type = table.Column<string>(type: "text", nullable: true),
                    claim_value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_role_claims", x => x.id);
                    table.ForeignKey(
                        name: "fk_asp_net_role_claims_asp_net_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "asp_net_roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "asp_net_user_claims",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<string>(type: "text", nullable: false),
                    claim_type = table.Column<string>(type: "text", nullable: true),
                    claim_value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_user_claims", x => x.id);
                    table.ForeignKey(
                        name: "fk_asp_net_user_claims_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "asp_net_user_logins",
                columns: table => new
                {
                    login_provider = table.Column<string>(type: "text", nullable: false),
                    provider_key = table.Column<string>(type: "text", nullable: false),
                    provider_display_name = table.Column<string>(type: "text", nullable: true),
                    user_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_user_logins", x => new { x.login_provider, x.provider_key });
                    table.ForeignKey(
                        name: "fk_asp_net_user_logins_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "asp_net_user_roles",
                columns: table => new
                {
                    user_id = table.Column<string>(type: "text", nullable: false),
                    role_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_user_roles", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "fk_asp_net_user_roles_asp_net_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "asp_net_roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_asp_net_user_roles_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "asp_net_user_tokens",
                columns: table => new
                {
                    user_id = table.Column<string>(type: "text", nullable: false),
                    login_provider = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_user_tokens", x => new { x.user_id, x.login_provider, x.name });
                    table.ForeignKey(
                        name: "fk_asp_net_user_tokens_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    token = table.Column<string>(type: "text", nullable: false),
                    expira_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    revogado = table.Column<bool>(type: "boolean", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    user_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_refresh_tokens_users_user_id",
                        column: x => x.user_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tb_cliente",
                columns: table => new
                {
                    id_cliente = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    numero_habilitacao = table.Column<string>(type: "text", nullable: true),
                    validade_habilitacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    total_locacoes = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    data_criacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    id_usuario_criacao = table.Column<string>(type: "text", nullable: true),
                    data_modificacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    id_usuario_modificacao = table.Column<string>(type: "text", nullable: true),
                    id_asp_net_users = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tb_cliente", x => x.id_cliente);
                    table.ForeignKey(
                        name: "fk_tb_cliente_users_id_asp_net_users",
                        column: x => x.id_asp_net_users,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tb_funcionario",
                columns: table => new
                {
                    id_funcionario = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    matricula = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    cargo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    status = table.Column<bool>(type: "boolean", nullable: false),
                    id_user = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tb_funcionario", x => x.id_funcionario);
                    table.ForeignKey(
                        name: "fk_tb_funcionario_users_id_user",
                        column: x => x.id_user,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tb_foto_categoria_veiculo",
                columns: table => new
                {
                    id_foto = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_categoria_veiculo = table.Column<int>(type: "integer", nullable: true),
                    nome_arquivo = table.Column<string>(type: "text", nullable: true),
                    raiz = table.Column<string>(type: "text", nullable: true),
                    diretorio = table.Column<string>(type: "text", nullable: true),
                    extensao = table.Column<string>(type: "text", nullable: true),
                    quantidade_bytes = table.Column<long>(type: "bigint", nullable: true),
                    data_upload = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tb_foto_categoria_veiculo", x => x.id_foto);
                    table.ForeignKey(
                        name: "fk_tb_foto_categoria_veiculo_tb_categoria_veiculo_id_categoria",
                        column: x => x.id_categoria_veiculo,
                        principalTable: "tb_categoria_veiculo",
                        principalColumn: "id_categoria",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tb_endereco",
                columns: table => new
                {
                    id_endereco = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_cliente = table.Column<int>(type: "integer", nullable: true),
                    logradouro = table.Column<string>(type: "text", nullable: false),
                    numero = table.Column<string>(type: "text", nullable: false),
                    complemento = table.Column<string>(type: "text", nullable: true),
                    bairro = table.Column<string>(type: "text", nullable: false),
                    cidade = table.Column<string>(type: "text", nullable: false),
                    estado = table.Column<string>(type: "text", nullable: false),
                    cep = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tb_endereco", x => x.id_endereco);
                    table.ForeignKey(
                        name: "fk_tb_endereco_tb_cliente_id_cliente",
                        column: x => x.id_cliente,
                        principalTable: "tb_cliente",
                        principalColumn: "id_cliente",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tb_filial",
                columns: table => new
                {
                    id_filial = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    cidade = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    id_endereco = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tb_filial", x => x.id_filial);
                    table.ForeignKey(
                        name: "fk_tb_filial_tb_endereco_id_endereco",
                        column: x => x.id_endereco,
                        principalTable: "tb_endereco",
                        principalColumn: "id_endereco",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tb_foto_filial",
                columns: table => new
                {
                    id_foto = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_filial = table.Column<int>(type: "integer", nullable: true),
                    nome_arquivo = table.Column<string>(type: "text", nullable: true),
                    raiz = table.Column<string>(type: "text", nullable: true),
                    diretorio = table.Column<string>(type: "text", nullable: true),
                    extensao = table.Column<string>(type: "text", nullable: true),
                    quantidade_bytes = table.Column<long>(type: "bigint", nullable: true),
                    data_upload = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tb_foto_filial", x => x.id_foto);
                    table.ForeignKey(
                        name: "fk_tb_foto_filial_tb_filial_id_filial",
                        column: x => x.id_filial,
                        principalTable: "tb_filial",
                        principalColumn: "id_filial",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tb_reserva",
                columns: table => new
                {
                    id_reserva = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_cliente = table.Column<int>(type: "integer", nullable: false),
                    id_categoria_veiculo = table.Column<int>(type: "integer", nullable: false),
                    id_filial = table.Column<int>(type: "integer", nullable: false),
                    data_inicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    data_fim = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<int>(type: "integer", maxLength: 20, nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tb_reserva", x => x.id_reserva);
                    table.ForeignKey(
                        name: "fk_tb_reserva_tb_categoria_veiculo_id_categoria_veiculo",
                        column: x => x.id_categoria_veiculo,
                        principalTable: "tb_categoria_veiculo",
                        principalColumn: "id_categoria",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tb_reserva_tb_cliente_id_cliente",
                        column: x => x.id_cliente,
                        principalTable: "tb_cliente",
                        principalColumn: "id_cliente",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tb_reserva_tb_filial_id_filial",
                        column: x => x.id_filial,
                        principalTable: "tb_filial",
                        principalColumn: "id_filial",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tb_veiculo",
                columns: table => new
                {
                    id_veiculo = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    placa = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    marca = table.Column<string>(type: "text", nullable: false),
                    modelo = table.Column<string>(type: "text", nullable: false),
                    ano = table.Column<int>(type: "integer", nullable: false),
                    chassi = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    id_categoria = table.Column<int>(type: "integer", nullable: false),
                    km_atual = table.Column<int>(type: "integer", nullable: false),
                    ativo = table.Column<bool>(type: "boolean", maxLength: 20, nullable: false),
                    disponivel = table.Column<bool>(type: "boolean", nullable: false),
                    id_filial_atual = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tb_veiculo", x => x.id_veiculo);
                    table.ForeignKey(
                        name: "fk_tb_veiculo_tb_categoria_veiculo_id_categoria",
                        column: x => x.id_categoria,
                        principalTable: "tb_categoria_veiculo",
                        principalColumn: "id_categoria",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tb_veiculo_tb_filial_filial_atual_id",
                        column: x => x.id_filial_atual,
                        principalTable: "tb_filial",
                        principalColumn: "id_filial",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tb_locacao",
                columns: table => new
                {
                    id_locacao = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_cliente = table.Column<int>(type: "integer", nullable: false),
                    id_veiculo = table.Column<int>(type: "integer", nullable: false),
                    id_funcionario = table.Column<int>(type: "integer", nullable: false),
                    id_filial_retirada = table.Column<int>(type: "integer", nullable: false),
                    id_filial_devolucao = table.Column<int>(type: "integer", nullable: true),
                    data_inicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    data_fim_prevista = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    data_fim_real = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    km_inicial = table.Column<int>(type: "integer", nullable: false),
                    km_final = table.Column<int>(type: "integer", nullable: true),
                    valor_previsto = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    valor_final = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tb_locacao", x => x.id_locacao);
                    table.ForeignKey(
                        name: "fk_tb_locacao_tb_cliente_cliente_id",
                        column: x => x.id_cliente,
                        principalTable: "tb_cliente",
                        principalColumn: "id_cliente",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tb_locacao_tb_filial_id_filial_devolucao",
                        column: x => x.id_filial_devolucao,
                        principalTable: "tb_filial",
                        principalColumn: "id_filial",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tb_locacao_tb_filial_id_filial_retirada",
                        column: x => x.id_filial_retirada,
                        principalTable: "tb_filial",
                        principalColumn: "id_filial",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tb_locacao_tb_funcionario_id_funcionario",
                        column: x => x.id_funcionario,
                        principalTable: "tb_funcionario",
                        principalColumn: "id_funcionario",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tb_locacao_tb_veiculo_id_veiculo",
                        column: x => x.id_veiculo,
                        principalTable: "tb_veiculo",
                        principalColumn: "id_veiculo",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tb_manutencao",
                columns: table => new
                {
                    id_manutencao = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tipo_manutencao = table.Column<int>(type: "integer", nullable: false),
                    descricao = table.Column<string>(type: "text", nullable: true),
                    custo = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    data_inicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    data_fim = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status_manutencao = table.Column<int>(type: "integer", nullable: false),
                    id_veiculo = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tb_manutencao", x => x.id_manutencao);
                    table.ForeignKey(
                        name: "fk_tb_manutencao_tb_veiculo_id_veiculo",
                        column: x => x.id_veiculo,
                        principalTable: "tb_veiculo",
                        principalColumn: "id_veiculo",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "historico_status_locacao",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_locacao = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    data_status = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    id_funcionario = table.Column<int>(type: "integer", nullable: false),
                    locacao_id_locacao = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_historico_status_locacao", x => x.id);
                    table.ForeignKey(
                        name: "fk_historico_status_locacao_tb_funcionario_id_funcionario",
                        column: x => x.id_funcionario,
                        principalTable: "tb_funcionario",
                        principalColumn: "id_funcionario",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_historico_status_locacao_tb_locacao_locacao_id_locacao",
                        column: x => x.locacao_id_locacao,
                        principalTable: "tb_locacao",
                        principalColumn: "id_locacao",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tb_caucao",
                columns: table => new
                {
                    id_caucao = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    valor = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    id_locacao = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tb_caucao", x => x.id_caucao);
                    table.ForeignKey(
                        name: "fk_tb_caucao_tb_locacao_id_locacao",
                        column: x => x.id_locacao,
                        principalTable: "tb_locacao",
                        principalColumn: "id_locacao",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tb_locacao_adicional",
                columns: table => new
                {
                    id_locacao_adicional = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_adicional = table.Column<int>(type: "integer", nullable: false),
                    id_locacao = table.Column<int>(type: "integer", nullable: false),
                    valor_diaria = table.Column<decimal>(type: "numeric", nullable: false),
                    valor_total = table.Column<decimal>(type: "numeric", nullable: false),
                    quantidade = table.Column<int>(type: "integer", nullable: false),
                    dias = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tb_locacao_adicional", x => x.id_locacao_adicional);
                    table.ForeignKey(
                        name: "fk_tb_locacao_adicional_tb_adicional_id_adicional",
                        column: x => x.id_adicional,
                        principalTable: "tb_adicional",
                        principalColumn: "id_adicional",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_tb_locacao_adicional_tb_locacao_id_locacao",
                        column: x => x.id_locacao,
                        principalTable: "tb_locacao",
                        principalColumn: "id_locacao",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tb_locacao_seguro",
                columns: table => new
                {
                    id_locacao_seguro = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_locacao = table.Column<int>(type: "integer", nullable: false),
                    id_seguro = table.Column<int>(type: "integer", nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tb_locacao_seguro", x => x.id_locacao_seguro);
                    table.ForeignKey(
                        name: "fk_tb_locacao_seguro_tb_locacao_id_locacao",
                        column: x => x.id_locacao,
                        principalTable: "tb_locacao",
                        principalColumn: "id_locacao",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tb_multa",
                columns: table => new
                {
                    id_multa = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    valor = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    id_locacao = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tb_multa", x => x.id_multa);
                    table.ForeignKey(
                        name: "fk_tb_multa_tb_locacao_id_locacao",
                        column: x => x.id_locacao,
                        principalTable: "tb_locacao",
                        principalColumn: "id_locacao",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tb_pagamento",
                columns: table => new
                {
                    id_pagamento = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    valor = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    data_pagamento = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    id_forma_pagamento = table.Column<int>(type: "integer", nullable: false),
                    id_locacao = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tb_pagamento", x => x.id_pagamento);
                    table.ForeignKey(
                        name: "fk_tb_pagamento_tb_locacao_id_locacao",
                        column: x => x.id_locacao,
                        principalTable: "tb_locacao",
                        principalColumn: "id_locacao",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tb_vistoria",
                columns: table => new
                {
                    id_vistoria = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_locacao = table.Column<int>(type: "integer", nullable: false),
                    tipo = table.Column<int>(type: "integer", nullable: false),
                    nivel_combustivel = table.Column<int>(type: "integer", nullable: false),
                    observacoes = table.Column<string>(type: "text", nullable: true),
                    data_vistoria = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    id_funcionario = table.Column<int>(type: "integer", nullable: false),
                    km_veiculo = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tb_vistoria", x => x.id_vistoria);
                    table.ForeignKey(
                        name: "fk_tb_vistoria_tb_funcionario_id_funcionario",
                        column: x => x.id_funcionario,
                        principalTable: "tb_funcionario",
                        principalColumn: "id_funcionario",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_tb_vistoria_tb_locacao_id_locacao",
                        column: x => x.id_locacao,
                        principalTable: "tb_locacao",
                        principalColumn: "id_locacao",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tb_dano",
                columns: table => new
                {
                    id_dano = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_vistoria = table.Column<int>(type: "integer", nullable: false),
                    descricao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    tipo_dano = table.Column<int>(type: "integer", nullable: false),
                    valor_estimado = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    tipo_status = table.Column<int>(type: "integer", nullable: false),
                    data_registro = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tb_dano", x => x.id_dano);
                    table.ForeignKey(
                        name: "fk_tb_dano_tb_vistoria_id_vistoria",
                        column: x => x.id_vistoria,
                        principalTable: "tb_vistoria",
                        principalColumn: "id_vistoria",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tb_foto_vistoria",
                columns: table => new
                {
                    id_foto = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_vistoria = table.Column<int>(type: "integer", nullable: true),
                    nome_arquivo = table.Column<string>(type: "text", nullable: true),
                    raiz = table.Column<string>(type: "text", nullable: true),
                    diretorio = table.Column<string>(type: "text", nullable: true),
                    extensao = table.Column<string>(type: "text", nullable: true),
                    quantidade_bytes = table.Column<long>(type: "bigint", nullable: true),
                    data_upload = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tb_foto_vistoria", x => x.id_foto);
                    table.ForeignKey(
                        name: "fk_tb_foto_vistoria_tb_vistoria_id_vistoria",
                        column: x => x.id_vistoria,
                        principalTable: "tb_vistoria",
                        principalColumn: "id_vistoria",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_asp_net_role_claims_role_id",
                table: "asp_net_role_claims",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "asp_net_roles",
                column: "normalized_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_asp_net_user_claims_user_id",
                table: "asp_net_user_claims",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_asp_net_user_logins_user_id",
                table: "asp_net_user_logins",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_asp_net_user_roles_role_id",
                table: "asp_net_user_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "asp_net_users",
                column: "normalized_email");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "asp_net_users",
                column: "normalized_user_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_historico_status_locacao_id_funcionario",
                table: "historico_status_locacao",
                column: "id_funcionario");

            migrationBuilder.CreateIndex(
                name: "ix_historico_status_locacao_locacao_id_locacao",
                table: "historico_status_locacao",
                column: "locacao_id_locacao");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_token",
                table: "refresh_tokens",
                column: "token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_user_id",
                table: "refresh_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_tb_caucao_id_locacao",
                table: "tb_caucao",
                column: "id_locacao");

            migrationBuilder.CreateIndex(
                name: "ix_tb_cliente_id_asp_net_users",
                table: "tb_cliente",
                column: "id_asp_net_users",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tb_dano_id_vistoria",
                table: "tb_dano",
                column: "id_vistoria");

            migrationBuilder.CreateIndex(
                name: "ix_tb_endereco_id_cliente",
                table: "tb_endereco",
                column: "id_cliente",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tb_filial_id_endereco",
                table: "tb_filial",
                column: "id_endereco",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tb_foto_categoria_veiculo_id_categoria_veiculo",
                table: "tb_foto_categoria_veiculo",
                column: "id_categoria_veiculo");

            migrationBuilder.CreateIndex(
                name: "ix_tb_foto_filial_id_filial",
                table: "tb_foto_filial",
                column: "id_filial");

            migrationBuilder.CreateIndex(
                name: "ix_tb_foto_vistoria_id_vistoria",
                table: "tb_foto_vistoria",
                column: "id_vistoria");

            migrationBuilder.CreateIndex(
                name: "ix_tb_funcionario_id_user",
                table: "tb_funcionario",
                column: "id_user",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tb_funcionario_matricula",
                table: "tb_funcionario",
                column: "matricula",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tb_locacao_cliente_id",
                table: "tb_locacao",
                column: "id_cliente");

            migrationBuilder.CreateIndex(
                name: "ix_tb_locacao_id_filial_devolucao",
                table: "tb_locacao",
                column: "id_filial_devolucao");

            migrationBuilder.CreateIndex(
                name: "ix_tb_locacao_id_filial_retirada",
                table: "tb_locacao",
                column: "id_filial_retirada");

            migrationBuilder.CreateIndex(
                name: "ix_tb_locacao_id_funcionario",
                table: "tb_locacao",
                column: "id_funcionario");

            migrationBuilder.CreateIndex(
                name: "ix_tb_locacao_id_veiculo",
                table: "tb_locacao",
                column: "id_veiculo");

            migrationBuilder.CreateIndex(
                name: "ix_tb_locacao_adicional_id_adicional",
                table: "tb_locacao_adicional",
                column: "id_adicional");

            migrationBuilder.CreateIndex(
                name: "ix_tb_locacao_adicional_id_locacao",
                table: "tb_locacao_adicional",
                column: "id_locacao");

            migrationBuilder.CreateIndex(
                name: "ix_tb_locacao_seguro_id_locacao",
                table: "tb_locacao_seguro",
                column: "id_locacao");

            migrationBuilder.CreateIndex(
                name: "ix_tb_manutencao_id_veiculo",
                table: "tb_manutencao",
                column: "id_veiculo");

            migrationBuilder.CreateIndex(
                name: "ix_tb_multa_id_locacao",
                table: "tb_multa",
                column: "id_locacao");

            migrationBuilder.CreateIndex(
                name: "ix_tb_pagamento_id_locacao",
                table: "tb_pagamento",
                column: "id_locacao");

            migrationBuilder.CreateIndex(
                name: "ix_tb_reserva_id_categoria_veiculo",
                table: "tb_reserva",
                column: "id_categoria_veiculo");

            migrationBuilder.CreateIndex(
                name: "ix_tb_reserva_id_cliente",
                table: "tb_reserva",
                column: "id_cliente");

            migrationBuilder.CreateIndex(
                name: "ix_tb_reserva_id_filial",
                table: "tb_reserva",
                column: "id_filial");

            migrationBuilder.CreateIndex(
                name: "ix_tb_veiculo_chassi",
                table: "tb_veiculo",
                column: "chassi",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tb_veiculo_filial_atual_id",
                table: "tb_veiculo",
                column: "id_filial_atual");

            migrationBuilder.CreateIndex(
                name: "ix_tb_veiculo_id_categoria",
                table: "tb_veiculo",
                column: "id_categoria");

            migrationBuilder.CreateIndex(
                name: "ix_tb_veiculo_placa",
                table: "tb_veiculo",
                column: "placa",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tb_vistoria_id_funcionario",
                table: "tb_vistoria",
                column: "id_funcionario");

            migrationBuilder.CreateIndex(
                name: "ix_tb_vistoria_id_locacao",
                table: "tb_vistoria",
                column: "id_locacao");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "asp_net_role_claims");

            migrationBuilder.DropTable(
                name: "asp_net_user_claims");

            migrationBuilder.DropTable(
                name: "asp_net_user_logins");

            migrationBuilder.DropTable(
                name: "asp_net_user_roles");

            migrationBuilder.DropTable(
                name: "asp_net_user_tokens");

            migrationBuilder.DropTable(
                name: "historico_status_locacao");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "tb_caucao");

            migrationBuilder.DropTable(
                name: "tb_cliente_historico");

            migrationBuilder.DropTable(
                name: "tb_dano");

            migrationBuilder.DropTable(
                name: "tb_foto_categoria_veiculo");

            migrationBuilder.DropTable(
                name: "tb_foto_filial");

            migrationBuilder.DropTable(
                name: "tb_foto_vistoria");

            migrationBuilder.DropTable(
                name: "tb_locacao_adicional");

            migrationBuilder.DropTable(
                name: "tb_locacao_seguro");

            migrationBuilder.DropTable(
                name: "tb_manutencao");

            migrationBuilder.DropTable(
                name: "tb_multa");

            migrationBuilder.DropTable(
                name: "tb_pagamento");

            migrationBuilder.DropTable(
                name: "tb_reserva");

            migrationBuilder.DropTable(
                name: "tb_seguro");

            migrationBuilder.DropTable(
                name: "tb_user_historico");

            migrationBuilder.DropTable(
                name: "asp_net_roles");

            migrationBuilder.DropTable(
                name: "tb_vistoria");

            migrationBuilder.DropTable(
                name: "tb_adicional");

            migrationBuilder.DropTable(
                name: "tb_locacao");

            migrationBuilder.DropTable(
                name: "tb_funcionario");

            migrationBuilder.DropTable(
                name: "tb_veiculo");

            migrationBuilder.DropTable(
                name: "tb_categoria_veiculo");

            migrationBuilder.DropTable(
                name: "tb_filial");

            migrationBuilder.DropTable(
                name: "tb_endereco");

            migrationBuilder.DropTable(
                name: "tb_cliente");

            migrationBuilder.DropTable(
                name: "asp_net_users");
        }
    }
}
