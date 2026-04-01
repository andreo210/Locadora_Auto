using System.ComponentModel.DataAnnotations;

namespace Locadora_Auto.Front.Models.Request.Categoria
{
    
    public class AtualizarCategoriaRequest
    {
        [Required(ErrorMessage = "O nome da categoria é obrigatório")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "O nome deve ter entre 3 e 100 caracteres")]
        public string? Nome { get; set; }

        [Required(ErrorMessage = "O valor da diária é obrigatório")]
        [Range(0.01, double.MaxValue, ErrorMessage = "O valor da diária deve ser maior que zero")]
        public double ValorDiaria { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "O limite de KM deve ser um valor positivo")]
        public int LimiteKm { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "O valor do KM excedente deve ser um valor positivo")]
        public double ValorKmExcedente { get; set; }
    }
    
}
