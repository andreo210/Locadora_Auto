using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Locadora_Auto.Infra.Data.Migrations
{
    /// <summary>
    /// RN-45/RN-46: minutos entre a devolução e o carro voltar à oferta da filial.
    ///
    /// O <c>defaultValue</c> não é enfeite — é o que dá um valor sensato às filiais que já existem.
    /// Sem ele a coluna nasceria zerada e toda filial passaria a declarar preparação instantânea,
    /// devolvendo o carro à oferta no mesmo minuto da entrega.
    /// </summary>
    public partial class TempoPreparacaoDaFilial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "tempo_preparacao_minutos",
                table: "tb_filial",
                type: "integer",
                nullable: false,
                defaultValue: 120);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "tempo_preparacao_minutos",
                table: "tb_filial");
        }
    }
}
