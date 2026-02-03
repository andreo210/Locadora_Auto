namespace Locadora_Auto.Domain.Entidades
{
    public class LocacaoSeguro
    {
        public int IdLocacao{ get; private set; }
        public int IdSeguro { get; private set; }
        public int IdLocacaoSeguro { get; set; }
        public bool Ativo { get; set; }


        protected LocacaoSeguro() { } // EF

        internal static LocacaoSeguro Contratar(int idSeguro)
        {

            return new LocacaoSeguro
            {
                IdSeguro = idSeguro,
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

