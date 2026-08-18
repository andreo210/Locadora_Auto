using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Locadora_Auto.Infra.Data.Migrations
{
    /// <summary>
    /// RN-56: o ativo deixa a frota. Motivo, data e responsável são colunas do próprio veículo, e
    /// não tabela própria, porque desmobilização é <b>terminal</b> e acontece uma vez só — não tem
    /// duas pontas para amarrar como o bloqueio e a transferência têm.
    ///
    /// A venda em si (canal, comprador, valor, baixa contábil) é outro processo e não está nesta
    /// especificação; o que a RN-56 fecha é o ativo parar de ser frota.
    ///
    /// Nada a migrar de dado: as três colunas nascem nulas, que é o valor certo para todo veículo
    /// que ainda é frota.
    /// </summary>
    public partial class Desmobilizacao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "data_desmobilizacao",
                table: "tb_veiculo",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "id_funcionario_desmobilizacao",
                table: "tb_veiculo",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "motivo_desmobilizacao",
                table: "tb_veiculo",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_tb_veiculo_id_funcionario_desmobilizacao",
                table: "tb_veiculo",
                column: "id_funcionario_desmobilizacao");

            migrationBuilder.AddForeignKey(
                name: "fk_tb_veiculo_tb_funcionario_id_funcionario_desmobilizacao",
                table: "tb_veiculo",
                column: "id_funcionario_desmobilizacao",
                principalTable: "tb_funcionario",
                principalColumn: "id_funcionario",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_tb_veiculo_tb_funcionario_id_funcionario_desmobilizacao",
                table: "tb_veiculo");

            migrationBuilder.DropIndex(
                name: "ix_tb_veiculo_id_funcionario_desmobilizacao",
                table: "tb_veiculo");

            migrationBuilder.DropColumn(
                name: "data_desmobilizacao",
                table: "tb_veiculo");

            migrationBuilder.DropColumn(
                name: "id_funcionario_desmobilizacao",
                table: "tb_veiculo");

            migrationBuilder.DropColumn(
                name: "motivo_desmobilizacao",
                table: "tb_veiculo");
        }
    }
}
