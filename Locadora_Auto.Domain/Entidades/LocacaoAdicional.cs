namespace Locadora_Auto.Domain.Entidades
{
    public class LocacaoAdicional
    {
        public int IdLocacaoAdicional { get; private set; }
        public int IdAdicional { get; private set; }
        public decimal ValorDiariaContratada { get; private set; }
        public int Quantidade { get; private set; }
        public int Dias { get; private set; }

        protected LocacaoAdicional() { }

        public static LocacaoAdicional Criar(
            int idAdicional,
            decimal valorDiaria,
            int quantidade,
            int dias)
        {
            if (quantidade <= 0)
                throw new DomainException("Quantidade inválida");

            return new LocacaoAdicional
            {
                IdAdicional = idAdicional,
                ValorDiariaContratada = valorDiaria,
                Quantidade = quantidade,
                Dias = dias
            };
        }

        public decimal CalcularTotal()
            => ValorDiariaContratada * Quantidade * Dias;
    }


}
