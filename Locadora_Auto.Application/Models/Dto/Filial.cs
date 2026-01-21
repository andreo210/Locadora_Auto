namespace Locadora_Auto.Application.Models.Dto
{
    using System.ComponentModel.DataAnnotations;

    namespace Locadora_Auto.Application.Models.Dto
    {
        public class FilialDto
        {
            public int IdFilial { get; set; }

            [Required]
            [StringLength(100)]
            public string Nome { get; set; } = string.Empty;

            [Required]
            [StringLength(100)]
            public string Cidade { get; set; } = string.Empty;

            public bool Ativo { get; set; }

            // Endereço inline (para simplificar)
            public string Logradouro { get; set; } = string.Empty;
            public string Numero { get; set; } = string.Empty;
            public string? Complemento { get; set; }
            public string Bairro { get; set; } = string.Empty;
            public string Estado { get; set; } = string.Empty;
            public string Cep { get; set; } = string.Empty;

            // Estatísticas (opcional)
            public int TotalVeiculos { get; set; }
            public int VeiculosDisponiveis { get; set; }
            public int TotalLocacoesMes { get; set; }

            public DateTime DataCriacao { get; set; }
            public DateTime? DataModificacao { get; set; }
        }

        public class CriarFilialDto
        {
            [Required(ErrorMessage = "Nome é obrigatório")]
            [StringLength(100, ErrorMessage = "Nome deve ter no máximo 100 caracteres")]
            public string Nome { get; set; } = string.Empty;

            [Required(ErrorMessage = "Cidade é obrigatória")]
            [StringLength(100, ErrorMessage = "Cidade deve ter no máximo 100 caracteres")]
            public string Cidade { get; set; } = string.Empty;

            [Required(ErrorMessage = "Logradouro é obrigatório")]
            [StringLength(200, ErrorMessage = "Logradouro deve ter no máximo 200 caracteres")]
            public string Logradouro { get; set; } = string.Empty;

            [Required(ErrorMessage = "Número é obrigatório")]
            [StringLength(10, ErrorMessage = "Número deve ter no máximo 10 caracteres")]
            public string Numero { get; set; } = string.Empty;

            [StringLength(50, ErrorMessage = "Complemento deve ter no máximo 50 caracteres")]
            public string? Complemento { get; set; }

            [Required(ErrorMessage = "Bairro é obrigatório")]
            [StringLength(100, ErrorMessage = "Bairro deve ter no máximo 100 caracteres")]
            public string Bairro { get; set; } = string.Empty;

            [Required(ErrorMessage = "Estado é obrigatório")]
            [StringLength(2, MinimumLength = 2, ErrorMessage = "Estado deve ter 2 caracteres")]
            public string Estado { get; set; } = string.Empty;

            [Required(ErrorMessage = "CEP é obrigatório")]
            [RegularExpression(@"^\d{8}$", ErrorMessage = "CEP deve ter 8 dígitos")]
            public string Cep { get; set; } = string.Empty;

            public bool Ativo { get; set; } = true;
        }

        public class AtualizarFilialDto
        {
            [StringLength(100, ErrorMessage = "Nome deve ter no máximo 100 caracteres")]
            public string? Nome { get; set; }

            [StringLength(100, ErrorMessage = "Cidade deve ter no máximo 100 caracteres")]
            public string? Cidade { get; set; }

            [StringLength(200, ErrorMessage = "Logradouro deve ter no máximo 200 caracteres")]
            public string? Logradouro { get; set; }

            [StringLength(10, ErrorMessage = "Número deve ter no máximo 10 caracteres")]
            public string? Numero { get; set; }

            [StringLength(50, ErrorMessage = "Complemento deve ter no máximo 50 caracteres")]
            public string? Complemento { get; set; }

            [StringLength(100, ErrorMessage = "Bairro deve ter no máximo 100 caracteres")]
            public string? Bairro { get; set; }

            [StringLength(2, MinimumLength = 2, ErrorMessage = "Estado deve ter 2 caracteres")]
            public string? Estado { get; set; }

            [RegularExpression(@"^\d{8}$", ErrorMessage = "CEP deve ter 8 dígitos")]
            public string? Cep { get; set; }

            public bool? Ativo { get; set; }
        }

        public class FilialResumoDto
        {
            public int IdFilial { get; set; }
            public string Nome { get; set; } = string.Empty;
            public string Cidade { get; set; } = string.Empty;
            public string Estado { get; set; } = string.Empty;
            public bool Ativo { get; set; }
            public int TotalVeiculos { get; set; }
            public int VeiculosDisponiveis { get; set; }
        }
    }
}
