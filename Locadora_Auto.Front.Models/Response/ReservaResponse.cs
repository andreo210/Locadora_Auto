namespace Locadora_Auto.Front.Models.Response
{
    public class ReservaResponse
    {
        public int IdCliente { get; set; }
        public int IdFilial { get; set; }
        public int IdCategoriaVeiculo { get; set; }
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
        public int IdReserva { get; set; }
        public bool Ativo { get; set; }
    }
}
