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

        /// <summary>
        /// Valor numérico de <see cref="Domain.Entidades.StatusReserva"/>, para o Front comparar sem depender do Domain.
        /// </summary>
        public int IdStatus { get; set; }
        public string? Status { get; set; }

        // nomes desnormalizados: evitam uma chamada por linha na listagem
        public string? NomeCliente { get; set; }
        public string? NomeFilial { get; set; }
        public string? NomeCategoriaVeiculo { get; set; }
    }

    public class CriarReservaDto : ReservaBaseDto { }
}
