using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Locadora_Auto.Infra.Data.Migrations
{
    /// <inheritdoc />
    public partial class MovimentoVeiculo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tb_movimento_veiculo",
                columns: table => new
                {
                    id_movimento_veiculo = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_veiculo = table.Column<int>(type: "integer", nullable: false),
                    status_origem = table.Column<int>(type: "integer", nullable: true),
                    status_destino = table.Column<int>(type: "integer", nullable: false),
                    tipo_origem = table.Column<int>(type: "integer", nullable: false),
                    id_locacao_origem = table.Column<int>(type: "integer", nullable: true),
                    id_manutencao_origem = table.Column<int>(type: "integer", nullable: true),
                    data_movimento = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    data_criacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    id_usuario_criacao = table.Column<string>(type: "text", nullable: true),
                    data_modificacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    id_usuario_modificacao = table.Column<string>(type: "text", nullable: true)

                    // O xmin que o EF gerava aqui foi removido à mão, pelo mesmo motivo que esvazia
                    // a migration ConcorrenciaOtimista: xmin é coluna de sistema do Postgres e
                    // declará-la no CREATE TABLE falha com "column name xmin conflicts with a
                    // system column name". O token de concorrência continua valendo — ele vem de
                    // AplicarTokenDeConcorrencia como propriedade sombra, sem coluna própria.
                    // Se esta migration for regerada, apague a linha do xmin de novo.
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tb_movimento_veiculo", x => x.id_movimento_veiculo);
                    table.ForeignKey(
                        name: "fk_tb_movimento_veiculo_tb_locacao_id_locacao_origem",
                        column: x => x.id_locacao_origem,
                        principalTable: "tb_locacao",
                        principalColumn: "id_locacao",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tb_movimento_veiculo_tb_manutencao_id_manutencao_origem",
                        column: x => x.id_manutencao_origem,
                        principalTable: "tb_manutencao",
                        principalColumn: "id_manutencao",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_tb_movimento_veiculo_tb_veiculo_id_veiculo",
                        column: x => x.id_veiculo,
                        principalTable: "tb_veiculo",
                        principalColumn: "id_veiculo",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_tb_movimento_veiculo_id_locacao_origem",
                table: "tb_movimento_veiculo",
                column: "id_locacao_origem");

            migrationBuilder.CreateIndex(
                name: "ix_tb_movimento_veiculo_id_manutencao_origem",
                table: "tb_movimento_veiculo",
                column: "id_manutencao_origem");

            migrationBuilder.CreateIndex(
                name: "ix_tb_movimento_veiculo_id_veiculo_data_movimento",
                table: "tb_movimento_veiculo",
                columns: new[] { "id_veiculo", "data_movimento" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tb_movimento_veiculo");
        }
    }
}
