namespace Locadora_Auto.Domain.Entidades
{
    /// <summary>
    /// RN-48/RN-49: movimentação programada de um veículo entre filiais.
    ///
    /// <b>Não é devolução one-way.</b> A RN-48 é explícita: carro devolvido em outra filial fica
    /// <b>disponível no destino</b>, porque a taxa de retorno (RN-21) já pagou o desequilíbrio e
    /// prendê-lo cobraria duas vezes pelo mesmo fato. <c>EmTransferencia</c> é para o outro caso —
    /// a casa decide remanejar frota, e aí existe um trecho de estrada em que o carro não está em
    /// filial nenhuma.
    ///
    /// A regra que dá o desenho é a RN-49: o veículo sai da oferta da origem <b>antes</b> de entrar
    /// na do destino. Por isso a transferência tem duas pontas (envio e chegada) e não um ato só —
    /// contar o mesmo carro nas duas filiais durante o trecho é overbooking involuntário, e é o
    /// erro que aparece justamente no dia de pico, quando as duas filiais venderam.
    ///
    /// É documento de origem da transição (RN-37), como o contrato e a ordem de serviço, e por isso
    /// <c>Criar</c> é <c>internal</c>: quem transfere é <see cref="Veiculo.EnviarParaTransferencia"/>.
    /// </summary>
    public class TransferenciaVeiculo
    {
        public int IdTransferenciaVeiculo { get; private set; }
        public int IdVeiculo { get; private set; }

        /// <summary>
        /// De onde o carro saiu. Fica gravada porque <c>Veiculo.FilialAtualId</c> muda na chegada e
        /// deixaria de responder de onde ele veio — e é essa a pergunta do remanejamento de frota.
        /// </summary>
        public int IdFilialOrigem { get; private set; }
        public int IdFilialDestino { get; private set; }

        public Filial FilialOrigem { get; private set; } = null!;
        public Filial FilialDestino { get; private set; } = null!;

        public DateTime DataEnvio { get; private set; }

        /// <summary>
        /// Quando o carro deveria chegar. Mesmo papel do prazo do bloqueio (RN-52): sem ela, carro
        /// que sumiu na estrada não aparece em lugar nenhum — ele não está na oferta de nenhuma das
        /// duas filiais e ninguém tem motivo para procurá-lo.
        /// </summary>
        public DateTime DataPrevistaChegada { get; private set; }

        public DateTime? DataChegada { get; private set; }

        public StatusTransferencia Status { get; private set; }

        public string? Observacao { get; private set; }

        /// <summary>Quem responde pelo carro enquanto ele está entre as duas filiais.</summary>
        public int IdFuncionarioResponsavel { get; private set; }
        public Funcionario Responsavel { get; private set; } = null!;

        private TransferenciaVeiculo() { } // EF

        public bool EmTransito => Status == StatusTransferencia.EmTransito;

        /// <summary>Em trânsito além da data prevista — o carro que sumiu na estrada.</summary>
        public bool Atrasada(DateTime agora) => EmTransito && agora > DataPrevistaChegada;

        internal static TransferenciaVeiculo Criar(
            int idVeiculo,
            int idFilialOrigem,
            int idFilialDestino,
            DateTime dataPrevistaChegada,
            int idFuncionarioResponsavel,
            string? observacao = null)
        {
            if (idFilialOrigem == idFilialDestino)
                throw new DomainException("A filial de destino tem que ser diferente da de origem");

            // comparação explícita, e não int.IsPositive: aquele devolve true para zero
            if (idFuncionarioResponsavel <= 0)
                throw new DomainException("Transferência exige um funcionário responsável");

            var agora = DateTime.UtcNow;

            if (dataPrevistaChegada <= agora)
                throw new DomainException("A data prevista de chegada tem que ser futura");

            return new TransferenciaVeiculo
            {
                IdVeiculo = idVeiculo,
                IdFilialOrigem = idFilialOrigem,
                IdFilialDestino = idFilialDestino,
                DataEnvio = agora,
                DataPrevistaChegada = dataPrevistaChegada,
                Status = StatusTransferencia.EmTransito,
                IdFuncionarioResponsavel = idFuncionarioResponsavel,
                Observacao = string.IsNullOrWhiteSpace(observacao) ? null : observacao.Trim()
            };
        }

        internal void ConfirmarChegada()
        {
            if (!EmTransito)
                throw new DomainException("Transferência não está em trânsito");

            DataChegada = DateTime.UtcNow;
            Status = StatusTransferencia.Concluida;
        }

        /// <summary>
        /// O carro nunca saiu, ou voltou sem chegar ao destino. Fica <c>Cancelada</c> em vez de
        /// sumir do banco: a trilha do ativo já registrou a saída da oferta, e apagar a
        /// transferência deixaria aquele movimento sem documento — o que a RN-37 proíbe e o
        /// indicador de transições sem origem (seção 12) contaria.
        /// </summary>
        internal void Cancelar()
        {
            if (!EmTransito)
                throw new DomainException("Só transferência em trânsito pode ser cancelada");

            Status = StatusTransferencia.Cancelada;
        }
    }

    public enum StatusTransferencia
    {
        /// <summary>Saiu da origem e ainda não chegou: não conta na oferta de nenhuma filial.</summary>
        EmTransito = 1,

        /// <summary>Chegou ao destino, que passou a ser a filial atual do veículo.</summary>
        Concluida = 2,

        /// <summary>Abortada antes da chegada; o carro volta à oferta da origem.</summary>
        Cancelada = 3
    }
}
