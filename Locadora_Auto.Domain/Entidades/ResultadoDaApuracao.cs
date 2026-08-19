namespace Locadora_Auto.Domain.Entidades
{
    /// <summary>
    /// O que a apuração completa (<see cref="Locacao.ApurarFechamento"/>) tem a dizer — a conta e
    /// tudo o que quem chamou precisa saber para responder ao balcão.
    ///
    /// Existe porque três coisas não cabem no saldo e não podem sumir: a avaria que ficou em
    /// análise (RN-24) tem prazo a comunicar, a multa recusada por redundância precisa de alguém
    /// avisado, e o saldo residual é o que dispara a régua de cobrança. Devolver só o
    /// <see cref="FechamentoLocacao"/> obrigaria quem chama a redescobrir tudo isso relendo linhas.
    /// </summary>
    public sealed class ResultadoDaApuracao
    {
        public required FechamentoLocacao Fechamento { get; init; }

        /// <summary>
        /// RN-24. Nulo quando a apuração não rodou nesta chamada (contrato já estava fechado) —
        /// o que ficou em análise já foi comunicado na primeira vez.
        /// </summary>
        public ApuracaoDeAvarias? Avarias { get; init; }

        /// <summary>
        /// RN-26: as multas que não entraram na conta por já estarem cobertas por linha apurada.
        /// Não somem em silêncio — quem chama avisa.
        /// </summary>
        public IReadOnlyList<Multa> MultasRecusadas { get; init; } = Array.Empty<Multa>();

        /// <summary>
        /// RN-14: como o combustível foi apurado. Interessa quando <b>não</b> foi cobrado por falta
        /// de cadastro — tanque não informado ou preço do litro zerado —, que é receita perdida em
        /// silêncio se ninguém for avisado. Nulo quando a apuração não rodou nesta chamada.
        /// </summary>
        public ApuracaoDeCombustivel? Combustivel { get; init; }

        /// <summary>RN-30: quanto a garantia quitou.</summary>
        public decimal CaucaoConsumida { get; init; }

        /// <summary>
        /// RN-32: a conta já estava selada e esta chamada não mexeu em nada. É a resposta que
        /// separa uma retentativa de rede de uma cobrança em dobro.
        /// </summary>
        public bool JaEstavaApurado { get; init; }

        /// <summary>RN-27: o que a conta deu, já abatidos os pagamentos confirmados.</summary>
        public decimal Saldo => Fechamento.Saldo;

        /// <summary>RN-30: o que sobrou para cobrar depois de a caução ser consumida.</summary>
        public decimal SaldoResidual => Math.Max(0m, Saldo - CaucaoConsumida);

        /// <summary>
        /// RN-29: o que a casa deve ao cliente. Positivo quando o saldo é negativo — e não é o
        /// mesmo que caução a devolver, que é garantia não tocada.
        /// </summary>
        public decimal CreditoADevolver => Math.Max(0m, -Saldo);
    }
}
