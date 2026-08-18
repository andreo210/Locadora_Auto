using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Locadora_Auto.Infra.Data.Migrations
{
    /// <inheritdoc />
    public partial class CamposCongeladosEParametrosDeFechamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "capacidade_tanque_litros",
                table: "tb_veiculo",
                type: "numeric(6,2)",
                precision: 6,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "franquia_contratada",
                table: "tb_locacao_seguro",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "valor_diaria_contratada",
                table: "tb_locacao_seguro",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "valor_diaria_contratada",
                table: "tb_locacao",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "habilitada_one_way",
                table: "tb_filial",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<decimal>(
                name: "percentual_hora_excedente",
                table: "tb_filial",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0.3333m);

            migrationBuilder.AddColumn<decimal>(
                name: "preco_litro_combustivel",
                table: "tb_filial",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "taxa_retorno_one_way",
                table: "tb_filial",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "taxa_servico_abastecimento",
                table: "tb_filial",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "tolerancia_minutos",
                table: "tb_filial",
                type: "integer",
                nullable: false,
                defaultValue: 30);

            migrationBuilder.AddColumn<decimal>(
                name: "valor_limpeza_especial",
                table: "tb_filial",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            // ------------------------------------------------------------------
            // Backfill dos valores congelados (RN-06, RN-18, RN-25)
            //
            // As três colunas acima nasceriam zeradas nas linhas que já existem, e zero num campo
            // de preço não é neutro: é um número plausível que a apuração aceitaria e usaria para
            // cobrar nada. Preencher com o cadastro de hoje não inventa histórico — hoje nenhum
            // cálculo lê essas tabelas, então o preço vigente da categoria e do seguro é
            // literalmente o único valor que o sistema já associou a esses contratos.
            //
            // Roda uma vez, sobre o passado. Contrato aberto daqui em diante congela na abertura,
            // que é o ponto do A2.
            // ------------------------------------------------------------------

            migrationBuilder.Sql(@"
                UPDATE tb_locacao l
                   SET valor_diaria_contratada = c.valor_diaria
                  FROM tb_veiculo v
                  JOIN tb_categoria_veiculo c ON c.id_categoria = v.id_categoria
                 WHERE v.id_veiculo = l.id_veiculo;");

            migrationBuilder.Sql(@"
                UPDATE tb_locacao_seguro ls
                   SET valor_diaria_contratada = s.valor_diaria,
                       franquia_contratada     = s.franquia
                  FROM tb_seguro s
                 WHERE s.id_seguro = ls.id_seguro;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "capacidade_tanque_litros",
                table: "tb_veiculo");

            migrationBuilder.DropColumn(
                name: "franquia_contratada",
                table: "tb_locacao_seguro");

            migrationBuilder.DropColumn(
                name: "valor_diaria_contratada",
                table: "tb_locacao_seguro");

            migrationBuilder.DropColumn(
                name: "valor_diaria_contratada",
                table: "tb_locacao");

            migrationBuilder.DropColumn(
                name: "habilitada_one_way",
                table: "tb_filial");

            migrationBuilder.DropColumn(
                name: "percentual_hora_excedente",
                table: "tb_filial");

            migrationBuilder.DropColumn(
                name: "preco_litro_combustivel",
                table: "tb_filial");

            migrationBuilder.DropColumn(
                name: "taxa_retorno_one_way",
                table: "tb_filial");

            migrationBuilder.DropColumn(
                name: "taxa_servico_abastecimento",
                table: "tb_filial");

            migrationBuilder.DropColumn(
                name: "tolerancia_minutos",
                table: "tb_filial");

            migrationBuilder.DropColumn(
                name: "valor_limpeza_especial",
                table: "tb_filial");
        }
    }
}
