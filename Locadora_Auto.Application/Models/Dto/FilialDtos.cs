// o using fica aqui fora porque dentro do namespace `Locadora_Auto.Application.Models.Dto` o
// compilador resolveria `Locadora_Auto.Domain` como se fosse relativo a ele
using Locadora_Auto.Domain.Entidades;

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

            /// <summary>Minutos entre a devolução e o carro voltar à oferta desta filial.</summary>
            public int TempoPreparacaoMinutos { get; set; }

            // Estatísticas (opcional)
            public int TotalVeiculos { get; set; }
            public int VeiculosDisponiveis { get; set; }
            public int TotalLocacoesMes { get; set; }

            public DateTime DataCriacao { get; set; }
            public DateTime? DataModificacao { get; set; }

            public List<FotoDto>? Fotos { get; set; }
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

            /// <summary>Ausente assume o padrão da casa (<c>Filial.PreparacaoPadraoMinutos</c>).</summary>
            [Range(0, Filial.PreparacaoMaximaMinutos, ErrorMessage = "Tempo de preparação deve estar entre 0 e {2} minutos")]
            public int? TempoPreparacaoMinutos { get; set; }
        }

        public class AtualizarFilialDto
        {
            [StringLength(100, ErrorMessage = "Nome deve ter no máximo 100 caracteres")]
            public string? Nome { get; set; }

            [StringLength(100, ErrorMessage = "Cidade deve ter no máximo 100 caracteres")]
            public string? Cidade { get; set; }
            public EnderecoDto Endereco { get; set; } = null!;

            /// <summary>
            /// Anulável de propósito: ausente <b>mantém</b> o valor atual. Se fosse <c>int</c>, todo
            /// cliente que não conhece o campo — o Front de hoje, entre eles — zeraria a preparação
            /// da filial a cada edição, e o carro voltaria à oferta no instante da devolução.
            /// </summary>
            [Range(0, Filial.PreparacaoMaximaMinutos, ErrorMessage = "Tempo de preparação deve estar entre 0 e {2} minutos")]
            public int? TempoPreparacaoMinutos { get; set; }
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
