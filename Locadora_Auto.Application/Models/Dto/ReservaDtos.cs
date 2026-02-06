namespace Locadora_Auto.Application.Models.Dto
{
    public abstract class ReservaBaseDto
    {
        public int IdCliente { get; set; }
        public int IdFilial{ get; set; }
        public int IdCategoriaVeiculo { get; set; }
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
    }

    public class ReservaDto : ReservaBaseDto
    {
        public int IdReserva { get; set; }
        public bool Ativo { get; set; }


    }

    public class CriarReservaDto : ReservaBaseDto { }

    public class CancelarReservaDto
    {
        public int IdReserva { get; set; }
    }
}
