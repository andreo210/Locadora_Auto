using System.ComponentModel.DataAnnotations;

namespace Locadora_Auto.Front.Models.Request.Seguro
{
    public class CriarSeguroRequest
    {
        [Required(ErrorMessage = "O nome do seguro é obrigatório")]
        [StringLength(45, MinimumLength = 3, ErrorMessage = "O nome deve ter entre 3 e 45 caracteres")]
        public string? Nome { get; set; }

        [Required(ErrorMessage = "A descrição é obrigatória")]
        [StringLength(100, ErrorMessage = "A descrição deve ter no máximo 100 caracteres")]
        public string? Descricao { get; set; }

        [Required(ErrorMessage = "O valor da diária é obrigatório")]
        [Range(0.01, double.MaxValue, ErrorMessage = "O valor da diária deve ser maior que zero")]
        [DataType(DataType.Currency)]
        public decimal ValorDiaria { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "A franquia não pode ser negativa")]
        [DataType(DataType.Currency)]
        public decimal Franquia { get; set; }

        [Required(ErrorMessage = "A cobertura é obrigatória")]
        [StringLength(200, ErrorMessage = "A cobertura deve ter no máximo 200 caracteres")]
        public string? Cobertura { get; set; }
    }
}
