using System.Globalization;

namespace Locadora_Auto.Domain.Entidades
{
    /// <summary>
    /// RN-01 a RN-07: quanto o <b>tempo</b> do contrato custa. É o primeiro pedaço da conta e o
    /// mais contestado no balcão — o cliente aceita pagar km e combustível, mas discute diária.
    ///
    /// Puro de propósito: entra data, valor e política; sai o número. Não conhece
    /// <see cref="Locacao"/>, não conhece <see cref="Filial"/> e não escreve em lugar nenhum, o que
    /// deixa os quatro cenários gherkin do doc 07 §10 testáveis sem montar contrato nenhum.
    ///
    /// O que ele <b>não</b> faz: km, combustível, proteção, acessório, taxa, avaria e multa. Cada
    /// um tem sua RN e seu item de backlog (A6–A9).
    /// </summary>
    public sealed class ApuracaoDePeriodo
    {
        /// <summary>
        /// RN-02: ciclos de 24h cheios, com o mínimo de 1. Contrato de duas horas continua sendo
        /// uma diária — não existe meia diária em locadora.
        /// </summary>
        public int Diarias { get; private init; }

        /// <summary>
        /// RN-05: 0 ou 1. É a diária cheia que <b>substitui</b> as horas excedentes quando elas
        /// acumulam o valor de uma — nunca mais que uma, porque o resto do último ciclo é menor
        /// que 24h por construção.
        /// </summary>
        public int DiariasPorTeto { get; private init; }

        /// <summary>RN-04: horas iniciadas depois da tolerância. Zera quando o teto entra.</summary>
        public int HorasExcedentes { get; private init; }

        /// <summary>
        /// As horas que o cálculo encontrou, antes de o teto da RN-05 decidir se elas viram uma
        /// diária. Iguais a <see cref="HorasExcedentes"/> quando o teto não entra, e é ela que o
        /// extrato cita para explicar a substituição — "4 horas viraram 1 diária" só faz sentido
        /// se as 4 horas aparecerem em algum lugar.
        /// </summary>
        public int HorasApuradas { get; private init; }

        /// <summary>
        /// O total de diárias que a conta cobra. É este número — e não <see cref="Diarias"/> — que
        /// a franquia de km da RN-09 e a proteção da RN-18 multiplicam.
        /// </summary>
        public int DiariasCobradas => Diarias + DiariasPorTeto;

        /// <summary>RN-06: a diária congelada na abertura do contrato.</summary>
        public decimal ValorDiaria { get; private init; }

        /// <summary>
        /// RN-04: <c>ValorDiaria × PercentualHoraExcedente</c>, já a 2 casas.
        ///
        /// O arredondamento aqui não é enfeite: a coluna é <c>numeric(10,2)</c>, então um unitário
        /// com mais casas seria arredondado pelo banco e a linha gravada passaria a discordar do
        /// total que ela mesma declara. Com o padrão da casa é também o que faz a conta bater:
        /// 150 × 0,3333 = 49,995, que vira 50,00 e devolve o "1/3 da diária" que o contrato promete.
        /// </summary>
        public decimal ValorHoraExcedente { get; private init; }

        /// <summary>Sobra do último ciclo, antes de descontar a tolerância.</summary>
        public TimeSpan RestoDoUltimoCiclo { get; private init; }

        /// <summary>
        /// Quanto o tempo do contrato custa. A soma é parcela a parcela, na mesma divisão das
        /// linhas que <c>Locacao.ApurarPeriodo</c> escreve — RN-33: arredondamento por linha, nunca
        /// só no total. Com quantidade inteira e unitário a 2 casas nenhum arredondamento chega a
        /// acontecer aqui, mas somar diferente das linhas é o tipo de divergência de um centavo
        /// que ninguém consegue explicar depois.
        /// </summary>
        public decimal Total =>
            LinhaFechamento.Arredondar(Diarias * ValorDiaria)
            + LinhaFechamento.Arredondar(DiariasPorTeto * ValorDiaria)
            + LinhaFechamento.Arredondar(HorasExcedentes * ValorHoraExcedente);

        private ApuracaoDePeriodo() { }

        /// <summary>
        /// Cultura fixa nos textos de base de cálculo. A alternativa seria a do servidor, e aí a
        /// mesma cobrança sairia "R$ 49,99" numa máquina e "R$ 49.99" na outra — num registro que
        /// o doc 07 §11 manda preservar pelo prazo fiscal, imutável.
        /// </summary>
        private static readonly CultureInfo Brasil = CultureInfo.GetCultureInfo("pt-BR");

