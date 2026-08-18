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
        public int? IdBloqueioOrigem { get; private set; }
        public int? IdTransferenciaOrigem { get; private set; }

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
        public BloqueioVeiculo? BloqueioOrigem { get; private set; }
        public TransferenciaVeiculo? TransferenciaOrigem { get; private set; }

        //auditoria: é daqui que sai o autor da transição, sem plumbing novo — o SaveChangesAsync
        //já preenche IdUsuarioCriacao com o usuário corrente (ou "SYSTEM", enquanto a autenticação
        //estiver comentada no Program.cs). Movimento não se altera, então o par de modificação
        //existe só porque o contrato IAuditoria pede, e permanece nulo.
        public DateTime DataCriacao { get; set; }
        public string? IdUsuarioCriacao { get; set; }
        public DateTime? DataModificacao { get; set; }
        public string? IdUsuarioModificacao { get; set; }

        private MovimentoVeiculo() { } // EF

        /// <param name="idVeiculo">
        /// Dono do movimento. Segue a mesma regra dos documentos de origem: veículo já gravado
        /// entra pelo id; no cadastro ele ainda é 0 e quem resolve a chave é a navegação, porque
        /// veículo e movimento nascem no mesmo <c>SaveChanges</c>. Preenchê-lo aqui é o que
        /// permite consultar a trilha por veículo sem reler o agregado inteiro.
        /// </param>
        internal static MovimentoVeiculo Criar(
            int idVeiculo,
            StatusVeiculo? statusOrigem,
            StatusVeiculo statusDestino,
            TipoDocumentoOrigem tipoOrigem,
            Locacao? contrato = null,
            Manutencao? ordemServico = null,
            BloqueioVeiculo? bloqueio = null,
            TransferenciaVeiculo? transferencia = null)
        {
            // RN-37: transição sem documento de origem é proibida. Contrato e ordem de serviço são
            // os tipos que têm documento; pátio e cadastro são o próprio ato, e o registro deles é
            // esta linha somada ao autor.
            if (tipoOrigem == TipoDocumentoOrigem.Contrato && contrato == null)
                throw new DomainException("Movimento com origem em contrato exige o contrato");

            if (tipoOrigem == TipoDocumentoOrigem.OrdemServico && ordemServico == null)
                throw new DomainException("Movimento com origem em ordem de serviço exige a ordem");

            if (tipoOrigem == TipoDocumentoOrigem.Bloqueio && bloqueio == null)
                throw new DomainException("Movimento com origem em bloqueio exige o bloqueio");

            if (tipoOrigem == TipoDocumentoOrigem.Transferencia && transferencia == null)
                throw new DomainException("Movimento com origem em transferência exige a transferência");

            return new MovimentoVeiculo
            {
                IdVeiculo = idVeiculo,
                StatusOrigem = statusOrigem,
                StatusDestino = statusDestino,
                TipoOrigem = tipoOrigem,
                LocacaoOrigem = contrato,
                ManutencaoOrigem = ordemServico,
                BloqueioOrigem = bloqueio,
                TransferenciaOrigem = transferencia,

                // documento já gravado entra pelo id; documento que está nascendo entra só pela
                // navegação, e o EF resolve a chave na hora do insert
                IdLocacaoOrigem = contrato is { IdLocacao: > 0 } ? contrato.IdLocacao : null,
                IdManutencaoOrigem = ordemServico is { IdManutencao: > 0 } ? ordemServico.IdManutencao : null,
                IdBloqueioOrigem = bloqueio is { IdBloqueioVeiculo: > 0 } ? bloqueio.IdBloqueioVeiculo : null,
                IdTransferenciaOrigem = transferencia is { IdTransferenciaVeiculo: > 0 } ? transferencia.IdTransferenciaVeiculo : null,

                DataMovimento = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Que documento autorizou a transição (RN-37).
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
        Patio = 4,

        /// <summary>
        /// Vencimento do <c>TempoPreparacaoMinutos</c> da filial (RN-45, parte automática).
        ///
        /// Separado de <see cref="Patio"/> de propósito, e a distinção é de negócio, não de
        /// implementação: a liberação por prazo devolve à oferta um carro que <b>ninguém
        /// conferiu</b>. Fundidas num tipo só, o tempo médio de preparação da seção 12 viraria
        /// ficção — um pátio que nunca declara nada exibiria exatamente o
        /// <c>TempoPreparacaoMinutos</c> da casa, a melhor média da rede, sem ter limpado carro
        /// nenhum. Assim a liberação sem conferência fica contável por filial e a métrica continua
        /// medindo o pátio.
        /// </summary>
        Prazo = 5,

        /// <summary>
        /// Bloqueio da RN-52: o carro sai da oferta por decisão da casa, com motivo, prazo e
        /// responsável. O documento é o <see cref="BloqueioVeiculo"/>, e ele aparece nas duas
        /// pontas — no movimento que tira o carro da oferta e no que o devolve —, que é o que
        /// permite medir quanto tempo cada motivo segurou a frota.
        ///
        /// Separado de <see cref="Cadastro"/> porque desativar um veículo também o leva a
        /// <c>Bloqueado</c>: sem os dois tipos, o indicador de bloqueios vencidos contaria carro
        /// que saiu do cadastro como carro esquecido na oficina.
        /// </summary>
        Bloqueio = 6,

        /// <summary>
        /// Transferência programada entre filiais (RN-48/RN-49). O documento é a
        /// <see cref="TransferenciaVeiculo"/>, e ela também aparece nas duas pontas: no movimento
        /// que tira o carro da oferta da origem e no que o coloca na do destino. É esse par que
        /// mede o tempo de estrada — o custo que o remanejamento de frota tem e que ninguém
        /// enxerga enquanto ele for só uma troca de filial no cadastro.
        /// </summary>
        Transferencia = 7,

        /// <summary>
        /// RN-56: o ativo deixou a frota. Não tem documento de entidade própria — o registro é
        /// esta linha somada ao motivo, à data e ao responsável gravados no próprio veículo, pela
        /// mesma razão que <see cref="Cadastro"/> e <see cref="Patio"/> não têm: desmobilização é
        /// terminal e acontece uma vez só, então não há duas pontas para amarrar.
        /// </summary>
        Desmobilizacao = 8
    }
}
