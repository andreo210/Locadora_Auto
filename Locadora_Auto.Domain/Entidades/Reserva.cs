namespace Locadora_Auto.Domain.Entidades
{
    public class Reserva
    {
        public int IdReserva { get; private set; }
        public int IdCliente { get; private set; }
        public int IdCategoria { get; private set; }
        public int IdFilial { get; private set; }
        public DateTime DataInicio { get; private set; }
        public DateTime DataFim { get; private set; }
        public StatusReserva Status { get; private set; }
        public bool Ativo { get; private set; }

        protected Reserva() { }

        public Clientes Cliente { get; private set; } = null!;
        public Filial Filial { get; private set; } = null!;
        public CategoriaVeiculo CategoriaVeiculo { get;  private set; } = null!;

        internal static Reserva Criar(int idCliente, DateTime inicio,int idilial, DateTime fim, int idCategoria)
        {
            if (idCliente == 0)
                throw new InvalidOperationException("Id cliente não pode ser zero");

            if (idCategoria == 0)
                throw new InvalidOperationException("Id veiculo não pode ser nulo");

            if (inicio <= DateTime.Now)
                throw new InvalidOperationException("data do inicio não pode ser menor que data atual");

            if (fim <= DateTime.Now)
                throw new InvalidOperationException("data do final não pode ser menor que data atual");

            if (fim <= inicio)
                throw new InvalidOperationException("data do final não pode ser menor que data inicio");


            return new Reserva
            {
                Status = StatusReserva.Reservado,
                Ativo = true,
                DataFim = fim,
                DataInicio = inicio,
                IdCliente = idCliente,
                IdCategoria = idCategoria,
                IdFilial = idilial
            };           
        }

        public void Cancelar()
        {
            if (Status != StatusReserva.Reservado)
                throw new DomainException("Somente reservas ativas podem ser canceladas");

            Status = StatusReserva.Cancelado;
            Ativo = false;
        }

        public void Expirar(DateTime agora)
        {
            if (Status == StatusReserva.Reservado && agora.Date > DataInicio.Date)
            {
                Status = StatusReserva.Expirado;
                Ativo = false;
            }
        }

        public void Finalizar()
        {
            if (Status != StatusReserva.Reservado)
                throw new DomainException("Reserva não pode ser finalizada");

            Status = StatusReserva.Finalizado;
            Ativo = false;
        }


        public void Desativar()
        {
            Ativo = false;
        }
    }

    public enum StatusReserva
    {
        Reservado,
        Cancelado,
        Finalizado,
        Expirado
    }

}


