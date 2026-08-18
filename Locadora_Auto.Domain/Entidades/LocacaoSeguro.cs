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


        protected LocacaoSeguro() { } // EF

        internal static LocacaoSeguro Contratar(int idSeguro, decimal valorDiaria, decimal franquia)
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
                Ativo = true
            };
        }

        internal void Cancelar()
        {
            if (Ativo != true)
                throw new DomainException("Seguro não pode ser cancelado");

            Ativo = false;
        }
    }    

}

