namespace Locadora_Auto.Domain.Entidades
{
    public class Locacao
    {
        public int IdLocacao { get; private set; }

        public int ClienteId { get; private set; }
        public int IdVeiculo { get; private set; }
        public int IdFuncionario { get; private set; }

        public int IdFilialRetirada { get; private set; }
        public int? IdFilialDevolucao { get; private set; }

        public DateTime DataInicio { get; private set; }
        public DateTime DataFimPrevista { get; private set; }
        public DateTime? DataFimReal { get; private set; }

        public int KmInicial { get; private set; }
        public int? KmFinal { get; private set; }

        public decimal ValorPrevisto { get; private set; }
        public decimal? ValorFinal { get; private set; }

        public StatusLocacao Status { get; private set; } = StatusLocacao.Pendente; // Pendente, Ativa, Finalizada, Cancelada

        public Clientes Cliente { get; private set; } = null!;
        public Veiculo Veiculo { get; private set; } = null!;
        public Funcionario Funcionario { get; private set; } = null!;
        public Filial FilialRetirada { get; private set; } = null!;
        public Filial? FilialDevolucao { get; private set; }

        public ICollection<Pagamento> Pagamentos { get; private set; } = new List<Pagamento>();
        public ICollection<Multa> Multas { get; private set; } = new List<Multa>();
        public ICollection<LocacaoSeguro> Seguros { get; private set; } = new List<LocacaoSeguro>();


        // ======================= CONSTRUTOR PRIVADO PARA EF =======================
        private Locacao() { }

        // ======================= FÁBRICA / CRIAÇÃO =======================
        public static Locacao Criar(
            Clientes cliente,
            Veiculo veiculo,
            Funcionario funcionario,
            int filialRetirada,
            DateTime dataInicio,
            DateTime dataFimPrevista,
            int kmInicial,
            decimal valorPrevisto)
        {
            if (veiculo == null)
                throw new ArgumentNullException(nameof(veiculo), "Veículo é obrigatório");

            if (cliente == null)
                throw new ArgumentNullException(nameof(cliente), "Cliente é obrigatório");

            if (dataFimPrevista <= dataInicio)
                throw new InvalidOperationException("Data fim prevista deve ser posterior à data de início");

            if (!veiculo.Disponivel)
                throw new InvalidOperationException("Veículo não está disponível para locação");

            var locacao = new Locacao
            {
                Cliente = cliente,
                Veiculo = veiculo,
                Funcionario = funcionario,
                ClienteId = cliente.IdCliente,
                IdVeiculo = veiculo.IdVeiculo,
                IdFuncionario = funcionario.IdFuncionario,
                IdFilialRetirada = filialRetirada,
                DataInicio = dataInicio,
                DataFimPrevista = dataFimPrevista,
                KmInicial = kmInicial,
                ValorPrevisto = valorPrevisto,
                Status = StatusLocacao.Ativa
            };

            // Marca veículo como indisponível
            veiculo.Disponivel = false;

            return locacao;
        }

        // ======================= MÉTODOS DE DOMÍNIO =======================

        public void Finalizar(DateTime dataFimReal, int kmFinal, decimal valorFinal, int filialDevolucao)
        {

            if (Status != StatusLocacao.Ativa)
                throw new InvalidOperationException("Somente locações ativas podem ser finalizadas");

            if (dataFimReal < DataInicio)
                throw new InvalidOperationException("Data de finalização não pode ser anterior à data de início");

            if (kmFinal < KmInicial)
                throw new InvalidOperationException("Quilometragem final não pode ser menor que a inicial");


            DataFimReal = dataFimReal;
            KmFinal = kmFinal;
            ValorFinal = valorFinal;
            IdFilialDevolucao = filialDevolucao;
            Status = StatusLocacao.Finalizada;

            // Libera veículo
            Veiculo.Disponivel = true;
        }

        public void AtualizarDados(DateTime dataFimPrevista, int kmInicial, decimal valorPrevisto)
        {
            if (Status != StatusLocacao.Pendente && Status != StatusLocacao.Ativa)
                throw new InvalidOperationException("Somente locações pendentes ou ativas podem ser atualizadas");

            if (dataFimPrevista <= DataInicio)
                throw new InvalidOperationException("Data fim prevista deve ser posterior à data de início");

            DataFimPrevista = dataFimPrevista;
            KmInicial = kmInicial;
            ValorPrevisto = valorPrevisto;
        }

        public void Cancelar()
        {
            if (Status != StatusLocacao.Ativa && Status != StatusLocacao.Pendente)
                throw new InvalidOperationException("Somente locações pendentes ou ativas podem ser canceladas");

            Status = StatusLocacao.Cancelada;

            // Libera veículo
            Veiculo.Disponivel = true;
        }

        public void AdicionarPagamento(Pagamento pagamento)
        {
            if (Status != StatusLocacao.Ativa)
                throw new InvalidOperationException("Pagamentos só podem ser adicionados a locações ativas");

            Pagamentos.Add(pagamento);
        }

        public void AdicionarMulta(Multa multa)
        {
            if (Status != StatusLocacao.Ativa)
                throw new InvalidOperationException("Multas só podem ser adicionadas a locações ativas");

            Multas.Add(multa);
        }

        public void AdicionarSeguro(LocacaoSeguro seguro)
        {
            Seguros.Add(seguro);
        }

        //TODO: isso é um job
        public void MarcarComoAtrasada(DateTime agora)
        {
            if (Status == StatusLocacao.Ativa && agora.Date > DataFimPrevista.Date)
            {
                Status = StatusLocacao.Atrasada;
            }
        }
    }

    public enum StatusLocacao
    {
        Pendente,
        Ativa,
        Cancelada,
        Atrasada,
        Finalizada
    }

}
