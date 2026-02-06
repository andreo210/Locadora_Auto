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

            public EnderecoDto Endereco { get; set; } = null!;

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
            public EnderecoDto Endereco { get; set; } = null!;
        }

        public class AtualizarFilialDto
        {
            [StringLength(100, ErrorMessage = "Nome deve ter no máximo 100 caracteres")]
            public string? Nome { get; set; }

            [StringLength(100, ErrorMessage = "Cidade deve ter no máximo 100 caracteres")]
            public string? Cidade { get; set; }
            public EnderecoDto Endereco { get; set; } = null!;

        }

        public class FilialResumoDto
        {
            public int IdFilial { get; set; }
            public string Nome { get; set; } = string.Empty;
            public string Cidade { get; set; } = string.Empty;
            public EnderecoDto Endereco { get; set; } = null!;
            public bool Ativo { get; set; }
            public int TotalVeiculos { get; set; }
            public int VeiculosDisponiveis { get; set; }
        }
    }
}
