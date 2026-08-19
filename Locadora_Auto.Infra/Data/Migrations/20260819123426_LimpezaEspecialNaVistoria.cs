using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Locadora_Auto.Infra.Data.Migrations
{
    /// <inheritdoc />
    public partial class LimpezaEspecialNaVistoria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "requer_limpeza_especial",
                table: "tb_vistoria",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "requer_limpeza_especial",
                table: "tb_vistoria");
        }
    }
}
