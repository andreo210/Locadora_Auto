using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Locadora_Auto.Infra.Data.Migrations
{
    /// <summary>
    /// RN-59/RN-61: <c>Cancelada</c> passa a existir como estado próprio, e com isso vira o segundo
    /// status terminal.
    ///
    /// Vazia de modelo de propósito, como a <c>ConcorrenciaOtimista</c>: a coluna <c>status</c> é
    /// <c>varchar</c> (<c>HasConversion&lt;string&gt;</c>), então acrescentar membro ao enum não
    /// muda schema nenhum. O que muda é o <b>predicado</b> da constraint EXCLUDE, que o EF não
    /// gera e por isso é reescrito à mão aqui.
    ///
    /// A constraint é derrubada e recriada em vez de alterada porque o PostgreSQL não tem
    /// <c>ALTER ... EXCLUDE</c>: o predicado faz parte da definição do índice.
    /// </summary>
    public partial class EstadosDeLocacao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE tb_locacao DROP CONSTRAINT IF EXISTS ex_locacao_sem_sobreposicao;");

            /*
             * A lista viva está em Locacao.StatusTerminais, e são só dois, por motivos diferentes:
             *
             *   Cancelada  — contrato anulado antes da retirada. O carro não rodou, então o período
             *                tem que voltar à oferta retroativamente. Sem isto, cancelar um
             *                contrato deixaria o período bloqueado por algo que não aconteceu.
             *   Finalizada — ciclo inteiro encerrado.
             *
             * Devolvida, Fechada e ComSaldoResidual ficam DE FORA de propósito: o carro rodou
             * naquele período. Como data_fim_real já está gravada quando o contrato chega nesses
             * estados, o tstzrange encolheu para o período real e o meio-aberto deixa um contrato
             * novo começar no mesmo instante da devolução — não trava a oferta, e ainda protege o
             * histórico contra lançamento retroativo sobreposto.
             *
             * Os valores vão entre aspas porque a coluna é character varying(20). Escrever aqui os
             * inteiros do enum não daria erro nenhum — apenas nenhuma linha casaria com o
             * predicado e a constraint ficaria desligada em silêncio.
             * SobreposicaoDeContratoTests compara esta lista com a do domínio e é quem faz o
             * esquecimento doer; ele lê sempre a MIGRATION MAIS RECENTE que define a constraint,
             * então mexer no predicado é sempre uma migration nova, nunca editar a anterior.
             *
             * Contratos gravados como 'Finalizada' por Cancelar() antes desta mudança continuam
             * 'Finalizada' e são indistinguíveis de uma conclusão normal — não há dado que permita
             * separá-los, e ambos são terminais de qualquer forma, então o comportamento da
             * constraint não muda para eles.
             */
            migrationBuilder.Sql(@"
                ALTER TABLE tb_locacao
                  ADD CONSTRAINT ex_locacao_sem_sobreposicao
                  EXCLUDE USING gist (
                    id_veiculo WITH =,
                    tstzrange(data_inicio, COALESCE(data_fim_real, data_fim_prevista)) WITH &&
                  ) WHERE (status NOT IN ('Finalizada', 'Cancelada'));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE tb_locacao DROP CONSTRAINT IF EXISTS ex_locacao_sem_sobreposicao;");

            // volta ao predicado da SobreposicaoDeContrato. Pode falhar se, com Cancelada já em
            // uso, houver contrato cancelado sobreposto a outro — que é exatamente o caso que esta
            // migration veio permitir
            migrationBuilder.Sql(@"
                ALTER TABLE tb_locacao
                  ADD CONSTRAINT ex_locacao_sem_sobreposicao
                  EXCLUDE USING gist (
                    id_veiculo WITH =,
                    tstzrange(data_inicio, COALESCE(data_fim_real, data_fim_prevista)) WITH &&
                  ) WHERE (status NOT IN ('Finalizada'));");
        }
    }
}
