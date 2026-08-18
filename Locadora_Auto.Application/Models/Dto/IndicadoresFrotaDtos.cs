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
        /// <c>Bloqueado</c> e em <c>Desmobilizado</c> — carro fora da oferta por decisão
        /// administrativa não é frota operacional, e carro vendido não é frota nenhuma.
        ///
        /// <c>EmTransferencia</c> continua no denominador: é meio para alugar o carro em outro
        /// lugar, não renúncia a alugá-lo, e a utilização deve mesmo cair quando a estrada demora.
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

        /// <summary>
        /// Bloqueios (RN-52) ainda abertos cuja data prevista de liberação já passou.
        ///
        /// É medida do <b>instante</b>, não do período: a pergunta é "quantos carros estão fora da
        /// oferta agora sem que ninguém tenha percebido". É este número que a RN-52 existe para
        /// tornar possível — antes dela, bloqueio não tinha prazo e a pergunta não tinha resposta.
        ///
        /// Só conta bloqueio de verdade. Veículo apenas desativado também fica em
        /// <c>Bloqueado</c>, mas isso é ato de cadastro, aparece em qualquer filtro por
        /// <c>Ativo</c> e não some da vista de ninguém.
        /// </summary>
        public int BloqueiosVencidos { get; set; }

        /// <summary>
        /// Movimentos do período cujo tipo de origem exige documento e está sem ele.
        ///
        /// <b>Tem que ser zero.</b> É controle de auditoria, não métrica de operação: qualquer
        /// número acima disso significa que uma transição de ativo perdeu o documento que a
        /// autorizou, e aí a conciliação de frota deixa de fechar. Contrato, ordem de serviço,
        /// bloqueio e transferência são os tipos que exigem; cadastro, pátio, prazo e
        /// desmobilização são o próprio ato e não têm documento a citar.
        /// </summary>
        public int TransicoesSemDocumento { get; set; }

        /// <summary>
        /// Tentativas de abrir ou estender contrato sobre veículo já comprometido no período
        /// (RN-40), recusadas dentro da janela.
        ///
        /// A recusa funcionou — nenhum cliente ficou sem carro. O que o número mede é
        /// <b>processo</b>: se sobe numa filial, o balcão de lá está escolhendo placa comprometida,
        /// e a causa é agenda de pátio desatualizada, treinamento ou frota curta. Não é defeito de
        /// sistema, e é por isso que ele é acompanhado por filial e não como total da rede.
        /// </summary>
        public int TentativasSobreposicaoRecusadas { get; set; }

        /// <summary>
        /// O mesmo número aberto por filial de retirada — que é onde a tentativa foi feita, e não
        /// necessariamente a filial atual do veículo.
        /// </summary>
        public List<RecusaPorFilialDto> RecusasPorFilial { get; set; } = new();
    }

    public class RecusaPorFilialDto
    {
        public int IdFilial { get; set; }
        public int Total { get; set; }

        /// <summary>
        /// Barradas pela consulta do serviço, antes de gravar. É o caso normal: o atendente
        /// escolheu placa comprometida e o sistema avisou a tempo.
        /// </summary>
        public int PelaConsulta { get; set; }

        /// <summary>
        /// Barradas pela constraint do banco (RN-41). Só acontece em concorrência real — dois
        /// atendentes abrindo o mesmo período no mesmo instante. Aqui o processo do balcão não
        /// errou: dois pontos de venda disputaram o mesmo carro, e o que falta é frota ou
        /// coordenação entre canais.
        /// </summary>
        public int PeloBanco { get; set; }
    }

    public class TempoPorSituacaoDto
    {
        public int IdStatus { get; set; }
        public string Status { get; set; } = null!;
        public double Dias { get; set; }
        public decimal PercentualDoTempo { get; set; }
    }
}
