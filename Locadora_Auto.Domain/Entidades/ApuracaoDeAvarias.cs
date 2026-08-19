using System.Globalization;

namespace Locadora_Auto.Domain.Entidades
{
    /// <summary>
    /// RN-24 e RN-25: o que a avaria custa ao cliente.
    ///
    /// Duas regras, e a segunda é a que mais gera conflito no balcão: havendo proteção, a cobrança
    /// é limitada à franquia contratada <b>somando todas as avarias</b>, e não por avaria. Um carro
    /// que volta com três amassados de R$ 1.500 cada não custa três franquias — custa uma.
    ///
    /// A primeira é a que mais gera caução retida: só avaria já <b>decidida</b> entra na conta. O
    /// que ainda está em análise não segura o fechamento nem o dinheiro do cliente; vira pendência
    /// de pós-contrato, com prazo declarado.
    /// </summary>
    public sealed class ApuracaoDeAvarias
    {
        /// <summary>
        /// Trinta dias a partir da devolução para a casa fechar o que ficou em análise.
        ///
        /// É política da empresa, não exigência legal — e é uniforme na rede, ao contrário do preço
        /// do litro, por isso constante e não parâmetro de filial. O que ele existe para impedir é a
        /// avaria "em apuração" por tempo indefinido, que é caução retida e cliente irritado.
        /// </summary>
        public const int PrazoPosContratoDias = 30;

        /// <summary>Soma das avarias em <c>Aprovado</c> ou <c>Cobrado</c>, antes da franquia.</summary>
        public decimal TotalApurado { get; private init; }

        public bool TemProtecao { get; private init; }

        /// <summary>RN-25: o teto contratado. Zero quando não há proteção cobrindo a devolução.</summary>
        public decimal FranquiaContratada { get; private init; }

        /// <summary>
        /// RN-25: o que a proteção absorve — o que passou da franquia, somadas todas as avarias.
        /// Zero quando não há proteção ou quando o total já cabe dentro dela.
        /// </summary>
        public decimal AbatimentoPorProtecao { get; private init; }

        /// <summary>O que de fato se cobra do cliente.</summary>
        public decimal TotalCobravel => TotalApurado - AbatimentoPorProtecao;

        /// <summary>RN-24: quantas ficaram em análise, e por quanto. Não entram na conta.</summary>
        public int AvariasEmAnalise { get; private init; }
        public decimal ValorEmAnalise { get; private init; }

        /// <summary>
        /// Até quando a casa se comprometeu a resolver o que ficou em análise. Nulo quando não
        /// ficou nada.
        /// </summary>
        public DateTime? PrazoDoPosContrato { get; private init; }

        public bool TemPendenciaDePosContrato => AvariasEmAnalise > 0;

        private ApuracaoDeAvarias() { }

        private static readonly CultureInfo Brasil = CultureInfo.GetCultureInfo("pt-BR");

        /// <summary>
        /// Avarias que já foram decididas e por isso entram no fechamento (RN-24).
        /// <c>Pago</c> entra junto: foi decidida e cobrada, e o abatimento do que já foi pago é da
        /// composição (RN-28), não daqui.
        /// </summary>
        private static readonly StatusDano[] Cobraveis =
        {
            StatusDano.Aprovado,
            StatusDano.Cobrado,
            StatusDano.Pago
        };

        public static ApuracaoDeAvarias Calcular(
            IEnumerable<Dano> danos,
            decimal? franquiaContratada,
            DateTime dataFimReal)
        {
            ArgumentNullException.ThrowIfNull(danos);

            if (franquiaContratada is < 0)
                throw new DomainException("Franquia contratada não pode ser negativa");

            var lista = danos.ToList();

            var total = lista
                .Where(d => Cobraveis.Contains(d.Status))
                .Sum(d => d.ValorEstimado);

            // `Registrado` conta junto com `EmAnalise`: os dois são avaria sem decisão, e a
            // diferença entre eles é de fluxo interno, não de efeito sobre a conta do cliente.
            // `Isento` e `Cancelado` ficam de fora — foram decididos, e a decisão foi não cobrar
            var emAnalise = lista
                .Where(d => d.Status is StatusDano.Registrado or StatusDano.EmAnalise)
                .ToList();

            var temProtecao = franquiaContratada.HasValue;
            var franquia = franquiaContratada ?? 0m;

            // RN-25: o teto é sobre a **soma**. Aplicar por avaria multiplicaria a franquia pelo
            // número de amassados, que é exatamente o que o cliente contesta — e ganha
            var abatimento = temProtecao
                ? Math.Max(0m, total - franquia)
                : 0m;

            return new ApuracaoDeAvarias
            {
                TotalApurado = total,
                TemProtecao = temProtecao,
                FranquiaContratada = franquia,
                AbatimentoPorProtecao = abatimento,
                AvariasEmAnalise = emAnalise.Count,
                ValorEmAnalise = emAnalise.Sum(d => d.ValorEstimado),
                PrazoDoPosContrato = emAnalise.Count > 0
                    ? dataFimReal.AddDays(PrazoPosContratoDias)
                    : null
            };
        }

        public string BaseCalculoDoAbatimento()
            => string.Format(Brasil,
                "proteção contratada com franquia de {0:C}: absorve o que passou dela na soma de todas as avarias ({1:C} apurados)",
                FranquiaContratada, TotalApurado);
    }
}
