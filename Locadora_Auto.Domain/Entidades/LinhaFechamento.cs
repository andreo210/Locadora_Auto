namespace Locadora_Auto.Domain.Entidades
{
    /// <summary>
    /// RN-31: uma linha discriminada da conta do cliente — <b>tipo, base de cálculo, quantidade,
    /// valor unitário e total</b>. É a unidade do extrato, e a razão de ela existir é simples:
    /// conta agregada é conta contestada. "R$ 1.240,00" não se defende no balcão; "3 diárias de
    /// R$ 150,00, 150 km excedentes a R$ 1,20, 24 litros a R$ 6,20" se defende.
    ///
    /// <b>É imutável.</b> Não há um único método que altere uma linha depois de criada — nem
    /// enquanto a apuração corre. Corrigir é lançar outra linha, com autor e motivo
    /// (<see cref="EhCorrecao"/>), e é isso que preserva o rastro: a conta original continua
    /// legível ao lado do que foi ajustado, que é exatamente o que uma edição silenciosa apagaria.
    ///
    /// <c>Lancar</c> é <c>internal</c> pela convenção do agregado: quem lança é
    /// <see cref="FechamentoLocacao"/>, que por sua vez só é alcançado pela
    /// <see cref="Locacao"/> — linha solta, sem fechamento, é linha que não entra em conta nenhuma.
    /// </summary>
    public class LinhaFechamento
    {
        public int IdLinhaFechamento { get; private set; }
        public int IdFechamento { get; private set; }

        public TipoLinhaFechamento Tipo { get; private set; }

        /// <summary>
        /// <b>Como</b> se chegou a este número, em texto: "franquia de 600 km sobre 3 diárias,
        /// rodados 750 km", "Cheio → Meio em tanque de 48 L", "devolução na filial 3, retirada na
        /// 1".
        ///
        /// É texto, e não um valor numérico, porque quem lê isto é o cliente com a chave na mão —
        /// e o que sustenta uma cobrança contestada é a medição declarada, não mais um número.
        /// Obrigatória de propósito: o doc 07 §9 fecha com "não faça em cenário nenhum: cobrar
        /// linha sem documento de suporte", e uma linha sem base de cálculo é exatamente isso.
        /// </summary>
        public string BaseCalculo { get; private set; } = null!;

        /// <summary>
        /// Quantas unidades: 3 diárias, 150 km, 24 litros, 1 taxa. Fracionária porque a RN-19
        /// cobra proteção <b>pró-rata</b> quando ela é cancelada no meio do contrato.
        /// </summary>
        public decimal Quantidade { get; private set; }

        public decimal ValorUnitario { get; private set; }

        /// <summary>
        /// RN-33: <c>Quantidade × ValorUnitario</c> arredondado a 2 casas com
        /// <see cref="MidpointRounding.AwayFromZero"/> — <b>por linha</b>, nunca só no total.
        ///
        /// Sempre positivo. O sinal da linha não mora aqui e sim no <see cref="Tipo"/>, via
        /// <see cref="Natureza"/>: débito soma, crédito abate. Guardar valor negativo faria a
        /// mesma informação existir em dois lugares — o sinal e o tipo — que é como um extrato
        /// passa a cobrar o que deveria devolver.
        /// </summary>
        public decimal Total { get; private set; }

        public DateTime DataLancamento { get; private set; }

        /// <summary>
        /// RN-31: lançada <b>depois</b> da selagem, para ajustar uma conta já apurada. Exige autor
        /// e motivo — é a fronteira entre erro corrigido e conta mexida.
        /// </summary>
        public bool EhCorrecao { get; private set; }

        /// <summary>
        /// RN-34: quem respondeu por esta linha. Nulo nas linhas que a apuração calculou sozinha —
        /// não há autor a registrar quando o autor é a regra.
        ///
        /// <b>Obrigatório</b> em correção e em isenção; <b>guardado sempre que informado</b>, e é
        /// isso que permite a alçada da RN-22 assinar uma taxa de one-way que a filial de destino
        /// não estava habilitada a receber.
        /// </summary>
        public int? IdFuncionarioLancamento { get; private set; }

        /// <summary>RN-34: por quê. Mesma obrigatoriedade do autor, e pela mesma razão.</summary>
        public string? Motivo { get; private set; }

        /// <summary>
        /// Se esta linha soma ou abate. Derivada do tipo, e não guardada em coluna, porque é
        /// propriedade <b>do tipo</b>: dois lugares dizendo o sinal é um lugar a mais para
        /// divergir, e um extrato com sinal divergente do tipo é indefensável.
        /// </summary>
        public NaturezaLinhaFechamento Natureza => NaturezaDe(Tipo);

        private LinhaFechamento() { } // EF

        internal static LinhaFechamento Lancar(
            TipoLinhaFechamento tipo,
            string baseCalculo,
            decimal quantidade,
            decimal valorUnitario,
            bool ehCorrecao = false,
            int? idFuncionarioLancamento = null,
            string? motivo = null)
        {
            if (!Enum.IsDefined(tipo))
                throw new DomainException("Tipo de linha de fechamento inválido");

            if (string.IsNullOrWhiteSpace(baseCalculo))
                throw new DomainException("Linha de fechamento exige a base de cálculo");

            // zero é válido nos dois: o doc 07 §10 pede a linha de km excedente em R$ 0,00 quando a
            // categoria é km livre, e essa linha vale mais que a ausência dela — diz ao cliente que
            // a quilometragem foi apurada e não gerou cobrança
            if (quantidade < 0)
                throw new DomainException("Quantidade da linha não pode ser negativa");

            if (valorUnitario < 0)
                throw new DomainException("Valor unitário da linha não pode ser negativo");

            // RN-34: autor e motivo. A isenção exige os dois sempre, correção ou não — ela é, por
            // definição, alguém decidindo não cobrar o que a regra apurou
            var exigeAutoria = ehCorrecao || tipo == TipoLinhaFechamento.Isencao;

            if (exigeAutoria)
            {
                // comparação explícita, e não int.IsPositive: aquele devolve true para zero, que é
                // justamente o "não informou" que aqui precisa ser recusado
                if (idFuncionarioLancamento is null or <= 0)
                    throw new DomainException("Correção e isenção exigem o funcionário responsável");

                if (string.IsNullOrWhiteSpace(motivo))
                    throw new DomainException("Correção e isenção exigem motivo");
            }

            return new LinhaFechamento
            {
                Tipo = tipo,
                BaseCalculo = baseCalculo.Trim(),
                Quantidade = quantidade,
                ValorUnitario = valorUnitario,
                Total = Arredondar(quantidade * valorUnitario),
                DataLancamento = DateTime.UtcNow,
                EhCorrecao = ehCorrecao,

                // guarda o autor sempre que ele vier, e não só quando é exigido: quem assina uma
                // linha por alçada (RN-22) não está corrigindo nem isentando nada, e descartar a
                // assinatura apagaria justamente a resposta que a auditoria vai pedir
                IdFuncionarioLancamento = idFuncionarioLancamento is > 0 ? idFuncionarioLancamento : null,
                Motivo = string.IsNullOrWhiteSpace(motivo) ? null : motivo.Trim()
            };
        }

        /// <summary>
        /// RN-33. Público porque a apuração (backlog A5–A10) vai arredondar valores intermediários
        /// com a mesma regra, e duas regras de arredondamento no mesmo cálculo produzem centavos
        /// que ninguém consegue explicar.
        ///
        /// <see cref="MidpointRounding.AwayFromZero"/>, e não o <c>ToEven</c> que é o padrão do
        /// .NET: o "arredondamento bancário" empurra metade dos meios centavos para baixo, e num
        /// extrato ao consumidor a conta tem que fechar do jeito que qualquer um refaz na
        /// calculadora.
        /// </summary>
        public static decimal Arredondar(decimal valor)
            => Math.Round(valor, 2, MidpointRounding.AwayFromZero);

        /// <summary>
        /// Os tipos que <b>abatem</b> a conta. Ficam listados aqui, e não espalhados num
        /// <c>switch</c>, porque somar um crédito como se fosse débito é o erro mais caro que este
        /// modelo permite — e assim ele é uma linha só, revisável de relance.
        /// </summary>
        private static readonly TipoLinhaFechamento[] Creditos =
        {
            TipoLinhaFechamento.PagamentoAbatido,
            TipoLinhaFechamento.Isencao,
            TipoLinhaFechamento.AbatimentoPorProtecao
        };

        public static NaturezaLinhaFechamento NaturezaDe(TipoLinhaFechamento tipo)
            => Creditos.Contains(tipo)
                ? NaturezaLinhaFechamento.Credito
                : NaturezaLinhaFechamento.Debito;
    }

    /// <summary>Se a linha soma ou abate no saldo do fechamento (RN-27).</summary>
    public enum NaturezaLinhaFechamento
    {
        Debito = 1,
        Credito = 2
    }

    /// <summary>
    /// O que cada linha do fechamento cobra ou abate. É a decomposição da RN-27, e a ordem dos
    /// valores segue a ordem em que a conta se lê: período, rodagem, consumo, o que foi contratado,
    /// taxas, o que deu errado e, por último, o que abate.
    ///
    /// Tipado, e não texto livre, porque a pergunta que o gestor faz é a do indicador da seção 12
    /// — "quanto da receita vem de acessório" — e sobre texto livre essa conta não sai.
    /// </summary>
    public enum TipoLinhaFechamento
    {
        /// <summary>RN-01/RN-02: ciclo de 24h a partir da retirada, mínimo de 1.</summary>
        Diaria = 1,

        /// <summary>RN-04: hora iniciada depois da tolerância, a uma fração da diária.</summary>
        HoraExcedente = 2,

        /// <summary>
        /// RN-05: a diária cheia que <b>substitui</b> as horas excedentes quando elas acumulam o
        /// valor de uma. Tipo próprio, e não mais uma <see cref="Diaria"/>, porque o extrato
        /// precisa contar essa história — o cliente devolveu com 4h de atraso e viu uma diária
        /// inteira; sem a linha dizer por quê, a cobrança parece arbitrária.
        /// </summary>
        DiariaPorTetoDeHoras = 3,

        /// <summary>RN-09/RN-10: o que passou de <c>LimiteKm × diárias cobradas</c>.</summary>
        KmExcedente = 4,

        /// <summary>RN-14: litros para repor o tanque no regime full-to-full.</summary>
        Combustivel = 5,

        /// <summary>RN-15: o serviço de abastecer, cobrado uma vez e só havendo litro a repor.</summary>
        TaxaServicoAbastecimento = 6,

        /// <summary>RN-18/RN-19: proteção pelas diárias cobradas, pró-rata se cancelada no meio.</summary>
        Protecao = 7,

        /// <summary>RN-17: acessório pelas diárias <b>efetivas</b>, não pelas previstas.</summary>
        Acessorio = 8,

        /// <summary>RN-21: devolução em filial diferente da de retirada.</summary>
        TaxaRetornoOneWay = 9,

        /// <summary>RN-23: valor fixo, só com registro na vistoria de devolução e ao menos uma foto.</summary>
        LimpezaEspecial = 10,

        /// <summary>RN-24/RN-25: avaria em <c>Aprovado</c> ou <c>Cobrado</c>, limitada à franquia.</summary>
        Avaria = 11,

        /// <summary>RN-26: multa conhecida até o fechamento. A que chega depois é pós-contrato.</summary>
        MultaTransito = 12,

        /// <summary>RN-28: pagamento em <c>Pago</c>. Pendente e falhado não abatem.</summary>
        PagamentoAbatido = 20,

        /// <summary>
        /// RN-25: o que a proteção contratada absorve das avarias, acima da franquia.
        ///
        /// Tipo próprio, e não <see cref="Isencao"/>, porque não é alguém decidindo não cobrar — é
        /// o produto que o cliente comprou funcionando. Sai em linha à parte para o extrato mostrar
        /// a proteção se pagando, que é o argumento de venda dela no contrato seguinte.
        /// </summary>
        AbatimentoPorProtecao = 22,

        /// <summary>
        /// RN-34: alguém decidiu não cobrar o que a regra apurou. Exige autor e motivo <b>sempre</b>
        /// — é o que separa cortesia registrada de receita que evaporou, e é o indicador
        /// "isenções por alçada" da seção 12.
        /// </summary>
        Isencao = 21
    }
}
