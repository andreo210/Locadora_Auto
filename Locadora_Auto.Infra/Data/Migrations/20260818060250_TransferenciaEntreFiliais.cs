using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Locadora_Auto.Infra.Data.Migrations
{
    /// <summary>
    /// RN-48/RN-49: remanejamento programado de frota entre filiais.
    ///
    /// A regra que dá o desenho é a RN-49 — o veículo sai da oferta da origem <b>antes</b> de
    /// entrar na do destino. Por isso a transferência é uma tabela com duas pontas (envio e
    /// chegada) e não uma troca de <c>id_filial_atual</c>: contar o mesmo carro nas duas filiais
    /// durante o trecho é overbooking involuntário, e ele aparece justamente no dia de pico.
    ///
    /// <c>permite_transferencia</c> em <c>tb_filial</c> nasce <c>true</c> para as filiais que já
    /// existiam: obrigar a rede inteira a se habilitar deixaria a frota sem circular até alguém
    /// perceber. Marcar a exceção é trabalho de quem a conhece.
    ///
    /// Nada a migrar de dado — só colunas e tabela novas.
    /// </summary>
    public partial class TransferenciaEntreFiliais : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "id_transferencia_origem",
                table: "tb_movimento_veiculo",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "permite_transferencia",
                table: "tb_filial",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "tb_transferencia_veiculo",
                columns: table => new
                {
                    id_transferencia_veiculo = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_veiculo = table.Column<int>(type: "integer", nullable: false),
                    id_filial_origem = table.Column<int>(type: "integer", nullable: false),
                    id_filial_destino = table.Column<int>(type: "integer", nullable: false),
                    data_envio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    data_prevista_chegada = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    data_chegada = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status_transferencia = table.Column<int>(type: "integer", nullable: false),
                    observacao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    id_funcionario_responsavel = table.Column<int>(type: "integer", nullable: false)

                    // O xmin que o EF gerava aqui foi removido à mão, pelo mesmo motivo que esvazia
                    // a migration ConcorrenciaOtimista: xmin é coluna de sistema do Postgres e
                    // declará-la no CREATE TABLE falha. O token de concorrência continua valendo
                    // como propriedade sombra.
                    //
                    // Se esta migration for regerada, apague a linha do xmin de novo.
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tb_transferencia_veiculo", x => x.id_transferencia_veiculo);
                    table.ForeignKey(
                        name: "fk_tb_transferencia_veiculo_tb_filial_id_filial_destino",
                        column: x => x.id_filial_destino,
                        principalTable: "tb_filial",
                        principalColumn: "id_filial",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tb_transferencia_veiculo_tb_filial_id_filial_origem",
                        column: x => x.id_filial_origem,
                        principalTable: "tb_filial",
                        principalColumn: "id_filial",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tb_transferencia_veiculo_tb_funcionario_id_funcionario_resp",
                        column: x => x.id_funcionario_responsavel,
                        principalTable: "tb_funcionario",
                        principalColumn: "id_funcionario",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tb_transferencia_veiculo_tb_veiculo_id_veiculo",
                        column: x => x.id_veiculo,
                        principalTable: "tb_veiculo",
                        principalColumn: "id_veiculo",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_tb_movimento_veiculo_id_transferencia_origem",
                table: "tb_movimento_veiculo",
                column: "id_transferencia_origem");

            migrationBuilder.CreateIndex(
                name: "ix_tb_transferencia_veiculo_data_prevista_chegada",
                table: "tb_transferencia_veiculo",
                column: "data_prevista_chegada");

            migrationBuilder.CreateIndex(
                name: "ix_tb_transferencia_veiculo_id_filial_destino",
                table: "tb_transferencia_veiculo",
                column: "id_filial_destino");

            migrationBuilder.CreateIndex(
                name: "ix_tb_transferencia_veiculo_id_filial_origem",
                table: "tb_transferencia_veiculo",
                column: "id_filial_origem");

            migrationBuilder.CreateIndex(
                name: "ix_tb_transferencia_veiculo_id_funcionario_responsavel",
                table: "tb_transferencia_veiculo",
                column: "id_funcionario_responsavel");

            migrationBuilder.CreateIndex(
                name: "ix_tb_transferencia_veiculo_id_veiculo_status_transferencia",
                table: "tb_transferencia_veiculo",
                columns: new[] { "id_veiculo", "status_transferencia" });

            migrationBuilder.AddForeignKey(
                name: "fk_tb_movimento_veiculo_tb_transferencia_veiculo_id_transferen",
                table: "tb_movimento_veiculo",
                column: "id_transferencia_origem",
                principalTable: "tb_transferencia_veiculo",
                principalColumn: "id_transferencia_veiculo",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_tb_movimento_veiculo_tb_transferencia_veiculo_id_transferen",
                table: "tb_movimento_veiculo");

            migrationBuilder.DropTable(
                name: "tb_transferencia_veiculo");

            migrationBuilder.DropIndex(
                name: "ix_tb_movimento_veiculo_id_transferencia_origem",
                table: "tb_movimento_veiculo");

            migrationBuilder.DropColumn(
                name: "id_transferencia_origem",
                table: "tb_movimento_veiculo");

            migrationBuilder.DropColumn(
                name: "permite_transferencia",
                table: "tb_filial");
        }
    }
}
