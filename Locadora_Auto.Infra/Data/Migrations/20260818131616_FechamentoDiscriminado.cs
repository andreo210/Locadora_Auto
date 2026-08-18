using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Locadora_Auto.Infra.Data.Migrations
{
    /// <inheritdoc />
    public partial class FechamentoDiscriminado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tb_fechamento_locacao",
                columns: table => new
                {
                    id_fechamento = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_locacao = table.Column<int>(type: "integer", nullable: false),
                    data_apuracao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    id_funcionario_apuracao = table.Column<int>(type: "integer", nullable: false),
                    data_selagem = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    total_debitos = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    total_creditos = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    saldo = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false)

                    // O xmin que o EF gerava aqui foi removido à mão, pelo mesmo motivo que esvazia
                    // a migration ConcorrenciaOtimista: xmin é coluna de sistema do Postgres e
                    // declará-la no CREATE TABLE falha com "column name xmin conflicts with a
                    // system column name". O token de concorrência continua valendo como
                    // propriedade sombra.
                    //
                    // Se esta migration for regerada, apague a linha do xmin de novo.
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tb_fechamento_locacao", x => x.id_fechamento);
                    table.ForeignKey(
                        name: "fk_tb_fechamento_locacao_tb_funcionario_id_funcionario_apuracao",
                        column: x => x.id_funcionario_apuracao,
                        principalTable: "tb_funcionario",
                        principalColumn: "id_funcionario",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tb_fechamento_locacao_tb_locacao_id_locacao",
                        column: x => x.id_locacao,
                        principalTable: "tb_locacao",
                        principalColumn: "id_locacao",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tb_linha_fechamento",
                columns: table => new
                {
                    id_linha_fechamento = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_fechamento = table.Column<int>(type: "integer", nullable: false),
                    tipo = table.Column<int>(type: "integer", nullable: false),
                    base_calculo = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    quantidade = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    valor_unitario = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    total = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    data_lancamento = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    eh_correcao = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    id_funcionario_lancamento = table.Column<int>(type: "integer", nullable: true),
                    motivo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)

                    // Mesma remoção do xmin da tabela acima, pelo mesmo motivo.
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tb_linha_fechamento", x => x.id_linha_fechamento);
                    table.ForeignKey(
                        name: "fk_tb_linha_fechamento_tb_fechamento_locacao_id_fechamento",
                        column: x => x.id_fechamento,
                        principalTable: "tb_fechamento_locacao",
                        principalColumn: "id_fechamento",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_tb_linha_fechamento_tb_funcionario_id_funcionario_lancamento",
                        column: x => x.id_funcionario_lancamento,
                        principalTable: "tb_funcionario",
                        principalColumn: "id_funcionario",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_tb_fechamento_locacao_id_funcionario_apuracao",
                table: "tb_fechamento_locacao",
                column: "id_funcionario_apuracao");

            migrationBuilder.CreateIndex(
                name: "ix_tb_fechamento_locacao_id_locacao",
                table: "tb_fechamento_locacao",
                column: "id_locacao",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tb_linha_fechamento_id_fechamento_id_linha_fechamento",
                table: "tb_linha_fechamento",
                columns: new[] { "id_fechamento", "id_linha_fechamento" });

            migrationBuilder.CreateIndex(
                name: "ix_tb_linha_fechamento_id_funcionario_lancamento",
                table: "tb_linha_fechamento",
                column: "id_funcionario_lancamento");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tb_linha_fechamento");

            migrationBuilder.DropTable(
                name: "tb_fechamento_locacao");
        }
    }
}
