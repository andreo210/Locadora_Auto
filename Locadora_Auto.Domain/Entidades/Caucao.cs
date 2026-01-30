namespace Locadora_Auto.Domain.Entidades
{
    public class Caucao
    {
        public int IdCaucao { get; private set; }
        public decimal Valor { get; private set; }
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

        internal decimal Deduzir( decimal valor)
        {
            return Valor- valor;
        }

        internal void Bloquear()
        {
            if (Status != StatusCaucao.Pendente)
                throw new InvalidOperationException("Só é possível bloquear caução pendente");

            Status = StatusCaucao.Bloqueada;
        }

        internal void MarcarComoInadimplente()
        {
            if (Status == StatusCaucao.Inadimplente)
                throw new InvalidOperationException("Caução já está inadimplente");

            Status = StatusCaucao.Inadimplente;
        }
        public enum StatusCaucao
        {
            Pendente,
            Bloqueada,
            Inadimplente
        }
    }

}
