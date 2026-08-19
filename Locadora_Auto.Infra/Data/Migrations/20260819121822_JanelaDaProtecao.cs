using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Locadora_Auto.Infra.Data.Migrations
{
    /// <inheritdoc />
    public partial class JanelaDaProtecao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "data_cancelamento",
                table: "tb_locacao_seguro",
                type: "timestamp with time zone",
                nullable: true);

            // ------------------------------------------------------------------
            // `data_contratacao` é obrigatória, mas as linhas que já existem não têm o dado. O EF
            // gerava aqui um DEFAULT de 0001-01-01, que ficaria na definição da coluna para sempre
            // e é data que não significa nada num campo `timestamptz`.
            //
            // O caminho é o honesto: nasce anulável, é preenchida com a data de início do contrato
            // — que é o que essas proteções de fato cobriram, já que nenhuma tem cancelamento
            // registrado — e só então vira NOT NULL.
            // ------------------------------------------------------------------

            migrationBuilder.AddColumn<DateTime>(
                name: "data_contratacao",
                table: "tb_locacao_seguro",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE tb_locacao_seguro ls
                   SET data_contratacao = l.data_inicio
                  FROM tb_locacao l
                 WHERE l.id_locacao = ls.id_locacao;");

            // a FK impede órfão, mas o ALTER abaixo não perdoa um único nulo — e uma migration que
            // falha no meio deixa o banco num estado que ninguém quer depurar em produção
            migrationBuilder.Sql(@"
                UPDATE tb_locacao_seguro
                   SET data_contratacao = now()
                 WHERE data_contratacao IS NULL;");

            migrationBuilder.AlterColumn<DateTime>(
                name: "data_contratacao",
                table: "tb_locacao_seguro",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "data_cancelamento",
                table: "tb_locacao_seguro");

            migrationBuilder.DropColumn(
                name: "data_contratacao",
                table: "tb_locacao_seguro");
        }
    }
}
