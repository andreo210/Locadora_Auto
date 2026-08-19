namespace Locadora_Auto.Domain.Entidades
{
    /// <summary>
    /// A garantia que o cliente deixa na abertura do contrato. Doc 07 §3.7, RN-30: <b>caução é
    /// garantia, não receita</b> — ela só é resolvida depois de a conta estar apurada, e o que
    /// sobra volta.
    ///
    /// A máquina desta entidade estava quebrada de três jeitos, todos corrigidos no backlog A10:
    /// <c>Devolver()</c> só aceitava <c>Pendente</c>, então a caução <c>Bloqueada</c> — que é o
    /// fluxo normal — nunca podia ser devolvida; <c>Deduzir</c> descontava do próprio
    /// <see cref="Valor"/> e marcava <c>Bloqueada</c>, apagando quanto o cliente tinha depositado;
    /// e <c>Utilizada</c> não era atribuído em lugar nenhum.
    /// </summary>
    public class Caucao
    {
        public int IdCaucao { get; private set; }

        /// <summary>
        /// Quanto o cliente depositou. <b>Não muda nunca.</b> Descontar daqui apagava a resposta
        /// para "eu deixei quanto?" — que é exatamente a pergunta de quem está esperando o
        /// estorno.
        /// </summary>
        public decimal Valor { get; private set; }

        /// <summary>RN-30: quanto o fechamento consumiu desta caução.</summary>
        public decimal ValorConsumido { get; private set; }

        /// <summary>O que volta para o cliente.</summary>
        public decimal ValorDisponivel => Valor - ValorConsumido;

        public StatusCaucao Status { get; private set; }

        protected Caucao() { } // EF

        internal static Caucao Criar(decimal valor)
        {
            if (valor <= 0)
                throw new DomainException("Valor da caução deve ser maior que zero");

            return new Caucao
            {
                Valor = valor,
                Status = StatusCaucao.Pendente
            };
        }

        internal void Bloquear()
        {
            if (Status != StatusCaucao.Pendente)
                throw new DomainException("Só é possível bloquear caução pendente");

            Status = StatusCaucao.Bloqueada;
        }

        /// <summary>
        /// RN-30: o fechamento consome parte ou tudo. Consumir marca <c>Utilizada</c> — mesmo
        /// parcialmente, porque o que o status responde é "esta garantia foi usada?", e a resposta
        /// para uma caução parcialmente consumida é sim.
        ///
        /// Aceita a partir de <c>Pendente</c> além de <c>Bloqueada</c>: o pré-bloqueio é prática de
        /// cartão, mas caução em dinheiro fica <c>Pendente</c> até o fim e também precisa quitar
        /// conta.
        /// </summary>
        internal void Consumir(decimal valor)
        {
            if (Status is StatusCaucao.Devolvida)
                throw new DomainException("Caução devolvida não pode ser consumida");

            if (valor <= 0)
                throw new DomainException("Valor inválido para consumo da caução");

            if (valor > ValorDisponivel)
                throw new DomainException("Valor excede o disponível da caução");

            ValorConsumido += valor;
            Status = StatusCaucao.Utilizada;
        }

        /// <summary>
        /// Encerra a garantia devolvendo o que não foi usado.
        ///
        /// Só faz sentido enquanto nada foi consumido — caução parcialmente usada permanece
        /// <c>Utilizada</c>, e o estorno do <see cref="ValorDisponivel"/> é fato financeiro, não
        /// mudança de estado. É o que o doc 07 §10 fixa: consumidos R$ 940 de R$ 1.500, devolvidos
        /// R$ 560, e a caução <b>fica em <c>Utilizada</c></b>.
        /// </summary>
        internal void Devolver()
        {
            if (Status == StatusCaucao.Devolvida)
                throw new DomainException("Caução já devolvida");

            if (ValorConsumido > 0)
                throw new DomainException("Caução já consumida pelo fechamento permanece como utilizada");

            Status = StatusCaucao.Devolvida;
        }

        public enum StatusCaucao
        {
            Pendente,
            Bloqueada,
            Utilizada,
            Devolvida
        }
    }

}
