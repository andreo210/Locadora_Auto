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

        internal void Deduzir(decimal valor)
        {
            if (valor <= 0)
                throw new DomainException("Valor inválido para dedução");

            if (valor > Valor)
                throw new DomainException("Valor excede a caução");

            Valor -= valor;

            if (Valor == 0)
                Status = StatusCaucao.Bloqueada;
        }

        internal void Bloquear()
        {
            if (Status != StatusCaucao.Pendente)
                throw new InvalidOperationException("Só é possível bloquear caução pendente");

            Status = StatusCaucao.Bloqueada;
        }


        internal void Devolver()
        {
            if (Status != StatusCaucao.Pendente)
                throw new DomainException("Somente caução pendente pode ser devolvida");

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
