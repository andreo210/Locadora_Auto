using Locadora_Auto.Application.Configuration.Ultils.NotificadorServices;
using Locadora_Auto.Application.Services.FilialServices;
using Locadora_Auto.Application.Services.LocacaoServices;
using Locadora_Auto.Application.Services.VeiculoServices;
using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace Locadora_Auto.Tests.Fabricas
{
    /// <summary>
    /// Entidades válidas para os testes, com valores padrão que passam nas validações de
    /// <c>Criar</c>. O teste só informa o que é relevante para o caso que está verificando —
    /// assim, quando uma validação nova entra na entidade, corrige-se um lugar só.
    /// </summary>
    public static class Fabrica
    {
        /// <summary>Data segura para "futuro": as entidades validam contra <c>DateTime.UtcNow</c>.</summary>
        public static DateTime DaquiADias(int dias) => DateTime.UtcNow.AddDays(dias);

        public static Endereco Endereco(string cidade = "São Paulo")
            => Domain.Entidades.Endereco.Criar("Rua das Flores", "100", "Centro", cidade, "SP", "01001-000");

        public static Clientes Cliente(bool ativo = true, DateTime? validadeCnh = null)
        {
            var cliente = Clientes.Criar(
                numeroHabilitacao: "12345678900",
                validadeCnh: validadeCnh ?? DateTime.Today.AddYears(2),
                endereco: Endereco());

            if (!ativo) cliente.Desativar();

            return cliente;
        }

        /// <summary>
        /// <paramref name="limiteKm"/> nulo é a categoria de <b>quilometragem livre</b> da RN-08 —
        /// e aí o valor do km excedente não se aplica.
        /// </summary>
        public static CategoriaVeiculo Categoria(
            string nome = "Hatch",
            decimal valorDiaria = 150m,
            int? limiteKm = 200,
            decimal? valorKmExcedente = 2m)
            => CategoriaVeiculo.Criar(nome, valorDiaria, limiteKm, valorKmExcedente);

        /// <param name="tempoPreparacaoMinutos">
        /// <c>null</c> deixa o padrão da casa (<c>Filial.PreparacaoPadraoMinutos</c>). Informe só
        /// nos testes em que o tempo de preparação é o objeto da verificação.
        /// </param>
        public static Filial Filial(string nome = "Filial Centro", int? tempoPreparacaoMinutos = null)
            => Domain.Entidades.Filial.Criar(nome, "São Paulo", Endereco(), tempoPreparacaoMinutos);

        public static Veiculo Veiculo(int idCategoria = 1, int idFilial = 1, string placa = "ABC1D23")
            => Domain.Entidades.Veiculo.Criar(placa, "Fiat", "Argo", 2022, "9BWZZZ377VT004251", 15_000, idCategoria, idFilial);

        public static Adicional Adicional(string nome = "Cadeirinha", decimal valorDiaria = 25m)
            => Domain.Entidades.Adicional.Criar(nome, valorDiaria);

        public static Seguro Seguro(string nome = "Proteção Total")
            => Domain.Entidades.Seguro.Criar(nome, "Cobertura ampla", valorDiaria: 40m, franquia: 1500m, cobertura: "Colisão, roubo e terceiros");

        /// <summary>
        /// Contrata proteção com os mesmos números do <see cref="Seguro"/> desta fábrica. Existe
        /// porque a RN-18/RN-25 passou a exigir diária e franquia no ato — e o teste que só quer
        /// exercitar a guarda de "um seguro ativo por contrato" não tem por que repetir dois
        /// valores que não vai inspecionar.
        /// </summary>
        public static void ContratarSeguro(
            Locacao locacao, int idSeguro, decimal valorDiaria = 40m, decimal franquia = 1500m)
            => locacao.AdicionarSeguro(idSeguro, valorDiaria, franquia);

        public static Funcionario Funcionario(string matricula = "F-0001", string cargo = "Atendente")
            => Domain.Entidades.Funcionario.Criar(matricula, cargo);

        /// <summary>
        /// Locação já criada, com cliente apto, veículo disponível e funcionário — o mínimo que
        /// <c>Locacao.Criar</c> exige. O teste passa só a peça que ele precisa inspecionar depois
        /// (<c>Fabrica.Locacao(veiculo: veiculo)</c>) ou variar (as datas).
        ///
        /// <paramref name="dataFimPrevista"/> é derivada de <paramref name="dataInicio"/> e não de
        /// um <c>UtcNow</c> novo: <c>CalcularDias</c> arredonda horas para cima, então dois
        /// <c>UtcNow</c> diferentes fariam 72h virar 72,0001h — e o teste esperaria 3 e receberia 4.
        /// </summary>
        public static Locacao Locacao(
            Clientes? cliente = null,
            Veiculo? veiculo = null,
            Funcionario? funcionario = null,
            Reserva? reserva = null,
            DateTime? dataInicio = null,
            DateTime? dataFimPrevista = null,
            int kmInicial = 15_000,
            decimal valorPrevisto = 450m,
            int idFilialRetirada = 1,
            decimal valorDiariaContratada = 150m)
        {
            var inicio = dataInicio ?? DateTime.UtcNow;

            return Domain.Entidades.Locacao.Criar(
                cliente ?? Cliente(),
                veiculo ?? Veiculo(),
                funcionario ?? Funcionario(),
                reserva!,
                idFilialRetirada,
                inicio,
                dataFimPrevista ?? inicio.AddDays(3),
                kmInicial,
                valorPrevisto,
                valorDiariaContratada);
        }

        /// <summary>
        /// RN-57: registra a vistoria de retirada, que é o que promove o contrato a
        /// <c>EmAndamento</c> — o carro na rua. Quase todo teste que antes bastava
        /// <c>Fabrica.Locacao(...)</c> hoje precisa disto, porque só de <c>EmAndamento</c> em
        /// diante o contrato aceita devolução.
        /// </summary>
        public static Locacao Retirar(Locacao locacao, int idFuncionario = 1)
        {
            locacao.RegistrarVistoria(
                idFuncionario,
                TipoVistoria.Retirada,
                NivelCombustivel.Cheio,
                locacao.KmInicial,
                observacoes: null);

            return locacao;
        }

        /// <summary>Contrato com o carro já na rua: <c>Criar</c> seguido de <see cref="Retirar"/>.</summary>
        public static Locacao LocacaoEmAndamento(
            Clientes? cliente = null,
            Veiculo? veiculo = null,
            Funcionario? funcionario = null,
            Reserva? reserva = null,
            DateTime? dataInicio = null,
            DateTime? dataFimPrevista = null,
            int kmInicial = 15_000,
            decimal valorPrevisto = 450m,
            int idFilialRetirada = 1)
            => Retirar(Locacao(
                cliente, veiculo, funcionario, reserva,
                dataInicio, dataFimPrevista, kmInicial, valorPrevisto, idFilialRetirada));

        /// <summary>
        /// Leva o contrato até <c>Devolvida</c>: vistoria de devolução (RN-57 exige o par) e o
        /// registro da devolução em si. Para o contrato com a conta apurada, use
        /// <see cref="LocacaoFechada"/> — receber o carro não fecha mais o contrato (RN-58).
        /// </summary>
        public static Locacao Devolver(
            Locacao locacao,
            DateTime? dataFimReal = null,
            int kmFinal = 15_400,
            int filialDevolucao = 1,
            int idFuncionario = 1)
        {
            locacao.RegistrarVistoria(
                idFuncionario,
                TipoVistoria.Devolucao,
                NivelCombustivel.Meio,
                kmFinal,
                observacoes: null);

            locacao.RegistrarDevolucao(dataFimReal ?? locacao.DataFimPrevista, filialDevolucao);

            return locacao;
        }

        /// <summary>
        /// Contrato com o carro já recebido e a conta ainda por apurar — o ponto de partida de
        /// todo teste de fechamento (doc 07 §6, <c>Devolvida → Fechada</c>).
        /// </summary>
        public static Locacao LocacaoDevolvida(
            Veiculo? veiculo = null,
            decimal valorDiariaContratada = 150m,
            int kmFinal = 15_400,
            int filialDevolucao = 1)
            => Devolver(
                Retirar(Locacao(veiculo: veiculo, valorDiariaContratada: valorDiariaContratada)),
                kmFinal: kmFinal,
                filialDevolucao: filialDevolucao);

        /// <summary>
        /// Contrato do começo ao fim da conta. <paramref name="valorFinal"/> zero é o padrão de
        /// propósito: sem saldo a cobrar o contrato cai em <c>Finalizada</c>, que é o estado que a
        /// maioria dos testes de multa e de pós-contrato quer como ponto de partida. Com valor e
        /// sem pagamento, ele para em <c>ComSaldoResidual</c>.
        /// </summary>
        public static Locacao LocacaoFechada(
            Veiculo? veiculo = null,
            decimal valorFinal = 0m,
            DateTime? dataFimReal = null,
            int kmFinal = 15_400,
            int filialDevolucao = 1)
        {
            var locacao = Devolver(
                LocacaoEmAndamento(veiculo: veiculo),
                dataFimReal,
                kmFinal,
                filialDevolucao);

            locacao.Fechar(valorFinal);
            locacao.LiquidarSaldo();

            return locacao;
        }

        /// <summary>
        /// Contrato descartável, para os testes do <c>Veiculo</c> que precisam apenas satisfazer o
        /// documento de origem que a RN-37 exige nas transições. Ele nasce sobre um veículo
        /// próprio, e não sobre o que está sob teste — quem quer contrato e veículo casados usa
        /// <c>Fabrica.Locacao(veiculo: veiculo)</c>, que é o caminho real.
        /// </summary>
        public static Locacao Contrato() => Locacao();

        /// <summary>
        /// Cria uma reserva pela raiz do agregado (única porta de entrada, já que
        /// <c>Reserva.Criar</c> é internal) e devolve as duas pontas para o teste.
        /// </summary>
        public static (Clientes cliente, Reserva reserva) ClienteComReserva(
            int idFilial = 1,
            int idCategoria = 1,
            int idReserva = 1)
        {
            var cliente = Cliente();
            DefinirId(cliente, 1);

            cliente.ReservarVeiculo(cliente.IdCliente, DaquiADias(3), DaquiADias(6), idFilial, idCategoria);

            var reserva = cliente.Reservas.Single();
            DefinirId(reserva, idReserva);

            return (cliente, reserva);
        }

        /// <summary>
        /// Escreve na chave primária mesmo com set privado. Em teste isso é necessário porque o id
        /// normalmente viria do banco, e sem ele todo filtro por id casaria com a entidade errada.
        /// </summary>
        public static void DefinirId(object entidade, int id) => ChavePrimaria.Definir(entidade, id);

        /// <summary>
        /// Liga as navegações que a apuração do fechamento lê — veículo, categoria e as duas
        /// filiais.
        ///
        /// Em produção quem faz isso é o <c>Include</c> do EF; o <c>RepositorioFake</c> o ignora
        /// (Include só existe sobre provider do EF), e sem elas o serviço recusa a apuração dizendo
        /// que o contrato está sem filial. É a mesma escrita por reflexão que a
        /// <see cref="DefinirId"/> faz na chave primária, e pelo mesmo motivo: em memória não há
        /// EF para materializar o grafo.
        /// </summary>
        public static void LigarNavegacoesDoFechamento(
            Locacao locacao, Veiculo veiculo, CategoriaVeiculo categoria, Filial retirada, Filial devolucao)
        {
            veiculo.Categoria = categoria;

            // o nome vai como texto porque `Locacao` aqui dentro resolve para o método desta
            // fábrica, e não para o tipo — o mesmo motivo dos `Domain.Entidades.` espalhados acima
            Escrever(locacao, "FilialRetirada", retirada);
            Escrever(locacao, "FilialDevolucao", devolucao);
        }

        private static void Escrever(object entidade, string propriedade, object valor)
            => entidade.GetType()
                .GetProperty(propriedade)!
                .SetValue(entidade, valor);

        /// <summary>
        /// Põe a trilha do veículo (RN-37) no armazém como o <c>SaveChangesAsync</c> faria, para
        /// que o serviço possa consultá-la por <c>IdVeiculo</c>.
        ///
        /// O movimento do cadastro é o único que nasce com <c>IdVeiculo</c> zerado — naquele
        /// instante o veículo ainda não tinha id, e quem resolve a chave é a navegação, no mesmo
        /// insert. Em memória não há EF para fazer essa resolução, então ela é refeita aqui.
        ///
        /// Semeie o veículo <b>antes</b> das transições: é o id dele que os movimentos seguintes
        /// carimbam.
        /// </summary>
        public static void SemearTrilha(ArmazemFake armazem, Veiculo veiculo)
        {
            foreach (var movimento in veiculo.Movimentos.Where(m => m.IdVeiculo == 0))
                DefinirPropriedade(movimento, nameof(MovimentoVeiculo.IdVeiculo), veiculo.IdVeiculo);

            armazem.Semear(veiculo.Movimentos.ToArray());
        }

        /// <summary>
        /// Ancora o instante de um movimento da trilha.
        ///
        /// <c>DataMovimento</c> nasce de um <c>DateTime.UtcNow</c> dentro do domínio, que é o certo
        /// em produção e inútil no teste: sem reescrever o instante, uma trilha inteira cabe em
        /// alguns milissegundos e nenhum teste de duração distingue uma preparação de 6 horas de
        /// uma de 6 dias.
        /// </summary>
        public static void DatarMovimento(MovimentoVeiculo movimento, DateTime data)
            => DefinirPropriedade(movimento, nameof(MovimentoVeiculo.DataMovimento), data);

        /// <summary>
        /// <c>VeiculoService</c> montado sobre um armazém só, com todos os repositórios fake que
        /// ele exige.
        ///
        /// Existe porque o construtor do serviço cresce a cada regra nova do ativo — bloqueio
        /// trouxe o funcionário, desmobilização trouxe a locação — e sem isto cada regra nova
        /// obriga a editar meia dúzia de arquivos de teste que não têm nada a ver com ela.
        /// </summary>
        /// <param name="veiculos">
        /// O fake de veículo que o teste quer inspecionar depois — <c>Salvamentos</c> é contado por
        /// instância, então quem verifica gravação precisa entregar a sua. Os demais repositórios o
        /// teste não observa, e por isso nascem aqui.
        /// </param>
        public static VeiculoService VeiculoService(
            ArmazemFake armazem,
            INotificadorService notificador,
            VeiculosRepositoryFake? veiculos = null)
            => new(
                veiculos ?? new VeiculosRepositoryFake(armazem),
                new CategoriaVeiculosRepositoryFake(armazem),
                new FilialRepositoryFake(armazem),
                new MovimentoVeiculoRepositoryFake(armazem),
                new FuncionarioRepositoryFake(armazem),
                new LocacaoRepositoryFake(armazem),
                notificador);

        /// <summary>
        /// <c>LocacaoService</c> montado sobre um armazém só. Mesmo motivo do
        /// <see cref="VeiculoService"/>: o construtor cresce a cada regra nova.
        /// </summary>
        /// <param name="locacoes">
        /// O fake que o teste quer inspecionar depois (<c>Salvamentos</c> é contado por instância).
        /// </param>
        public static LocacaoService LocacaoService(
            ArmazemFake armazem,
            INotificadorService notificador,
            LocacaoRepositoryFake? locacoes = null,
            RecusaSobreposicaoRepositoryFake? recusas = null)
            => new(
                locacoes ?? new LocacaoRepositoryFake(armazem),
                new ClienteRepositoryFake(armazem),
                new VeiculosRepositoryFake(armazem),
                new ReservaRepositoryFake(armazem),
                new VistoriaRepositoryFake(armazem),
                new FilialRepositoryFake(armazem),
                new SeguroRepositoryFake(armazem),
                new AdicionalRepositoryFake(armazem),
                new LocacaoSeguroRepositoryFake(armazem),
                new FuncionarioRepositoryFake(armazem),
                new UploadDownloadFileServiceFake(),
                recusas ?? new RecusaSobreposicaoRepositoryFake(armazem),
                new CategoriaVeiculosRepositoryFake(armazem),
                notificador);

        /// <summary>
        /// <c>FilialService</c> montado sobre um armazém só. O logger é o <c>NullLogger</c>: o
        /// serviço loga, mas nada do que se testa aqui depende do que foi logado.
        /// </summary>
        public static FilialService FilialService(
            ArmazemFake armazem,
            INotificadorService notificador,
            FilialRepositoryFake? filiais = null)
            => new(
                new UploadDownloadFileServiceFake(),
                filiais ?? new FilialRepositoryFake(armazem),
                notificador,
                NullLogger<FilialService>.Instance);

        /// <summary>
        /// <c>IndicadoresFrotaService</c> montado sobre um armazém só. Mesmo motivo dos outros dois.
        /// </summary>
        public static IndicadoresFrotaService IndicadoresFrotaService(
            ArmazemFake armazem, INotificadorService notificador)
            => new(
                new VeiculosRepositoryFake(armazem),
                new MovimentoVeiculoRepositoryFake(armazem),
                new BloqueioVeiculoRepositoryFake(armazem),
                new RecusaSobreposicaoRepositoryFake(armazem),
                notificador);

        /// <summary>Escreve em propriedade de set privado — o que o EF e o relógio fazem em produção.</summary>
        private static void DefinirPropriedade(object entidade, string propriedade, object valor)
            => entidade.GetType()
                .GetProperty(propriedade)!
                .SetValue(entidade, valor);
    }
}