        public static ApuracaoDePeriodo Calcular(
            DateTime dataInicio,
            DateTime dataFimReal,
            decimal valorDiariaContratada,
            int toleranciaMinutos,
            decimal percentualHoraExcedente)
        {
            if (dataFimReal < dataInicio)
                throw new DomainException("A devolução não pode ser anterior à retirada");

            if (valorDiariaContratada <= 0)
                throw new DomainException("Contrato sem diária contratada não pode ser apurado");

            if (toleranciaMinutos < 0)
                throw new DomainException("Tolerância não pode ser negativa");

            if (percentualHoraExcedente <= 0 || percentualHoraExcedente > 1)
                throw new DomainException("Percentual de hora excedente deve estar entre 0 (exclusivo) e 1");

            var duracao = dataFimReal - dataInicio;

            // RN-01: a divisão é sobre ticks, não sobre TotalHours. `TotalHours` é double, e um
            // contrato de exatamente 48h pode sair como 47,999999999 — que viraria uma diária a
            // menos, na conta do cliente, sem ninguém entender por quê
            var ciclosCompletos = (int)(duracao.Ticks / TimeSpan.TicksPerDay);
            var resto = TimeSpan.FromTicks(duracao.Ticks % TimeSpan.TicksPerDay);

            // RN-02: mínimo de 1
            var diarias = Math.Max(1, ciclosCompletos);

            // quando o mínimo é que dá a diária, ela **cobre** o ciclo inteiro: um contrato de 22h
            // é uma diária e nada mais. Cobrar hora excedente sobre esse resto seria cobrar duas
            // vezes o mesmo período
            var restoCobravel = ciclosCompletos == 0 ? TimeSpan.Zero : resto;

            // RN-03 e RN-04: a tolerância é tempo livre, e as horas contam **a partir dela** — não
            // do fim do ciclo. É o que o cenário 3 do doc 07 §10 fixa: 2h30 de sobra com 30 min de
            // tolerância dão 2 horas excedentes, não 3
            var excedente = restoCobravel - TimeSpan.FromMinutes(toleranciaMinutos);

            var horas = excedente > TimeSpan.Zero
                // divisão inteira com arredondamento para cima: "por hora iniciada" (RN-04)
                ? (int)((excedente.Ticks + TimeSpan.TicksPerHour - 1) / TimeSpan.TicksPerHour)
                : 0;

            var valorHora = LinhaFechamento.Arredondar(valorDiariaContratada * percentualHoraExcedente);

            // RN-05: sem o teto a conta produz valor maior que prorrogar o contrato por um dia, e
            // isso é indefensável. "Atingir" inclui o empate — 3 horas que dão exatamente 1 diária
            // já viram a diária
            var tetoAtingido = horas > 0 && horas * valorHora >= valorDiariaContratada;

            return new ApuracaoDePeriodo
            {
                Diarias = diarias,
                DiariasPorTeto = tetoAtingido ? 1 : 0,
                HorasExcedentes = tetoAtingido ? 0 : horas,
                HorasApuradas = horas,
                ValorDiaria = valorDiariaContratada,
                ValorHoraExcedente = valorHora,
                RestoDoUltimoCiclo = restoCobravel
            };
        }

        // ---- textos de base de cálculo (RN-31) ----
        //
        // Ficam aqui, e não em quem lança a linha, porque são a explicação **deste** cálculo: quem
        // muda a regra tem o texto do lado, e a chance de a conta passar a dizer uma coisa e cobrar
        // outra cai para perto de zero.

        public string BaseCalculoDasDiarias(DateTime dataInicio, TimeSpan duracao)
            => Diarias == 1 && duracao < TimeSpan.FromDays(1)
                ? string.Format(Brasil,
                    "contrato de {0}; mínimo de 1 diária (RN-02)", Duracao(duracao))
                : string.Format(Brasil,
                    "{0} ciclo(s) de 24h a partir de {1:dd/MM/yyyy HH:mm} UTC",
                    Diarias, dataInicio);

        public string BaseCalculoDasHoras(int toleranciaMinutos)
            => string.Format(Brasil,
                "{0} além do último ciclo, menos {1} min de tolerância; cobrança por hora iniciada",
                Duracao(RestoDoUltimoCiclo), toleranciaMinutos);

        public string BaseCalculoDoTeto(int toleranciaMinutos)
            => string.Format(Brasil,
                "{0} além do último ciclo com {1} min de tolerância: {2} hora(s) a {3:C} atingiram o valor de 1 diária (teto da RN-05)",
                Duracao(RestoDoUltimoCiclo), toleranciaMinutos, HorasApuradas, ValorHoraExcedente);

        /// <summary>Duração legível para o extrato: "2h30", "45min", "3 dias 4h".</summary>
        private static string Duracao(TimeSpan tempo)
        {
            if (tempo < TimeSpan.FromHours(1))
                return string.Format(Brasil, "{0}min", (int)tempo.TotalMinutes);

            var horas = (int)tempo.TotalHours;
            return tempo.Minutes == 0
                ? string.Format(Brasil, "{0}h", horas)
                : string.Format(Brasil, "{0}h{1:00}", horas, tempo.Minutes);
        }
    }
}
