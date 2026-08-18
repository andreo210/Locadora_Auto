namespace Locadora_Auto.Domain.Entidades
{
    /// <summary>
    /// A conta apurada de um contrato (doc 07 §1: <c>DEVOLUÇÃO → FECHAMENTO → QUITAÇÃO</c>).
    /// Um por locação, com as linhas discriminadas da RN-31 e o saldo da RN-27.
    ///
    /// Existe porque <c>Locacao.ValorFinal</c> é um número e uma conta não é um número: sem as
    /// linhas não há como responder "por que R$ 1.240,00", não há extrato para o cliente, não há
    /// como medir de onde vem a receita acessória (seção 12) e não há como corrigir um erro sem
    /// apagar o que estava errado.
    ///
    /// <b>Ciclo:</b> <c>Abrir</c> → <c>Lancar</c> n vezes → <c>Selar</c> → (<c>RegistrarCorrecao</c>)*.
    /// A selagem é a fronteira: antes dela a apuração está em curso e linha nova é cálculo; depois
    /// dela a conta é histórico, e linha nova é <b>correção</b>, com autor e motivo.
    ///
    /// <b>Nada aqui calcula.</b> Quem apura período, km, combustível, proteção e taxas é o backlog
    /// A5–A10; esta entidade é o livro em que essa apuração escreve, e o que ela garante é a forma:
    /// linha imutável, valor arredondado, crédito que não vira débito, conta que não se reabre.
    /// </summary>
    public class FechamentoLocacao
    {
        public int IdFechamento { get; private set; }
        public int IdLocacao { get; private set; }

        /// <summary>Quando a apuração começou. UTC, como tudo que o cálculo do doc 07 §11 lê.</summary>
        public DateTime DataApuracao { get; private set; }

        /// <summary>
        /// Quem apurou. É funcionário, e não o autor da auditoria, pela mesma razão do
        /// <see cref="BloqueioVeiculo.IdFuncionarioResponsavel"/>: a auditoria responde "quem
        /// digitou" e o indicador de vazamento de receita da seção 12 quer "por atendente".
        /// </summary>
        public int IdFuncionarioApuracao { get; private set; }

        /// <summary>
        /// Quando a conta deixou de ser rascunho. Nula enquanto a apuração corre, e é ela que
        /// responde <see cref="Selado"/> — um <c>bool</c> diria que a conta está fechada, mas não
        /// desde quando, e a retenção fiscal do doc 07 §11 pergunta as duas coisas.
        /// </summary>
        public DateTime? DataSelagem { get; private set; }

        /// <summary>Soma das linhas que cobram.</summary>
        public decimal TotalDebitos { get; private set; }

        /// <summary>RN-28: soma das linhas que abatem — pagamento confirmado e isenção.</summary>
        public decimal TotalCreditos { get; private set; }

        /// <summary>
        /// RN-27 e RN-29: <c>TotalDebitos − TotalCreditos</c>. <b>Pode ser negativo</b>, e não
        /// trunca para zero: saldo negativo é crédito a devolver ao cliente, e truncar seria a
        /// casa ficando com dinheiro que não é dela.
        /// </summary>
        public decimal Saldo { get; private set; }

        private readonly List<LinhaFechamento> _linhas = new();
        public IReadOnlyCollection<LinhaFechamento> Linhas => _linhas;

        private FechamentoLocacao() { } // EF

        public bool Selado => DataSelagem != null;

        internal static FechamentoLocacao Abrir(int idLocacao, int idFuncionarioApuracao)
        {
            // comparação explícita, e não int.IsPositive: aquele devolve true para zero, que é o
            // caso de "não informou o atendente"
            if (idFuncionarioApuracao <= 0)
                throw new DomainException("A apuração exige o funcionário responsável");

            return new FechamentoLocacao
            {
                IdLocacao = idLocacao,
                IdFuncionarioApuracao = idFuncionarioApuracao,
                DataApuracao = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Escreve uma linha da apuração. Só antes da selagem — depois dela o caminho é
        /// <see cref="RegistrarCorrecao"/>, e a diferença entre os dois é o que a RN-31 protege.
        /// </summary>
        internal LinhaFechamento Lancar(
            TipoLinhaFechamento tipo,
            string baseCalculo,
            decimal quantidade,
            decimal valorUnitario,
            int? idFuncionarioLancamento = null,
            string? motivo = null)
        {
            if (Selado)
                throw new DomainException("Fechamento selado não recebe lançamento novo; use uma correção");

            var linha = LinhaFechamento.Lancar(
                tipo, baseCalculo, quantidade, valorUnitario,
                ehCorrecao: false,
                idFuncionarioLancamento: idFuncionarioLancamento,
                motivo: motivo);

            Acumular(linha);
            return linha;
        }

        /// <summary>
        /// Encerra a apuração. Daqui em diante a conta é histórico.
        ///
        /// Exige ao menos uma linha porque fechamento vazio não existe: a RN-02 garante o mínimo de
        /// uma diária em qualquer contrato, então um fechamento sem linha só pode ser apuração que
        /// não rodou — e selá-lo criaria um contrato "fechado" com conta em branco, que ninguém
        /// mais reabre.
        /// </summary>
        internal void Selar()
        {
            if (Selado)
                throw new DomainException("Fechamento já está selado");

            if (_linhas.Count == 0)
                throw new DomainException("Fechamento sem nenhuma linha não pode ser selado");

            DataSelagem = DateTime.UtcNow;
        }

        /// <summary>
        /// RN-31: a única forma de mexer numa conta selada. Não altera linha nenhuma — <b>acrescenta
        /// uma</b>, marcada como correção, com autor e motivo, e o saldo anda junto.
        ///
        /// O extrato passa a mostrar a conta original e o ajuste lado a lado, que é o ponto: quem
        /// contesta consegue ver o que foi cobrado, o que foi corrigido e por quem — e quem audita
        /// consegue ver que ninguém apagou nada.
        /// </summary>
        internal LinhaFechamento RegistrarCorrecao(
            TipoLinhaFechamento tipo,
            string baseCalculo,
            decimal quantidade,
            decimal valorUnitario,
            int idFuncionarioLancamento,
            string motivo)
        {
            if (!Selado)
                throw new DomainException("Só há o que corrigir depois que o fechamento é selado");

            var linha = LinhaFechamento.Lancar(
                tipo, baseCalculo, quantidade, valorUnitario,
                ehCorrecao: true,
                idFuncionarioLancamento: idFuncionarioLancamento,
                motivo: motivo);

            Acumular(linha);
            return linha;
        }

        /// <summary>
        /// Soma incremental, e não um <c>_linhas.Sum(...)</c>: as linhas só existem em memória se
        /// alguém pediu o <c>Include</c>, e um total recalculado sobre uma coleção parcialmente
        /// carregada sairia menor que o real — silenciosamente, e num campo de dinheiro.
        ///
        /// Andar de linha em linha é correto independentemente do que está carregado, porque a
        /// coleção é <b>append-only</b>: nenhuma linha muda de valor depois de criada (RN-31), e
        /// nenhuma é removida.
        /// </summary>
        private void Acumular(LinhaFechamento linha)
        {
            _linhas.Add(linha);

            if (linha.Natureza == NaturezaLinhaFechamento.Credito)
                TotalCreditos += linha.Total;
            else
                TotalDebitos += linha.Total;

            Saldo = TotalDebitos - TotalCreditos;
        }
    }
}
