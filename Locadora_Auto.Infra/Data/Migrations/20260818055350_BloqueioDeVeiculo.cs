using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Locadora_Auto.Infra.Data.Migrations
{
    /// <summary>
    /// RN-52: bloqueio com motivo, prazo e responsável. Até aqui, tirar um carro da oferta era
    /// mudar o status para "Indisponivel" e mais nada — ninguém sabia por que, até quando, nem a
    /// quem cobrar. A tabela é o documento de origem desse movimento (RN-37), no mesmo papel do
    /// contrato e da ordem de serviço, e é dela que sai o indicador de bloqueios vencidos da
    /// seção 12.
    ///
    /// A coluna id_bloqueio_origem em tb_movimento_veiculo fecha a trilha nas duas pontas: o
    /// movimento que tira o carro da oferta e o que o devolve citam o mesmo bloqueio, e é isso que
    /// permite medir quanto tempo cada motivo segurou a frota.
    ///
    /// Nada a migrar de dado: o enum StatusVeiculo só foi renomeado (Indisponivel -> Bloqueado) e
    /// a coluna guarda o int, que continua 2.
    /// </summary>
    public partial class BloqueioDeVeiculo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "id_bloqueio_origem",
                table: "tb_movimento_veiculo",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "tb_bloqueio_veiculo",
                columns: table => new
                {
                    id_bloqueio_veiculo = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_veiculo = table.Column<int>(type: "integer", nullable: false),
                    motivo = table.Column<int>(type: "integer", nullable: false),
                    observacao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    data_bloqueio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    data_prevista_liberacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    data_liberacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status_anterior = table.Column<int>(type: "integer", nullable: false),
                    id_funcionario_responsavel = table.Column<int>(type: "integer", nullable: false)

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
                    table.PrimaryKey("pk_tb_bloqueio_veiculo", x => x.id_bloqueio_veiculo);
                    table.ForeignKey(
                        name: "fk_tb_bloqueio_veiculo_tb_funcionario_id_funcionario_responsav",
                        column: x => x.id_funcionario_responsavel,
                        principalTable: "tb_funcionario",
                        principalColumn: "id_funcionario",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tb_bloqueio_veiculo_tb_veiculo_id_veiculo",
                        column: x => x.id_veiculo,
                        principalTable: "tb_veiculo",
                        principalColumn: "id_veiculo",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_tb_movimento_veiculo_id_bloqueio_origem",
                table: "tb_movimento_veiculo",
                column: "id_bloqueio_origem");

            migrationBuilder.CreateIndex(
                name: "ix_tb_bloqueio_veiculo_data_prevista_liberacao",
                table: "tb_bloqueio_veiculo",
                column: "data_prevista_liberacao");

            migrationBuilder.CreateIndex(
                name: "ix_tb_bloqueio_veiculo_id_funcionario_responsavel",
                table: "tb_bloqueio_veiculo",
                column: "id_funcionario_responsavel");

            migrationBuilder.CreateIndex(
                name: "ix_tb_bloqueio_veiculo_id_veiculo_data_liberacao",
                table: "tb_bloqueio_veiculo",
                columns: new[] { "id_veiculo", "data_liberacao" });

            migrationBuilder.AddForeignKey(
                name: "fk_tb_movimento_veiculo_tb_bloqueio_veiculo_id_bloqueio_origem",
                table: "tb_movimento_veiculo",
                column: "id_bloqueio_origem",
                principalTable: "tb_bloqueio_veiculo",
                principalColumn: "id_bloqueio_veiculo",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_tb_movimento_veiculo_tb_bloqueio_veiculo_id_bloqueio_origem",
                table: "tb_movimento_veiculo");

            migrationBuilder.DropTable(
                name: "tb_bloqueio_veiculo");

            migrationBuilder.DropIndex(
                name: "ix_tb_movimento_veiculo_id_bloqueio_origem",
                table: "tb_movimento_veiculo");

            migrationBuilder.DropColumn(
                name: "id_bloqueio_origem",
                table: "tb_movimento_veiculo");
        }
    }
}
