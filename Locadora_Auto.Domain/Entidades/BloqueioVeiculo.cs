namespace Locadora_Auto.Domain.Entidades
{
    /// <summary>
    /// RN-52: tirar um veículo da oferta por decisão da casa, com <b>motivo, prazo e
    /// responsável</b>. Sem os três, bloqueio é carro que some da frota e ninguém percebe — o
    /// gerente não sabe que ele existe, o balcão não sabe por que não pode vendê-lo, e ninguém
    /// responde por devolvê-lo à oferta.
    ///
    /// É documento de origem da transição (RN-37), no mesmo papel que o contrato tem na locação e
    /// a ordem de serviço tem na oficina. Por isso <c>Criar</c> é <c>internal</c>: quem bloqueia é
    /// <see cref="Veiculo.Bloquear"/>, e abrir bloqueio pela borda seria exatamente o "status
    /// trocado à mão" que a RN-37 proíbe.
    ///
    /// <b>Não é o mesmo que desativar o veículo.</b> A desativação cadastral (<c>Ativo = false</c>)
    /// também tira o carro da oferta, mas não é temporária e não tem prazo: a saída dela é
    /// <see cref="Veiculo.Ativar"/>, ela aparece em qualquer filtro por <c>Ativo</c> e sua origem
    /// na trilha é <see cref="TipoDocumentoOrigem.Cadastro"/>. O bloqueio é a suspensão com data
    /// para acabar, e é só ele que entra no indicador de bloqueios vencidos.
    /// </summary>
    public class BloqueioVeiculo
    {
        public int IdBloqueioVeiculo { get; private set; }
        public int IdVeiculo { get; private set; }

        public MotivoBloqueio Motivo { get; private set; }

        /// <summary>Texto livre do responsável. O motivo tipado é o que se conta; isto é o que se lê.</summary>
        public string? Observacao { get; private set; }

        public DateTime DataBloqueio { get; private set; }

        /// <summary>
        /// O coração da RN-52. Obrigatória e sempre no futuro do bloqueio: é ela que transforma
        /// "carro sumiu da oferta" em "carro sai da oferta até tal dia", e é contra ela que o
        /// indicador de bloqueios vencidos (seção 12) mede.
        /// </summary>
        public DateTime DataPrevistaLiberacao { get; private set; }

        /// <summary>Quando o bloqueio de fato acabou. Nulo enquanto ele está em aberto.</summary>
        public DateTime? DataLiberacao { get; private set; }

        /// <summary>
        /// Situação em que o veículo estava quando o bloqueio começou, para a liberação devolvê-lo
        /// a ela. Bloqueio <b>suspende</b> a situação do ativo, não a apaga: carro bloqueado no
        /// pátio volta para o pátio (ainda não foi limpo), e carro bloqueado por não devolução
        /// volta para <c>Locado</c> (o contrato continua aberto e ele continua na rua). Sem isto,
        /// liberar jogaria os dois direto na oferta — um carro sujo e um carro que nem está na
        /// filial.
        /// </summary>
        public StatusVeiculo StatusAnterior { get; private set; }

        /// <summary>
        /// Quem responde por este carro voltar à oferta. É funcionário, e não o autor da auditoria,
        /// porque as perguntas são diferentes: a auditoria responde "quem digitou", e a RN-52 quer
        /// "a quem cobrar". Também é o único que funciona hoje — enquanto a autenticação estiver
        /// comentada no <c>Program.cs</c>, o autor de tudo é "SYSTEM".
        /// </summary>
        public int IdFuncionarioResponsavel { get; private set; }
        public Funcionario Responsavel { get; private set; } = null!;

        private BloqueioVeiculo() { } // EF

        /// <summary>Bloqueio ainda não encerrado.</summary>
        public bool EmAberto => DataLiberacao == null;

        /// <summary>
        /// Passou do prazo e ninguém liberou. É a definição do indicador "bloqueios vencidos" da
        /// seção 12 — o número que existe para nenhum carro sumir da oferta por esquecimento.
        /// </summary>
        public bool Vencido(DateTime agora) => EmAberto && agora > DataPrevistaLiberacao;

        internal static BloqueioVeiculo Criar(
            int idVeiculo,
            MotivoBloqueio motivo,
            DateTime dataPrevistaLiberacao,
            int idFuncionarioResponsavel,
            StatusVeiculo statusAnterior,
            string? observacao = null)
        {
            if (!Enum.IsDefined(motivo))
                throw new DomainException("Motivo de bloqueio inválido");

            // comparação explícita, e não int.IsPositive: aquele devolve true para zero (o
            // contrato de INumberBase trata 0 como positivo), e id zero é justamente o caso de
            // "não informou responsável" que a RN-52 precisa recusar
            if (idFuncionarioResponsavel <= 0)
                throw new DomainException("Bloqueio exige um funcionário responsável");

            var agora = DateTime.UtcNow;

            // sem prazo à frente o bloqueio nasce vencido, e o indicador da seção 12 perderia o
            // sentido: ele mede quem passou do prazo, não quem nunca teve um
            if (dataPrevistaLiberacao <= agora)
                throw new DomainException("A data prevista de liberação tem que ser futura");

            return new BloqueioVeiculo
            {
                IdVeiculo = idVeiculo,
                Motivo = motivo,
                Observacao = string.IsNullOrWhiteSpace(observacao) ? null : observacao.Trim(),
                DataBloqueio = agora,
                DataPrevistaLiberacao = dataPrevistaLiberacao,
                StatusAnterior = statusAnterior,
                IdFuncionarioResponsavel = idFuncionarioResponsavel
            };
        }

        /// <summary>
        /// Encerra o bloqueio. Não recebe data: o carro volta à oferta <b>agora</b>, nunca
        /// retroativo — mesma decisão da liberação por prazo da RN-45. Datar a liberação no
        /// vencimento esconderia justamente os dias em que o carro ficou parado, que é o que o
        /// indicador existe para mostrar.
        /// </summary>
        internal void Encerrar()
        {
            if (!EmAberto)
                throw new DomainException("Bloqueio já foi liberado");

            DataLiberacao = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Por que o carro saiu da oferta. É tipado, e não texto livre, porque a pergunta que o gestor
    /// faz é "quanto da minha frota está parada por quê" — e sobre texto livre essa conta não sai.
    /// A observação continua existindo para o caso concreto.
    /// </summary>
    public enum MotivoBloqueio
    {
        /// <summary>Pendência de documento: licenciamento vencido, gravame, transferência de propriedade.</summary>
        Documental = 1,

        /// <summary>
        /// Oferta segurada por decisão comercial (guardar frota para o pico). Existe porque
        /// acontece; separado dos demais justamente para poder ser medido e questionado.
        /// </summary>
        Comercial = 2,

        /// <summary>Frota dedicada a um cliente corporativo ou a um evento com data marcada.</summary>
        Evento = 3,

        /// <summary>
        /// Sinistro em apuração. Não volta à oferta sem ordem de serviço encerrada (doc 08 §5) —
        /// o bloqueio segura o carro enquanto o processo com a seguradora corre.
        /// </summary>
        Sinistro = 4,

        /// <summary>
        /// Doc 08 §5: passou do limiar e o carro não voltou. Tira o contrato de <c>Locado</c>
        /// indefinidamente, que contaminaria a utilização da seção 12 — carro sumido não é carro
        /// trabalhando.
        /// </summary>
        NaoDevolvido = 5,

        /// <summary>
        /// Primeira etapa da venda: o carro sai da agenda para laudo, reparo estético e
        /// documentação. Ainda é frota — quem encerra o ciclo é a desmobilização (RN-56), que é
        /// estado terminal e não bloqueio.
        /// </summary>
        Desmobilizacao = 6,

        /// <summary>Fora dos anteriores. A observação passa a ser obrigatória na prática do balcão.</summary>
        Outro = 7
    }
}
