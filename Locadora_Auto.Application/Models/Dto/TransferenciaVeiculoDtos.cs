namespace Locadora_Auto.Application.Models.Dto
{
    /// <summary>
    /// RN-49: manda o veículo para outra filial. A origem não vem no corpo — ela é a filial atual
    /// do próprio veículo, e aceitá-la de fora abriria caminho para o remanejamento gravar uma
    /// origem que não é de onde o carro saiu.
    /// </summary>
    public class EnviarTransferenciaDto
    {
        public int IdFilialDestino { get; set; }

        /// <summary>
        /// Tem que ser futura. Sem prazo, carro que sumiu na estrada não aparece em lugar
        /// nenhum — ele não está na oferta de nenhuma das duas filiais.
        /// </summary>
        public DateTime DataPrevistaChegada { get; set; }

        /// <summary>Quem responde pelo carro enquanto ele está entre as duas filiais.</summary>
        public int IdFuncionarioResponsavel { get; set; }

        public string? Observacao { get; set; }
    }

    /// <summary>
    /// Chegada ao destino. O hodômetro é obrigatório: o trecho foi rodado, e não registrá-lo faria
    /// a próxima devolução parecer ter percorrido a viagem inteira.
    /// </summary>
    public class ChegadaTransferenciaDto
    {
        public int KmChegada { get; set; }
    }

    public class TransferenciaVeiculoDto
    {
        public int IdTransferenciaVeiculo { get; set; }
        public int IdVeiculo { get; set; }

        public int IdFilialOrigem { get; set; }
        public string? FilialOrigem { get; set; }

        public int IdFilialDestino { get; set; }
        public string? FilialDestino { get; set; }

        public DateTime DataEnvio { get; set; }
        public DateTime DataPrevistaChegada { get; set; }
        public DateTime? DataChegada { get; set; }

        public int IdStatus { get; set; }
        public string Status { get; set; } = null!;

        public int IdFuncionarioResponsavel { get; set; }
        public string? Responsavel { get; set; }

        public string? Observacao { get; set; }

        /// <summary>
        /// Em trânsito além da data prevista — o carro que sumiu na estrada. Vem calculado para a
        /// tela não repetir a regra.
        /// </summary>
        public bool Atrasada { get; set; }
    }
}
