using System.Globalization;

namespace Locadora_Auto.Domain.Entidades
{
    /// <summary>
    /// RN-13 a RN-16: reposição do tanque no regime <b>full-to-full</b> — o cliente devolve como
    /// recebeu, e o que faltar a casa repõe e cobra.
    ///
    /// É a linha que mais gera atrito no balcão, e por isso a que mais precisa se explicar: o nível
    /// não é medido em litro e sim em fração do ponteiro, e é essa fração que a
    /// <see cref="Veiculo.CapacidadeTanqueLitros"/> transforma em litro e depois em dinheiro.
    ///
    /// Nada aqui bloqueia. Quando falta o dado — tanque não cadastrado, preço do litro não
    /// configurado — a apuração <b>não cobra e diz por quê</b>, na própria base de cálculo da
    /// linha: melhor perder a cobrança que emitir número inventado (doc 07 §4). A
    /// <see cref="Situacao"/> existe para quem chama decidir se além disso avisa alguém.
    /// </summary>
    public sealed class ApuracaoDeCombustivel
    {
        public SituacaoDoCombustivel Situacao { get; private init; }

        public NivelCombustivel NivelRetirada { get; private init; }
        public NivelCombustivel NivelDevolucao { get; private init; }

        /// <summary>Litros da capacidade total, quando o veículo a tem cadastrada.</summary>
        public decimal? CapacidadeTanqueLitros { get; private init; }

        /// <summary>
        /// RN-14: <c>(fraçãoRetirada − fraçãoDevolução) × capacidade</c>, <b>arredondado para
        /// cima</b> — o posto não vende fração de litro, e quem abastece paga o litro inteiro.
        ///
        /// Preenchido mesmo quando não há como cobrar (preço não configurado), porque o extrato
        /// dizer "24 L a repor, não cobrados" vale mais que uma linha em branco.
        /// </summary>
        public int LitrosFaltantes { get; private init; }

        public decimal PrecoLitro { get; private init; }
        public decimal TaxaServico { get; private init; }

        public bool Cobravel => Situacao == SituacaoDoCombustivel.Cobravel;

        /// <summary>Só o combustível, sem a taxa de serviço — são linhas diferentes no extrato.</summary>
        public decimal TotalDoCombustivel
            => Cobravel ? LinhaFechamento.Arredondar(LitrosFaltantes * PrecoLitro) : 0m;

        /// <summary>RN-15: a taxa é cobrada <b>uma vez</b>, e só quando há litro a repor.</summary>
        public decimal TotalDaTaxa => Cobravel ? TaxaServico : 0m;

        public decimal Total => TotalDoCombustivel + TotalDaTaxa;

        private ApuracaoDeCombustivel() { }

        private static readonly CultureInfo Brasil = CultureInfo.GetCultureInfo("pt-BR");

        public static ApuracaoDeCombustivel Calcular(
            NivelCombustivel nivelRetirada,
            NivelCombustivel nivelDevolucao,
            decimal? capacidadeTanqueLitros,
            decimal precoLitro,
            decimal taxaServico)
        {
            if (!Enum.IsDefined(nivelRetirada) || !Enum.IsDefined(nivelDevolucao))
                throw new DomainException("Nível de combustível inválido");

            if (precoLitro < 0 || taxaServico < 0)
                throw new DomainException("Preço do litro e taxa de serviço não podem ser negativos");

            var faltante = FracaoDe(nivelRetirada) - FracaoDe(nivelDevolucao);

            ApuracaoDeCombustivel Montar(SituacaoDoCombustivel situacao, int litros = 0) => new()
            {
                NivelRetirada = nivelRetirada,
                NivelDevolucao = nivelDevolucao,
                CapacidadeTanqueLitros = capacidadeTanqueLitros,
                PrecoLitro = precoLitro,
                TaxaServico = taxaServico,
                LitrosFaltantes = litros,
                Situacao = situacao
            };

            // RN-13 e RN-16: devolver no mesmo nível ou acima não cobra nada — e também não gera
            // crédito. Prática consolidada de mercado, e é a primeira coisa que se checa porque
            // dispensa tanque, preço e taxa
            if (faltante <= 0)
                return Montar(SituacaoDoCombustivel.SemDiferenca);

            // doc 07 §4: sem a capacidade não há como transformar fração em litro, e presumir um
            // tanque "médio" seria cobrar sobre número que ninguém conferiu
            if (capacidadeTanqueLitros is null or <= 0)
                return Montar(SituacaoDoCombustivel.TanqueNaoCadastrado);

            var litros = (int)Math.Ceiling(faltante * capacidadeTanqueLitros.Value);

            // o zero da filial significa "ninguém configurou", não "de graça" — é a decisão
            // registrada no backlog A3, e aqui ela vira uma linha que se explica
            return Montar(
                precoLitro <= 0
                    ? SituacaoDoCombustivel.PrecoNaoConfigurado
                    : SituacaoDoCombustivel.Cobravel,
                litros);
        }

