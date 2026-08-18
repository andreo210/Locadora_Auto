using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Locadora_Auto.Infra.Data.Migrations
{
    /// <summary>
    /// Seção 12: uma linha por tentativa de abrir ou estender contrato sobre veículo já
    /// comprometido no período (RN-40), para o indicador "tentativas de sobreposição recusadas por
    /// filial" existir.
    ///
    /// A recusa em si sempre funcionou — o que faltava era contar. E contar em <b>tabela</b>, e não
    /// em log, porque a leitura do número é comparativa e no tempo: se ele sobe numa filial, o
    /// problema é de processo de balcão (agenda desatualizada, treinamento, frota curta) e não de
    /// sistema. Contagem que mora no arquivo de log ninguém acompanha, e ela some a cada restart.
    ///
    /// <b>Sem chave estrangeira de propósito.</b> A recusa tem de sobreviver ao veículo ser
    /// excluído ou desmobilizado: uma FK <c>Restrict</c> travaria a exclusão por causa de uma
    /// tentativa recusada meses atrás, e uma <c>Cascade</c> apagaria a série histórica, que é
    /// justamente o que se quer acompanhar.
    ///
    /// Nada a migrar de dado — tabela nova, e as recusas anteriores à implantação se perderam
    /// porque nunca foram registradas em lugar nenhum.
    /// </summary>
    public partial class RecusaDeSobreposicao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tb_recusa_sobreposicao",
                columns: table => new
                {
                    id_recusa_sobreposicao = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_veiculo = table.Column<int>(type: "integer", nullable: false),
                    id_filial_retirada = table.Column<int>(type: "integer", nullable: false),
                    inicio_solicitado = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    fim_solicitado = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    data_recusa = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    origem = table.Column<int>(type: "integer", nullable: false),
                    id_locacao_em_extensao = table.Column<int>(type: "integer", nullable: true),
                    data_criacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    id_usuario_criacao = table.Column<string>(type: "text", nullable: true),
                    data_modificacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    id_usuario_modificacao = table.Column<string>(type: "text", nullable: true)

                    // O xmin que o EF gerava aqui foi removido à mão, pelo mesmo motivo que esvazia
                    // a migration ConcorrenciaOtimista: xmin é coluna de sistema do Postgres e
                    // declará-la no CREATE TABLE falha.
                    //
                    // Se esta migration for regerada, apague a linha do xmin de novo.
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tb_recusa_sobreposicao", x => x.id_recusa_sobreposicao);
                });

            migrationBuilder.CreateIndex(
                name: "ix_tb_recusa_sobreposicao_id_filial_retirada_data_recusa",
                table: "tb_recusa_sobreposicao",
                columns: new[] { "id_filial_retirada", "data_recusa" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tb_recusa_sobreposicao");
        }
    }
}
