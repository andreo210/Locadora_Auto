using Locadora_Auto.Domain.Auditoria;

namespace Locadora_Auto.Domain.Entidades
{
    /// <summary>
    /// RN-37: uma linha por transição do ativo. Sem isto o status do veículo muda e ninguém sabe
    /// por qual documento, por quem nem quando — o que inviabiliza tanto a conciliação de frota
    /// quanto os indicadores de utilização e de tempo de preparação.
    ///
    /// Só a própria <see cref="Veiculo"/> cria movimento (<c>Criar</c> é internal): registrar
    /// transição pela borda seria exatamente o "status trocado à mão" que a RN-37 proíbe.
    /// </summary>
    public class MovimentoVeiculo : IAuditoria
    {
        public int IdMovimentoVeiculo { get; private set; }
        public int IdVeiculo { get; private set; }

        /// <summary>
        /// Situação da qual o ativo saiu. Nulo só no cadastro, porque antes de existir o veículo
        /// não estava em situação nenhuma.
        /// </summary>
        public StatusVeiculo? StatusOrigem { get; private set; }
        public StatusVeiculo StatusDestino { get; private set; }

        public TipoDocumentoOrigem TipoOrigem { get; private set; }
        public int? IdLocacaoOrigem { get; private set; }
        public int? IdManutencaoOrigem { get; private set; }

        /// <summary>
        /// Quando a transição aconteceu. Vem do domínio, e não do <c>DataCriacao</c> da auditoria,
        /// porque aquele só é preenchido pelo <c>SaveChangesAsync</c> — o carimbo precisa existir
        /// mesmo antes de gravar, e é dele que a liberação automática da preparação (RN-45) vai
        /// medir os <c>TempoPreparacaoMinutos</c>.
        /// </summary>
        public DateTime DataMovimento { get; private set; }

        /// <summary>
        /// Documento que originou a transição. São navegações, e não apenas ids, porque na
        /// abertura do contrato e na abertura da ordem de serviço o documento ainda não tem id —
        /// ele nasce no mesmo <c>SaveChanges</c>. É o EF que preenche a chave estrangeira a partir
        /// daqui; gravar o id "na mão" registraria zero.
        /// </summary>
        public Locacao? LocacaoOrigem { get; private set; }
        public Manutencao? ManutencaoOrigem { get; private set; }

        //auditoria: é daqui que sai o autor da transição, sem plumbing novo — o SaveChangesAsync
        //já preenche IdUsuarioCriacao com o usuário corrente (ou "SYSTEM", enquanto a autenticação
        //estiver comentada no Program.cs). Movimento não se altera, então o par de modificação
        //existe só porque o contrato IAuditoria pede, e permanece nulo.
        public DateTime DataCriacao { get; set; }
        public string? IdUsuarioCriacao { get; set; }
        public DateTime? DataModificacao { get; set; }
        public string? IdUsuarioModificacao { get; set; }

        private MovimentoVeiculo() { } // EF

        internal static MovimentoVeiculo Criar(
            StatusVeiculo? statusOrigem,
            StatusVeiculo statusDestino,
            TipoDocumentoOrigem tipoOrigem,
            Locacao? contrato = null,
            Manutencao? ordemServico = null)
        {
            // RN-37: transição sem documento de origem é proibida. Contrato e ordem de serviço são
            // os tipos que têm documento; pátio e cadastro são o próprio ato, e o registro deles é
            // esta linha somada ao autor.
            if (tipoOrigem == TipoDocumentoOrigem.Contrato && contrato == null)
                throw new DomainException("Movimento com origem em contrato exige o contrato");

            if (tipoOrigem == TipoDocumentoOrigem.OrdemServico && ordemServico == null)
                throw new DomainException("Movimento com origem em ordem de serviço exige a ordem");

            return new MovimentoVeiculo
            {
                StatusOrigem = statusOrigem,
                StatusDestino = statusDestino,
                TipoOrigem = tipoOrigem,
                LocacaoOrigem = contrato,
                ManutencaoOrigem = ordemServico,

                // documento já gravado entra pelo id; documento que está nascendo entra só pela
                // navegação, e o EF resolve a chave na hora do insert
                IdLocacaoOrigem = contrato is { IdLocacao: > 0 } ? contrato.IdLocacao : null,
                IdManutencaoOrigem = ordemServico is { IdManutencao: > 0 } ? ordemServico.IdManutencao : null,

                DataMovimento = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Que documento autorizou a transição (RN-37). Transferência e bloqueio entram quando as
    /// RN-48/RN-49 e RN-52 forem implantadas — hoje não há de onde originar esses movimentos.
    /// </summary>
    public enum TipoDocumentoOrigem
    {
        /// <summary>Cadastro do veículo, ativação e desativação administrativa.</summary>
        Cadastro = 1,

        /// <summary>Locação: abertura, devolução e cancelamento.</summary>
        Contrato = 2,

        /// <summary>Ordem de serviço da oficina.</summary>
        OrdemServico = 3,

        /// <summary>Liberação da preparação declarada pelo pátio.</summary>
        Patio = 4
    }
}