        /// <summary>
        /// RN-14: a fração de tanque que cada ponto do ponteiro representa. Público porque o
        /// extrato e o teste falam nela, e porque é a tabela que traduz "meio tanque" em dinheiro.
        /// </summary>
        public static decimal FracaoDe(NivelCombustivel nivel) => nivel switch
        {
            NivelCombustivel.Vazio => 0m,
            NivelCombustivel.UmQuarto => 0.25m,
            NivelCombustivel.Meio => 0.5m,
            NivelCombustivel.TresQuartos => 0.75m,
            NivelCombustivel.Cheio => 1m,
            _ => throw new DomainException("Nível de combustível inválido")
        };

        public string BaseCalculoDoCombustivel() => Situacao switch
        {
            SituacaoDoCombustivel.SemDiferenca => string.Format(Brasil,
                "devolvido em {0} contra {1} na retirada; sem reposição e sem crédito (RN-16)",
                NivelDevolucao, NivelRetirada),

            SituacaoDoCombustivel.TanqueNaoCadastrado => string.Format(Brasil,
                "{0} → {1}, mas o veículo não tem capacidade de tanque cadastrada; sem cobrança",
                NivelRetirada, NivelDevolucao),

            SituacaoDoCombustivel.PrecoNaoConfigurado => string.Format(Brasil,
                "{0} → {1} em tanque de {2:0.##} L: {3} L a repor, mas a filial de devolução não tem preço do litro configurado; sem cobrança",
                NivelRetirada, NivelDevolucao, CapacidadeTanqueLitros, LitrosFaltantes),

            _ => string.Format(Brasil,
                "{0} → {1} em tanque de {2:0.##} L: {3} L a repor",
                NivelRetirada, NivelDevolucao, CapacidadeTanqueLitros, LitrosFaltantes)
        };

        public string BaseCalculoDaTaxa() => string.Format(Brasil,
            "serviço de abastecimento dos {0} L repostos; cobrada uma vez por contrato (RN-15)",
            LitrosFaltantes);
    }

    /// <summary>
    /// Por que o combustível foi ou não cobrado. Tipado porque quem chama precisa distinguir o que
    /// é <b>normal</b> — o cliente devolveu o tanque como pegou — do que é <b>cadastro faltando</b>,
    /// que não cobra mas alguém precisa saber.
    /// </summary>
    public enum SituacaoDoCombustivel
    {
        /// <summary>RN-13/RN-16: devolveu no mesmo nível ou acima. Nada a cobrar, nada a creditar.</summary>
        SemDiferenca = 1,

        /// <summary>Há litros a repor e há como cobrá-los.</summary>
        Cobravel = 2,

        /// <summary>
        /// RN-14: o veículo não tem <c>CapacidadeTanqueLitros</c>. Não cobra — e é o caso da frota
        /// cadastrada antes do campo existir, então tende a aparecer muito até o `F14` entrar.
        /// </summary>
        TanqueNaoCadastrado = 3,

        /// <summary>
        /// A filial de devolução está com <c>PrecoLitroCombustivel</c> zerado, que significa "não
        /// configurado" e não "de graça". Não cobra.
        /// </summary>
        PrecoNaoConfigurado = 4
    }
}
