namespace Locadora_Auto.Domain.Entidades
{
    public class Reserva
    {
        public int IdReserva { get; set; }
        public int IdCliente { get; set; }
        public int IdCategoria { get; set; }
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
        public string Status { get; set; } = null!;

        public Clientes Cliente { get; set; } = null!;
        public CategoriaVeiculo Categoria { get; set; } = null!;
    }
}

