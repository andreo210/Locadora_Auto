using System.ComponentModel.DataAnnotations;

namespace Locadora_Auto.Front.Models.Request.Reserva
{
    public class CriarReservaRequest
    {
        [Required(ErrorMessage = "O cliente é obrigatório")]
        [Range(1, int.MaxValue, ErrorMessage = "Selecione um cliente")]
        public int IdCliente { get; set; }

        [Required(ErrorMessage = "A filial é obrigatória")]
        [Range(1, int.MaxValue, ErrorMessage = "Selecione uma filial")]
        public int IdFilial { get; set; }

        [Required(ErrorMessage = "A categoria de veículo é obrigatória")]
        [Range(1, int.MaxValue, ErrorMessage = "Selecione uma categoria de veículo")]
        public int IdCategoriaVeiculo { get; set; }

        [Required(ErrorMessage = "A data de início é obrigatória")]
        public DateTime DataInicio { get; set; } = DateTime.Now.Date.AddDays(1).AddHours(9);

        [Required(ErrorMessage = "A data de entrega é obrigatória")]
        public DateTime DataFim { get; set; } = DateTime.Now.Date.AddDays(3).AddHours(9);
    }
}
