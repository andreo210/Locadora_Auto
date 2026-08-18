using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Locadora_Auto.Infra.Data.Migrations
{
    /// <summary>
    /// RN-55: placa e chassi passam a ser únicos <b>entre os veículos ativos</b>, e não na tabela
    /// inteira. O índice global de antes impedia recadastrar a placa de um carro que já saiu da
    /// frota — e placa é reemitida pelo Detran, chassi de veículo baixado sai do cadastro. O que a
    /// regra protege é a conciliação de multa e de sinistro, e essa só olha carro ativo.
    ///
    /// O predicado (<c>filter: "ativo"</c>) vira um índice parcial do Postgres. Em base com dados,
    /// o <c>CREATE UNIQUE INDEX</c> falha se já houver dois <b>ativos</b> com a mesma placa ou o
    /// mesmo chassi; duplicata envolvendo inativo passa a ser aceita, então esta migration afrouxa
    /// a regra e não a aperta. Para encontrar conflito antes de aplicar:
    ///
    /// <code>
    /// SELECT placa, count(*) FROM tb_veiculo WHERE ativo GROUP BY placa HAVING count(*) > 1;
    /// SELECT chassi, count(*) FROM tb_veiculo WHERE ativo GROUP BY chassi HAVING count(*) > 1;
    /// </code>
    /// </summary>
    public partial class UnicidadeEntreVeiculosAtivos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_tb_veiculo_chassi",
                table: "tb_veiculo");

            migrationBuilder.DropIndex(
                name: "ix_tb_veiculo_placa",
                table: "tb_veiculo");

            migrationBuilder.CreateIndex(
                name: "ix_tb_veiculo_chassi",
                table: "tb_veiculo",
                column: "chassi",
                unique: true,
                filter: "ativo");

            migrationBuilder.CreateIndex(
                name: "ix_tb_veiculo_placa",
                table: "tb_veiculo",
                column: "placa",
                unique: true,
                filter: "ativo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_tb_veiculo_chassi",
                table: "tb_veiculo");

            migrationBuilder.DropIndex(
                name: "ix_tb_veiculo_placa",
                table: "tb_veiculo");

            migrationBuilder.CreateIndex(
                name: "ix_tb_veiculo_chassi",
                table: "tb_veiculo",
                column: "chassi",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tb_veiculo_placa",
                table: "tb_veiculo",
                column: "placa",
                unique: true);
        }
    }
}
