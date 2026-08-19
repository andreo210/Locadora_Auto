using System.Globalization;

namespace Locadora_Auto.Domain.Entidades
{
    /// <summary>
    /// RN-08 a RN-11: quanto a <b>rodagem</b> do contrato custa.
    ///
    /// Puro como a <see cref="ApuracaoDePeriodo"/>: entra hodômetro, política da categoria e o
    /// número de diárias cobradas; sai o excedente. A franquia depende do período, e é por isso que
    /// as diárias entram como parâmetro — franquia é <b>km por diária</b>, não um teto fixo do
    /// contrato, então devolver antes reduz a franquia junto com a conta.
    /// </summary>
    public sealed class ApuracaoDeQuilometragem
    {
        /// <summary>Diferença entre os hodômetros das duas vistorias (RN-11).</summary>
        public int KmRodados { get; private init; }

        /// <summary>RN-09: <c>LimiteKm × diárias cobradas</c>. Zero quando a categoria é km livre.</summary>
        public int FranquiaKm { get; private init; }

        /// <summary>RN-10: o que passou da franquia. Nunca negativo — rodar menos não gera crédito.</summary>
        public int KmExcedentes { get; private init; }

        public decimal ValorKmExcedente { get; private init; }

        /// <summary>
        /// RN-08: <c>LimiteKm</c> nulo é km livre, e aí não há o que cobrar por mais que o carro
        /// tenha rodado. É o plano mais vendido no varejo, então não é exceção: é o caso comum.
        /// </summary>
        public bool KmLivre { get; private init; }

        public decimal Total => LinhaFechamento.Arredondar(KmExcedentes * ValorKmExcedente);

        private ApuracaoDeQuilometragem() { }

        private static readonly CultureInfo Brasil = CultureInfo.GetCultureInfo("pt-BR");

        public static ApuracaoDeQuilometragem Calcular(
            int kmInicial,
            int kmFinal,
            int? limiteKm,
            decimal? valorKmExcedente,
            int diariasCobradas)
        {
            // doc 07 §4: hodômetro adulterado ou erro de digitação. Bloqueia porque não há resposta
            // segura — cobrar zero esconderia a adulteração e cobrar o módulo inventaria rodagem
            if (kmFinal < kmInicial)
                throw new DomainException("Quilometragem final não pode ser menor que a inicial");

            if (diariasCobradas <= 0)
                throw new DomainException("A quilometragem só é apurada depois do período");

            var rodados = kmFinal - kmInicial;

            if (limiteKm is null)
                return new ApuracaoDeQuilometragem
                {
                    KmRodados = rodados,
                    KmLivre = true
                };

            // doc 07 §4: "categoria com LimiteKm preenchido e ValorKmExcedente nulo — bloqueia:
            // cadastro inconsistente". O zero entra junto: `CategoriaVeiculo.Criar` recusa limite
            // não positivo, então um zero aqui só pode ser dado velho ou carga malfeita, e tratá-lo
            // como "franquia zero" cobraria o contrato inteiro por km sem ninguém ter pedido
            if (limiteKm <= 0)
                throw new DomainException("Categoria com limite de km inválido: corrija o cadastro antes de apurar");

            if (valorKmExcedente is null or <= 0)
                throw new DomainException("Categoria com limite de km e sem valor de km excedente: cadastro inconsistente");

            var franquia = limiteKm.Value * diariasCobradas;

            return new ApuracaoDeQuilometragem
            {
                KmRodados = rodados,
                FranquiaKm = franquia,
                KmExcedentes = Math.Max(0, rodados - franquia),
                ValorKmExcedente = valorKmExcedente.Value
            };
        }

        /// <summary>RN-31: o que sustenta a linha quando o cliente contesta a rodagem.</summary>
        public string BaseCalculo(int kmInicial, int kmFinal, int? limiteKm, int diariasCobradas)
        {
            if (KmLivre)
                return string.Format(Brasil,
                    "categoria com quilometragem livre; {0} km rodados ({1} a {2}), sem cobrança",
                    KmRodados, kmInicial, kmFinal);

            var franquia = string.Format(Brasil,
                "franquia de {0} km ({1} km × {2} diária(s)); rodados {3} km ({4} a {5})",
                FranquiaKm, limiteKm, diariasCobradas, KmRodados, kmInicial, kmFinal);

            return KmExcedentes > 0
                ? string.Format(Brasil, "{0}: {1} km além da franquia", franquia, KmExcedentes)
                : string.Format(Brasil, "{0}: dentro da franquia", franquia);
        }
    }
}
