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

            /// <summary>RN-49: a filial manda e recebe veículo em remanejamento de frota.</summary>
            public bool PermiteTransferencia { get; set; }

            // Parâmetros do fechamento (doc 07 §9). Nenhum é lido pela apuração ainda.

            /// <summary>RN-21: a filial aceita devolução de contrato aberto em outra filial.</summary>
            public bool HabilitadaOneWay { get; set; }

            /// <summary>RN-21: o que o cliente paga para devolver aqui. Zero é cortesia.</summary>
            public decimal TaxaRetornoOneWay { get; set; }

            /// <summary>RN-03: atraso até este limite não vira cobrança.</summary>
            public int ToleranciaMinutos { get; set; }

            /// <summary>RN-04: fração da diária por hora excedente iniciada.</summary>
            public decimal PercentualHoraExcedente { get; set; }

            /// <summary>RN-15: preço do litro no regime full-to-full. Zero é não configurado.</summary>
            public decimal PrecoLitroCombustivel { get; set; }

            /// <summary>RN-15: o que a casa cobra por abastecer, uma vez por contrato.</summary>
            public decimal TaxaServicoAbastecimento { get; set; }

            /// <summary>RN-23: valor fixo da limpeza especial.</summary>
            public decimal ValorLimpezaEspecial { get; set; }

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

            /// <summary>Ausente assume <c>true</c>: o caso normal é a filial participar (RN-49).</summary>
            public bool? PermiteTransferencia { get; set; }

            // Parâmetros do fechamento (doc 07 §9). Ausentes assumem o padrão da entidade — a
            // filial nova não fica sem tolerância nem sem percentual de hora excedente, e os
            // valores locais (combustível, limpeza, one-way) nascem zerados esperando quem os
            // conheça.

            /// <summary>Ausente assume <c>true</c>: a rede aceita one-way hoje (RN-21).</summary>
            public bool? HabilitadaOneWay { get; set; }

            [Range(0, double.MaxValue, ErrorMessage = "Taxa de retorno one-way não pode ser negativa")]
            public decimal? TaxaRetornoOneWay { get; set; }

            /// <summary>Ausente assume <c>Filial.ToleranciaPadraoMinutos</c> (RN-03).</summary>
            [Range(0, Filial.ToleranciaMaximaMinutos, ErrorMessage = "Tolerância deve estar entre 0 e {2} minutos")]
            public int? ToleranciaMinutos { get; set; }

            /// <summary>Ausente assume <c>Filial.PercentualHoraExcedentePadrao</c> (RN-04).</summary>
            [Range(0.0001, 1, ErrorMessage = "Percentual de hora excedente deve estar entre 0 e 1")]
            public decimal? PercentualHoraExcedente { get; set; }

            [Range(0, double.MaxValue, ErrorMessage = "Preço do litro não pode ser negativo")]
            public decimal? PrecoLitroCombustivel { get; set; }

            [Range(0, double.MaxValue, ErrorMessage = "Taxa de serviço de abastecimento não pode ser negativa")]
            public decimal? TaxaServicoAbastecimento { get; set; }

            [Range(0, double.MaxValue, ErrorMessage = "Valor da limpeza especial não pode ser negativo")]
            public decimal? ValorLimpezaEspecial { get; set; }
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

            /// <summary>
            /// Anulável pelo mesmo motivo do tempo de preparação: ausente <b>mantém</b> o valor
            /// atual. Se fosse <c>bool</c>, todo cliente que não conhece o campo tiraria a filial
            /// do remanejamento a cada edição, e a frota pararia de circular sem ninguém ter
            /// pedido isso.
            /// </summary>
            public bool? PermiteTransferencia { get; set; }

            // Parâmetros do fechamento (doc 07 §9). Todos anuláveis pela mesma razão dos dois
            // acima: ausente **mantém** o valor atual. Aqui a razão pesa mais — um cliente que não
            // conhece os campos zeraria, a cada edição de nome de filial, o preço do litro e o
            // valor da limpeza da praça inteira, e a apuração pararia de cobrar sem ninguém pedir.

            public bool? HabilitadaOneWay { get; set; }

            [Range(0, double.MaxValue, ErrorMessage = "Taxa de retorno one-way não pode ser negativa")]
            public decimal? TaxaRetornoOneWay { get; set; }

            [Range(0, Filial.ToleranciaMaximaMinutos, ErrorMessage = "Tolerância deve estar entre 0 e {2} minutos")]
            public int? ToleranciaMinutos { get; set; }

            [Range(0.0001, 1, ErrorMessage = "Percentual de hora excedente deve estar entre 0 e 1")]
            public decimal? PercentualHoraExcedente { get; set; }

            [Range(0, double.MaxValue, ErrorMessage = "Preço do litro não pode ser negativo")]
            public decimal? PrecoLitroCombustivel { get; set; }

            [Range(0, double.MaxValue, ErrorMessage = "Taxa de serviço de abastecimento não pode ser negativa")]
            public decimal? TaxaServicoAbastecimento { get; set; }

            [Range(0, double.MaxValue, ErrorMessage = "Valor da limpeza especial não pode ser negativo")]
            public decimal? ValorLimpezaEspecial { get; set; }
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
