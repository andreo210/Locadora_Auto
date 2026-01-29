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

        //subentidades
        private readonly List<Pagamento> _pagamentos = new();
        public IReadOnlyCollection<Pagamento> Pagamentos => _pagamentos;

        private readonly List<Caucao> _caucao = new();
        public IReadOnlyCollection<Caucao> Caucoes => _caucao;

        private readonly List<Multa> _multas = new();
        public IReadOnlyCollection<Multa> Multas => _multas;

        private readonly List<Dano> _danos = new();
        public IReadOnlyCollection<Dano> Danos => _danos;

        private readonly List<Vistoria> _vistorias = new();
        public IReadOnlyCollection<Vistoria> Vistorias => _vistorias;

        private readonly List<LocacaoSeguro> _seguros = new();
        public IReadOnlyCollection<LocacaoSeguro> Seguros => _seguros;

        private readonly List<LocacaoAdicional> _adicionais = new();
        public IReadOnlyCollection<LocacaoAdicional> Adicionais => _adicionais;


        private Locacao() { }


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
            if (!cliente.PodeLocar())
                throw new ArgumentNullException("Cliente não pode locar");

            if (!veiculo.Disponivel)
                throw new ArgumentNullException("Veículo indisponível");

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
                Status = StatusLocacao.Criada
            };

            // Marca veículo como indisponível
            veiculo.Indisponibilizar();

            return locacao;
        }

        // ======================= MÉTODOS DE DOMÍNIO =======================

        public void Finalizar(DateTime dataFimReal, int kmFinal, decimal valorFinal, int filialDevolucao)
        {

            if (Status != StatusLocacao.Criada)
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
            Veiculo.Disponibilizar();
        }


        public void Cancelar()
        {
            if (Status != StatusLocacao.Criada && Status != StatusLocacao.Pendente)
                throw new InvalidOperationException("Somente locações pendentes ou ativas podem ser canceladas");

            Status = StatusLocacao.Cancelada;

            // Libera veículo
            Veiculo.Disponibilizar();
        }

        //pagamentos
        public void AdicionarPagamento(decimal valor, FormaPagamento formaPagamento)
        {
            if (Status != StatusLocacao.Criada)
                throw new DomainException("Só é possível pagar locações criada");

            var pagamento = new Pagamento(valor, formaPagamento);

            _pagamentos.Add(pagamento);
        }
        public void RegistrarPagamento(decimal valor, FormaPagamento forma)
        {
            var pagamento = new Pagamento(valor, forma);
            _pagamentos.Add(pagamento);
        }

        //caucão
        public void DefinirCaucao(decimal valor)
        {
            if (Caucoes != null)
                throw new InvalidOperationException("Locação já possui caução");

            _caucao.Add(Caucao.Criar(valor));
        }

        //public void BloquearCaucao()
        //{
        //    if (Caucao == null)
        //        throw new InvalidOperationException("Locação não possui caução");

        //    Caucao.Bloquear();
        //}


        public void AdicionarMulta(Multa multa)
        {
            if (Status != StatusLocacao.Finalizada)
                throw new InvalidOperationException("Multa só pode ser aplicada após finalização");

            _multas.Add(multa);
        }

        public void RegistrarDano(Dano dano)
        {
            _danos.Add(dano);
        }

        public void RegistrarVistoria(Vistoria vistoria)
        {
            _vistorias.Add(vistoria);
        }



        public void AtualizarDados(DateTime dataFimPrevista, int kmInicial, decimal valorPrevisto)
        {
            if (Status != StatusLocacao.Pendente && Status != StatusLocacao.Criada)
                throw new InvalidOperationException("Somente locações pendentes ou ativas podem ser atualizadas");

            if (dataFimPrevista <= DataInicio)
                throw new InvalidOperationException("Data fim prevista deve ser posterior à data de início");

            DataFimPrevista = dataFimPrevista;
            KmInicial = kmInicial;
            ValorPrevisto = valorPrevisto;
        }


        //TODO: isso é um job
        public void MarcarComoAtrasada(DateTime agora)
        {
            if (Status == StatusLocacao.Criada && agora.Date > DataFimPrevista.Date)
            {
                Status = StatusLocacao.Atrasada;
            }
        }
    }

    public enum StatusLocacao
    {
        Pendente,
        Criada,
        Cancelada,
        Atrasada,
        Finalizada,
        EmAndamento
    }

}
