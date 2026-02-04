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

        public Reserva() { }

        public Clientes Cliente { get; private set; } = null!;
        public Filial Filial { get; private set; } = null!;
        public CategoriaVeiculo CategoriaVeiculo { get;  private set; } = null!;
        internal static Reserva Criar(int idCliente, DateTime inicio,int idilial, DateTime fim, CategoriaVeiculo veiculo)
        {
            if (idCliente == 0)
                throw new InvalidOperationException("Id cliente não pode ser zero");

            if (veiculo == null)
                throw new InvalidOperationException("veiculo não pode ser nulo");

            if (veiculo.Id == 0)
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
                IdCategoria = veiculo.Id,
                IdFilial = idilial
            };           
        }


        public void Atualizar(DateTime inicio, DateTime fim)
        {

            if (inicio <= DateTime.Now)
                throw new InvalidOperationException("data do inicio não pode ser menor que data atual");

            if (fim <= DateTime.Now)
                throw new InvalidOperationException("data do final não pode ser menor que data atual");

            if (fim <= inicio)
                throw new InvalidOperationException("data do final não pode ser menor que data inicio");
            
            DataFim = fim;
            DataInicio = inicio;
        }



        public void Cancelar()
        {
            Status = StatusReserva.Cancelado;
        }

        public void Finalizar(Veiculo veiculo )
        {
            Status = StatusReserva.Finalizado;
            veiculo.Disponibilizar();
        }


        public void Ativar()
        {
            Ativo = true;
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
        Finalizado
    }

}


