namespace Locadora_Auto.Domain.Entidades
{
    public class LocacaoSeguro
    {
        public int IdLocacao{ get; private set; }
        public int IdSeguro { get; private set; }
        public int IdLocacaoSeguro { get; set; }
        public bool Ativo { get; set; }

        /// <summary>
        /// RN-18: o que a proteção custa por diária <b>neste</b> contrato. Cópia do
        /// <c>Seguro.ValorDiaria</c> no instante da contratação, e não leitura do cadastro no
        /// fechamento — o mesmo motivo da RN-06: reajustar a tabela de seguros não pode reescrever
        /// contrato já vendido.
        /// </summary>
        public decimal ValorDiariaContratada { get; private set; }

        /// <summary>
        /// RN-25: teto da cobrança de avaria ao cliente quando há proteção, <b>somando todas as
        /// avarias</b> do contrato — não por avaria.
        ///
        /// Congelada pela mesma razão da diária, e com um agravante: franquia é o número que o
        /// cliente leu e assinou. Apurar avaria pela franquia vigente hoje, e não pela do dia da
        /// retirada, é cobrança que não sobrevive a uma contestação.
        /// </summary>
        public decimal FranquiaContratada { get; private set; }

        /// <summary>
        /// Desde quando a proteção cobre. Doc 07 §4: contratar depois do início é caso normal — o
        /// cliente que vê o trânsito da cidade e liga pedindo proteção no segundo dia — e aí ela é
        /// cobrada <b>pró-rata a partir daqui</b>, não desde a retirada.
        /// </summary>
        public DateTime DataContratacao { get; private set; }

        /// <summary>
        /// RN-19: até quando cobriu. Nula enquanto a proteção está ativa.
        ///
        /// Sem esta coluna a RN-19 é inexequível: <c>Ativo = false</c> diz que foi cancelada, mas
        /// não quando — e sem o quando não há pró-rata, só a escolha entre cobrar o contrato
        /// inteiro (o cliente reclama com razão) ou não cobrar nada (a casa perde o que cobriu).
        /// </summary>
        public DateTime? DataCancelamento { get; private set; }


        protected LocacaoSeguro() { } // EF

        internal static LocacaoSeguro Contratar(
            int idSeguro, decimal valorDiaria, decimal franquia, DateTime dataContratacao)
        {
            if (valorDiaria <= 0)
                throw new DomainException("Valor da diária do seguro inválido");

            // zero é proteção sem franquia, que existe e se vende; negativo é erro
            if (franquia < 0)
                throw new DomainException("Franquia do seguro não pode ser negativa");

            return new LocacaoSeguro
            {
                IdSeguro = idSeguro,
                ValorDiariaContratada = valorDiaria,
                FranquiaContratada = franquia,
                DataContratacao = dataContratacao,
                Ativo = true
            };
        }

        /// <summary>
        /// Não recebe data: a cobertura acaba <b>agora</b>, nunca retroativa — mesma decisão da
        /// liberação de bloqueio da RN-52. Datar o cancelamento para trás devolveria ao cliente
        /// dias em que ele esteve coberto, e é justamente o que a pró-rata existe para medir.
        /// </summary>
        internal void Cancelar()
        {
            if (Ativo != true)
                throw new DomainException("Seguro não pode ser cancelado");

            Ativo = false;
            DataCancelamento = DateTime.UtcNow;
        }
    }    

}

