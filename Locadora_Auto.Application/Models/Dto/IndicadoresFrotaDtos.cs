namespace Locadora_Auto.Application.Models.Dto
{
    /// <summary>
    /// Os indicadores da seção 12 da especificação do ativo, apurados sobre a trilha da RN-37.
    ///
    /// São medidas <b>físicas</b>: contam o que o ativo fez, não o que foi faturado. A utilização
    /// comercial do mercado (diárias faturadas ÷ frota × dias) é outro número e sai do contrato —
    /// a diferença entre as duas é informação, não erro: diária faturada acima do tempo em
    /// <c>Locado</c> aponta cobrança sem carro na rua; tempo em <c>Locado</c> acima da diária
    /// faturada aponta carro na rua sem contrato, que é vazamento de receita.
    /// </summary>
    public class IndicadoresFrotaDto
    {
        public DateTime De { get; set; }
        public DateTime Ate { get; set; }

        /// <summary>Veículos que o filtro (filial/categoria) selecionou.</summary>
        public int VeiculosNoRecorte { get; set; }

        /// <summary>
        /// Quantos deles tinham trilha na janela. Enquanto a RN-37 for recente este número fica
        /// abaixo do recorte — carro que não se moveu desde a implantação não tem linha nenhuma, e
        /// tratar isso como frota parada distorceria os dois indicadores.
        /// </summary>
        public int VeiculosComTrilha { get; set; }

        /// <summary>Tempo em <c>Locado</c> ÷ tempo de frota ativa, em %.</summary>
        public decimal UtilizacaoRealPercentual { get; set; }

        public double DiasLocado { get; set; }

        /// <summary>
        /// Denominador da utilização: todo o tempo apurado menos o que o veículo passou em
        /// <c>Indisponivel</c> — carro fora da oferta por decisão administrativa não é frota
        /// operacional e não pode puxar a utilização para baixo.
        /// </summary>
        public double DiasFrotaAtiva { get; set; }

        /// <summary>
        /// Média das preparações <b>encerradas</b>, em horas. Nulo quando nenhuma fechou na janela.
        /// </summary>
        public double? TempoMedioPreparacaoHoras { get; set; }

        public int PreparacoesEncerradas { get; set; }

        /// <summary>
        /// Preparações que começaram na janela e não tinham saída até o fim dela. Vem junto com a
        /// média de propósito: sem este número, um pátio que nunca libera carro nenhum exibiria a
        /// melhor média da rede, porque só as rápidas entrariam na conta.
        /// </summary>
        public int PreparacoesEmAberto { get; set; }

        /// <summary>
        /// Onde o tempo da frota foi parar. É a "frota parada por motivo" da seção 12 medida em
        /// tempo — mais útil que o total, porque parada por oficina é problema de suprimento e
        /// parada por pátio é problema de processo.
        /// </summary>
        public List<TempoPorSituacaoDto> TempoPorSituacao { get; set; } = new();
    }

    public class TempoPorSituacaoDto
    {
        public int IdStatus { get; set; }
        public string Status { get; set; } = null!;
        public double Dias { get; set; }
        public decimal PercentualDoTempo { get; set; }
    }
}
