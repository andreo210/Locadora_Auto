namespace Locadora_Auto.Application.Models.Dto
{
    public class ReservaDto
    {
        public int IdReserva { get; set; }
        public int IdCliente { get; set; }
        public int IdCategoria { get; set; }
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
        public string Status { get; set; } = null!;
    }

}
