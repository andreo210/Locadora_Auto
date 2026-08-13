using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Locadora_Auto.Infra.Data.Migrations
{
    /// <summary>
    /// Marca o <c>xmin</c> como token de concorrência otimista nas entidades de domínio.
    ///
    /// Migration deliberadamente vazia: <c>xmin</c> é coluna de SISTEMA, que o Postgres já mantém
    /// em toda tabela. O gerador do EF não sabe disso e produziu um <c>AddColumn</c> por tabela —
    /// que falharia com <i>"column name xmin conflicts with a system column name"</i>. O banco não
    /// precisa de alteração nenhuma; o que muda é só o modelo, e é isso que o snapshot registra
    /// para as próximas migrations não tentarem gerar a mesma coisa de novo.
    /// </summary>
    public partial class ConcorrenciaOtimista : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // sem alteração de schema, por definição
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // sem alteração de schema, por definição
        }
    }
}
