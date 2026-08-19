using System.Globalization;

namespace Locadora_Auto.Domain.Entidades
{
    /// <summary>
    /// RN-18 e RN-19: quanto a proteção contratada custa neste contrato.
    ///
    /// O caso comum é trivial — proteção vendida no balcão junto com o contrato, cobrada pelas
    /// mesmas diárias que o período (RN-18). O que esta classe existe para resolver é o outro:
    /// proteção contratada depois do início ou cancelada no meio, que a RN-19 manda cobrar
    /// <b>pró-rata</b> pela janela em que de fato cobriu.
    ///
    /// RN-20 não é implementada aqui e sim onde ela morde: a proteção limita a cobrança de
    /// <b>avaria</b> à franquia (RN-25, backlog A9) e não encosta em combustível, limpeza, multa
    /// nem km excedente — que já saem em linhas próprias, sem consultar proteção nenhuma.
    /// </summary>
    public sealed class ApuracaoDeProtecao
    {
        /// <summary>
        /// Diárias a cobrar da proteção. <b>Fracionária</b> quando há pró-rata — é para isso que a
        /// coluna <c>quantidade</c> de <c>tb_linha_fechamento</c> tem 4 casas.
        /// </summary>
        public decimal Diarias { get; private init; }

        public decimal ValorDiaria { get; private init; }

        /// <summary>
        /// A proteção cobriu o contrato inteiro? Quando sim, as diárias são <b>exatamente</b> as
        /// que o período cobrou (RN-18), sem passar pela conta proporcional: cobrar 2,9986 diárias
        /// de proteção num contrato de 3 diárias seria um centavo de diferença que ninguém explica.
        /// </summary>
        public bool CobriuOContratoInteiro { get; private init; }

        public DateTime InicioDaCobertura { get; private init; }
        public DateTime FimDaCobertura { get; private init; }

        public decimal Total => LinhaFechamento.Arredondar(Diarias * ValorDiaria);

        private ApuracaoDeProtecao() { }

        private static readonly CultureInfo Brasil = CultureInfo.GetCultureInfo("pt-BR");

        public static ApuracaoDeProtecao Calcular(
            DateTime dataInicioContrato,
            DateTime dataFimReal,
            DateTime dataContratacao,
            DateTime? dataCancelamento,
            decimal valorDiariaContratada,
            int diariasCobradasDoContrato)
        {
            if (dataFimReal < dataInicioContrato)
                throw new DomainException("A devolução não pode ser anterior à retirada");

            if (valorDiariaContratada <= 0)
                throw new DomainException("Proteção sem diária contratada não pode ser apurada");

            if (diariasCobradasDoContrato <= 0)
                throw new DomainException("A proteção só é apurada depois do período");

            // a cobertura nunca começa antes do contrato nem acaba depois dele: proteção
            // contratada antes da retirada não cobre o carro parado no pátio, e cancelada depois
            // da devolução não cobre o que já acabou
            var inicio = dataContratacao > dataInicioContrato ? dataContratacao : dataInicioContrato;
            var fim = dataCancelamento is { } cancelada && cancelada < dataFimReal
                ? cancelada
                : dataFimReal;

            // cancelamento antes mesmo de a cobertura começar: janela vazia, e não negativa
            if (fim < inicio)
                fim = inicio;

            var cobriuTudo = dataContratacao <= dataInicioContrato
                             && (dataCancelamento is null || dataCancelamento >= dataFimReal);

            var diarias = cobriuTudo
                ? diariasCobradasDoContrato
                // pró-rata: a fração de 24h que a cobertura durou, nunca acima do que o contrato
                // cobrou — a proteção não pode custar mais diárias que o próprio período
                : Math.Min(diariasCobradasDoContrato, ProRataEmDiarias(fim - inicio));

            return new ApuracaoDeProtecao
            {
                Diarias = diarias,
                ValorDiaria = valorDiariaContratada,
                CobriuOContratoInteiro = cobriuTudo,
                InicioDaCobertura = inicio,
                FimDaCobertura = fim
            };
        }

        /// <summary>
        /// Quatro casas, que é a escala da coluna <c>quantidade</c>. Mais que isso o banco truncaria
        /// e a linha gravada passaria a discordar do total que ela declara — o mesmo cuidado do
        /// valor unitário da hora excedente no A5.
        ///
        /// A divisão é sobre <c>Ticks</c> em <c>decimal</c>, e não sobre <c>TotalDays</c>, pela
        /// razão de sempre: <c>double</c> transforma 2 dias exatos em 1,9999999997.
        /// </summary>
        private static decimal ProRataEmDiarias(TimeSpan cobertura)
            => Math.Round(
                (decimal)cobertura.Ticks / TimeSpan.TicksPerDay,
                4,
                MidpointRounding.AwayFromZero);

        /// <summary>RN-31: o que sustenta a linha da proteção quando o cliente contesta.</summary>
        public string BaseCalculo()
            => CobriuOContratoInteiro
                ? string.Format(Brasil,
                    "proteção ativa durante todo o contrato: {0:0.####} diária(s) a {1:C}",
                    Diarias, ValorDiaria)
                : string.Format(Brasil,
                    "cobertura de {0:dd/MM/yyyy HH:mm} a {1:dd/MM/yyyy HH:mm} UTC: {2:0.####} diária(s) pró-rata a {3:C} (RN-19)",
                    InicioDaCobertura, FimDaCobertura, Diarias, ValorDiaria);
    }
}
