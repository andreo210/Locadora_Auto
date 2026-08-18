namespace Locadora_Auto.Application.Models.Dto
{
    /// <summary>
    /// RN-52: o que o balcão precisa informar para tirar um carro da oferta. Os três campos
    /// obrigatórios são a regra inteira — sem motivo ninguém sabe por quê, sem prazo o carro some,
    /// sem responsável não há a quem cobrar.
    /// </summary>
    public class BloquearVeiculoDto
    {
        /// <summary>Valor de <c>MotivoBloqueio</c>. Fora do enum, a recusa é do serviço.</summary>
        public int IdMotivo { get; set; }

        /// <summary>Tem que ser futura: bloqueio que nasce vencido não é prazo, é esquecimento.</summary>
        public DateTime DataPrevistaLiberacao { get; set; }

        /// <summary>Quem responde por este carro voltar à oferta.</summary>
        public int IdFuncionarioResponsavel { get; set; }

        public string? Observacao { get; set; }
    }

    public class BloqueioVeiculoDto
    {
        public int IdBloqueioVeiculo { get; set; }
        public int IdVeiculo { get; set; }

        public int IdMotivo { get; set; }
        public string Motivo { get; set; } = null!;

        public string? Observacao { get; set; }

        public DateTime DataBloqueio { get; set; }
        public DateTime DataPrevistaLiberacao { get; set; }
        public DateTime? DataLiberacao { get; set; }

        /// <summary>
        /// Para onde o veículo volta quando o bloqueio for liberado. Aparece no DTO porque é o que
        /// responde a pergunta do balcão: liberar este carro o coloca à venda ou o devolve à fila
        /// do pátio?
        /// </summary>
        public int IdStatusAnterior { get; set; }
        public string StatusAnterior { get; set; } = null!;

        public int IdFuncionarioResponsavel { get; set; }
        public string? Responsavel { get; set; }

        public bool EmAberto { get; set; }

        /// <summary>
        /// Em aberto e passado do prazo. É a linha que compõe o indicador de bloqueios vencidos da
        /// seção 12 — e vem calculada aqui para a tela não repetir a regra.
        /// </summary>
        public bool Vencido { get; set; }
    }
}
