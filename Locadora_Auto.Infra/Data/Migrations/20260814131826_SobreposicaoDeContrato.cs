
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Locadora_Auto.Infra.Data.Migrations
{
    /// <summary>
    /// RN-40/RN-41: um veículo tem no máximo um contrato não encerrado por período.
    ///
    /// Escrita à mão porque o EF não gera <c>EXCLUDE</c> — daí a migration não ter modelo nenhum
    /// por trás e o snapshot ficar intocado, como na <c>ConcorrenciaOtimista</c>.
    ///
    /// A checagem equivalente em <c>LocacaoService</c> é mensagem amigável, não garantia: duas
    /// requisições simultâneas passam juntas por qualquer <c>if</c> antes de uma delas gravar.
    /// Quem serializa é esta constraint. A violação chega como <c>PostgresException</c> com
    /// SQLSTATE <c>23P01</c> e o <c>ExceptionProblemFactory</c> a traduz para 409.
    /// </summary>
    public partial class SobreposicaoDeContrato : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            /*
             * btree_gist é o que permite misturar a igualdade de id_veiculo (btree) com a
             * sobreposição do intervalo (gist) no mesmo índice. É extensão "trusted" desde o
             * PostgreSQL 13, então o dono do banco cria sem ser superusuário.
             */
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS btree_gist;");

            /*
             * tstzrange(inicio, fim) é meio-aberto por padrão — [inicio, fim) — que é exatamente a
             * semântica de Locacao.Sobrepostas: contrato que começa no instante em que o outro
             * termina não conflita, porque devolver e retirar no mesmo horário é operação normal
             * de balcão.
             *
             * O predicado precisa dos status terminais na forma REAL da coluna: tb_locacao.status
             * é character varying(20) (LocacaoConfig aplica HasConversion<string>()), então vai
             * entre aspas. Escrever aqui os inteiros do enum não daria erro nenhum — apenas
             * nenhuma linha casaria com o predicado e a constraint ficaria desligada em silêncio.
             * A lista viva está em Locacao.StatusTerminais; hoje é só Finalizada, porque
             * Cancelar() também grava Finalizada. Ao acrescentar status terminal lá, mexa aqui.
             *
             * Se a tabela já tiver contratos sobrepostos, este ALTER falha — a constraint valida o
             * que já está gravado. Para achar os culpados antes de migrar:
             *
             *   SELECT a.id_locacao, b.id_locacao, a.id_veiculo
             *     FROM tb_locacao a
             *     JOIN tb_locacao b
             *       ON a.id_veiculo = b.id_veiculo
             *      AND a.id_locacao < b.id_locacao
             *      AND a.status NOT IN ('Finalizada')
             *      AND b.status NOT IN ('Finalizada')
             *      AND tstzrange(a.data_inicio, COALESCE(a.data_fim_real, a.data_fim_prevista))
             *       && tstzrange(b.data_inicio, COALESCE(b.data_fim_real, b.data_fim_prevista));
             */
            migrationBuilder.Sql(@"
                ALTER TABLE tb_locacao
                  ADD CONSTRAINT ex_locacao_sem_sobreposicao
                  EXCLUDE USING gist (
                    id_veiculo WITH =,
                    tstzrange(data_inicio, COALESCE(data_fim_real, data_fim_prevista)) WITH &&
                  ) WHERE (status NOT IN ('Finalizada'));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE tb_locacao DROP CONSTRAINT IF EXISTS ex_locacao_sem_sobreposicao;");

            // a extensão fica: outros objetos podem ter passado a depender dela, e derrubá-la aqui
            // seria destruir mais do que esta migration criou
        }
    }
}
