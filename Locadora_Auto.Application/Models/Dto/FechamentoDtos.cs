using System.ComponentModel.DataAnnotations;

namespace Locadora_Auto.Application.Models.Dto
{
    /// <summary>
    /// Doc 07 §1: encerrar a <b>posse</b>. Não fecha o contrato e não apura nada — quem faz isso é
    /// a apuração, que é o ato seguinte.
    ///
    /// Não recebe o hodômetro: ele sai da vistoria de devolução (RN-11), que a devolução já exige.
    /// </summary>
    public class RegistrarDevolucaoDto
    {
        [Required]
        public DateTime DataFimReal { get; set; }

        [Required]
        public int IdFilialDevolucao { get; set; }
    }

    /// <summary>
    /// Doc 07 §1: apurar a conta. Idempotente (RN-32) — chamar de novo devolve a mesma apuração.
    /// </summary>
    public class ApurarFechamentoDto
    {
        /// <summary>
        /// Quem apurou. O indicador de vazamento de receita do doc 07 §12 abre por atendente, e é
        /// esta a resposta.
        /// </summary>
        [Required]
        public int IdFuncionarioApuracao { get; set; }

        /// <summary>
        /// RN-22: só para o caso de a filial de devolução não estar habilitada para one-way, que
        /// bloqueia o fechamento. Sem os dois, a apuração recusa.
        /// </summary>
        public int? IdFuncionarioAlcada { get; set; }

        [StringLength(500)]
        public string? MotivoAlcada { get; set; }
    }

    /// <summary>
    /// RN-31: uma linha discriminada do extrato. É o que o cliente lê para conferir a conta item a
    /// item, e a razão de a conta não sair agregada.
    /// </summary>
    public class LinhaFechamentoDto
    {
        public int IdLinhaFechamento { get; set; }
        public string Tipo { get; set; } = null!;

        /// <summary>Débito ou crédito. Sai do tipo, não de um sinal no valor.</summary>
        public string Natureza { get; set; } = null!;

        /// <summary>Como se chegou ao número. É o que sustenta a linha quando ela é contestada.</summary>
        public string BaseCalculo { get; set; } = null!;

        public decimal Quantidade { get; set; }
        public decimal ValorUnitario { get; set; }
        public decimal Total { get; set; }
        public DateTime DataLancamento { get; set; }

        /// <summary>RN-31: lançada depois da selagem, para ajustar conta já apurada.</summary>
        public bool EhCorrecao { get; set; }
        public int? IdFuncionarioLancamento { get; set; }
        public string? Motivo { get; set; }
    }

    /// <summary>O extrato do contrato — a conta discriminada que o cliente recebe.</summary>
    public class FechamentoLocacaoDto
    {
        public int IdFechamento { get; set; }
        public int IdLocacao { get; set; }
        public DateTime DataApuracao { get; set; }
        public int IdFuncionarioApuracao { get; set; }
        public DateTime? DataSelagem { get; set; }
        public bool Selado { get; set; }

        public decimal TotalDebitos { get; set; }
        public decimal TotalCreditos { get; set; }

        /// <summary>RN-29: pode ser negativo — crédito a devolver ao cliente.</summary>
        public decimal Saldo { get; set; }

        public List<LinhaFechamentoDto> Linhas { get; set; } = new();
    }

    /// <summary>
    /// O que a apuração respondeu. Separado do extrato de propósito: o
    /// <see cref="FechamentoLocacaoDto"/> é a conta, e isto aqui é o que <b>esta chamada</b> fez —
    /// quanto a caução quitou, o que sobrou para cobrar, e os avisos que não cabem numa linha.
    /// </summary>
    public class ResultadoDaApuracaoDto
    {
        public FechamentoLocacaoDto Fechamento { get; set; } = null!;

        /// <summary>RN-30: quanto a garantia quitou.</summary>
        public decimal CaucaoConsumida { get; set; }

        /// <summary>RN-30: o que sobrou para cobrar depois da caução. Alimenta a régua de cobrança.</summary>
        public decimal SaldoResidual { get; set; }

        /// <summary>RN-29: o que a casa deve ao cliente.</summary>
        public decimal CreditoADevolver { get; set; }

        /// <summary>
        /// RN-32: a conta já estava apurada e esta chamada não mexeu em nada. É o que separa uma
        /// retentativa de rede de uma cobrança em dobro.
        /// </summary>
        public bool JaEstavaApurado { get; set; }

        /// <summary>
        /// O que não entrou na conta e alguém precisa saber: avaria em análise com o prazo do
        /// pós-contrato, multa deixada de fora por já estar coberta por linha apurada, e cobrança
        /// não feita por falta de cadastro. Nada some em silêncio.
        /// </summary>
        public List<string> Avisos { get; set; } = new();
    }
}
