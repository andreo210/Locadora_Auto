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

        /// <summary>0 Reservado, 1 Cancelado, 2 Finalizado, 3 Expirado.</summary>
        public int IdStatus { get; set; }
        public string? Status { get; set; }

        public string? NomeCliente { get; set; }
        public string? NomeFilial { get; set; }
        public string? NomeCategoriaVeiculo { get; set; }
    }
}
