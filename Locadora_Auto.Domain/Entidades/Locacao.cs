using System.Linq.Expressions;
using System.Runtime.Intrinsics.X86;

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

        /// <summary>
        /// RN-06: preço de uma diária <b>neste</b> contrato, copiado de
        /// <c>CategoriaVeiculo.ValorDiaria</c> na abertura.
        ///
        /// É a diferença entre uma locadora que fecha o mês e uma que não fecha. Sem esta coluna a
        /// apuração leria a categoria no dia da devolução, e então reajustar a tabela — que é ato
        /// comercial rotineiro — reescreveria em silêncio o valor de todo contrato ainda aberto,
        /// inclusive os que já foram cobrados. Também é dela que saem a hora excedente (RN-04, uma
        /// fração desta diária) e o teto da RN-05.
        ///
        /// Não se confunde com <see cref="ValorPrevisto"/>, que é a estimativa do contrato inteiro
        /// no ato da venda: este é o preço unitário, e sobrevive à devolução antecipada ou atrasada
        /// justamente porque não embute quantidade.
        /// </summary>
        public decimal ValorDiariaContratada { get; private set; }

        /// <summary>
        /// Doc 07 §6. O contrato tem duas vidas, e o modelo antigo só tinha uma: a física
        /// (<c>Criada</c> → <c>EmAndamento</c> → <c>Devolvida</c>) e a financeira (<c>Fechada</c>
        /// → <c>Finalizada</c> ou <c>ComSaldoResidual</c>). Enquanto receber o carro gravava
        /// <c>Finalizada</c>, tudo o que a apuração cobraria — km excedente, combustível, limpeza,
        /// avaria, diária excedente — se perdia em silêncio, porque o contrato já constava
        /// concluído.
        /// </summary>
        public StatusLocacao Status { get; private set; } = StatusLocacao.Criada;

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

        private readonly List<Vistoria> _vistorias = new();
        public IReadOnlyCollection<Vistoria> Vistorias => _vistorias;

        private readonly List<LocacaoSeguro> _seguros = new();
        public IReadOnlyCollection<LocacaoSeguro> Seguros => _seguros;

        private readonly List<LocacaoAdicional> _adicionais = new();
        public IReadOnlyCollection<LocacaoAdicional> Adicionais => _adicionais;

        /// <summary>
        /// A conta discriminada do contrato (RN-31). Nula até a apuração ser aberta — contrato que
        /// ainda não voltou não tem o que apurar.
        ///
        /// Um por locação, garantido também por índice único no banco: apurar duas vezes não pode
        /// produzir duas contas (RN-32).
        /// </summary>
        public FechamentoLocacao? Fechamento { get; private set; }




        /// <summary>
        /// RN-61: status que soltam a placa. Enquanto a locação não estiver em um destes, ela
        /// continua ocupando o veículo no período dela (RN-40).
        ///
        /// São só dois, e por motivos diferentes. <c>Cancelada</c> porque o contrato foi anulado
        /// antes da retirada — o carro não rodou, e o período tem que voltar à oferta
        /// retroativamente. <c>Finalizada</c> porque o ciclo inteiro acabou. Os estados do meio
        /// (<c>Devolvida</c>, <c>Fechada</c>, <c>ComSaldoResidual</c>) ficam **de fora** de
        /// propósito: o carro rodou naquele período, e como <c>DataFimReal</c> já está gravada o
        /// intervalo encolheu para o que de fato aconteceu — não atrapalha contrato novo e ainda
        /// protege o histórico contra lançamento retroativo sobreposto.
        ///
        /// Esta lista é repetida pela constraint EXCLUDE no banco; ao mexer aqui, a migration
        /// correspondente precisa mudar junto, ou a garantia se desliga em silêncio. A coluna
        /// <c>status</c> de <c>tb_locacao</c> é <c>varchar(20)</c>
        /// (<c>HasConversion&lt;string&gt;</c>), então lá os valores vão entre aspas, não como int.
        /// <c>SobreposicaoDeContratoTests</c> é quem faz o esquecimento doer.
        /// </summary>
        public static readonly StatusLocacao[] StatusTerminais =
        {
            StatusLocacao.Finalizada,
            StatusLocacao.Cancelada
        };

        /// <summary>Carro na rua: o cliente está de posse do veículo.</summary>
        private static readonly StatusLocacao[] ComCarroNaRua =
        {
            StatusLocacao.EmAndamento,
            StatusLocacao.Atrasada
        };

        /// <summary>
        /// Contrato já apurado. Doc 07 §6: nenhuma linha do fechamento muda depois de
        /// <c>Fechada</c> — correção é lançamento novo, com autor e motivo (RN-31).
        /// </summary>
        private static readonly StatusLocacao[] AposApuracao =
        {
            StatusLocacao.Fechada,
            StatusLocacao.ComSaldoResidual,
            StatusLocacao.Finalizada
        };

        /// <summary>Carro já recebido — daqui em diante o que corre é a conta, não a posse.</summary>
        private static readonly StatusLocacao[] AposDevolucao =
        {
            StatusLocacao.Devolvida,
            StatusLocacao.Fechada,
            StatusLocacao.ComSaldoResidual,
            StatusLocacao.Finalizada
        };

        /// <summary>Contrato em montagem no balcão: aberto, e ainda alterável.</summary>
        private static readonly StatusLocacao[] EmMontagem =
        {
            StatusLocacao.Criada,
            StatusLocacao.EmAndamento
        };

        /// <summary>
        /// RN-40: contratos que ocupam <paramref name="idVeiculo"/> em algum instante de
        /// <c>[inicio, fim)</c>. Mora aqui, e não no serviço, porque é a regra em si — o serviço
        /// só a leva ao repositório, e o teste de tradução e a constraint do banco repetem a
        /// mesma forma.
        ///
        /// O intervalo é meio-aberto de propósito: contrato que começa no instante exato em que o
        /// outro termina é aceito, porque devolver e retirar no mesmo horário é operação normal de
        /// balcão. <c>DataFimReal ?? DataFimPrevista</c> vira <c>COALESCE</c> no SQL — enquanto o
        /// contrato está aberto vale a previsão; depois de fechado, o que de fato aconteceu.
        /// </summary>
        /// <param name="idLocacaoIgnorada">
        /// A própria locação, quando a checagem é de extensão (RN-42): sem isso ela colidiria
        /// consigo mesma. Zero não casa com nenhum id gerado pelo banco, então o padrão não ignora
        /// ninguém.
        /// </param>
        public static Expression<Func<Locacao, bool>> Sobrepostas(
            int idVeiculo,
            DateTime inicio,
            DateTime fim,
            int idLocacaoIgnorada = 0)
            => l => l.IdVeiculo == idVeiculo
                    && l.IdLocacao != idLocacaoIgnorada
                    && !StatusTerminais.Contains(l.Status)
                    && l.DataInicio < fim
                    && (l.DataFimReal ?? l.DataFimPrevista) > inicio;

        private Locacao() { }


        public static Locacao Criar(
            Clientes cliente,
            Veiculo veiculo,
            Funcionario funcionario,
            Reserva reserva,
            int filialRetirada,
            DateTime dataInicio,
            DateTime dataFimPrevista,
            int kmInicial,
            decimal valorPrevisto,
            decimal valorDiariaContratada)
        {
            if (veiculo == null)
                throw new ArgumentNullException(nameof(veiculo), "Veículo é obrigatório");

            if (cliente == null)
                throw new ArgumentNullException(nameof(cliente), "Cliente é obrigatório");

            if (!cliente.PodeLocar())
                throw new ArgumentNullException("Cliente não pode locar");

            if (dataFimPrevista <= dataInicio)
                throw new InvalidOperationException("Data fim prevista deve ser posterior à data de início");

            // RN-06: parâmetro explícito, e não `veiculo.Categoria.ValorDiaria` lido aqui dentro,
            // porque a navegação chega nula sempre que o chamador não pediu o Include — e aí o
            // contrato nasceria com diária zero, defeito que só apareceria no fechamento, semanas
            // depois. Exigir o número obriga quem abre o contrato a tê-lo em mãos.
            if (valorDiariaContratada <= 0)
                throw new InvalidOperationException("Valor da diária contratada deve ser maior que zero");

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
                ValorDiariaContratada = valorDiariaContratada,
                Status = StatusLocacao.Criada
            };

            // quem decide é o status do ativo, não o booleano `Disponivel` — e a mesma chamada que
            // valida é a que consome a placa. Contrato sobreposto no mesmo veículo não cabe aqui:
            // depende de consulta às outras locações, então é validação do serviço.
            //
            // Vem depois do contrato montado, e não antes, porque a RN-37 exige o documento de
            // origem do movimento: recusa continua recusando — o objeto acima é descartado sem ter
            // tocado em nada além de si mesmo.
            veiculo.Locar(locacao);

            if (reserva != null)
            {
                reserva.Finalizar();
            }

            return locacao;
        }

        // ======================= MÉTODOS DE DOMÍNIO =======================

        /// <summary>
        /// RN-58: receber o carro encerra a <b>posse</b>, não o contrato. O contrato morre no
        /// fechamento — daí este método parar em <c>Devolvida</c> e não em <c>Finalizada</c>.
        ///
        /// RN-57: exige o par de vistorias. Sem base comparável entre retirada e devolução não há
        /// cobrança de avaria, de quilometragem nem de combustível que sobreviva a uma
        /// contestação, e a vistoria de devolução é o que traz o hodômetro e o nível do tanque que
        /// a apuração vai usar.
        ///
        /// RN-11: <b>não recebe o hodômetro</b>. Ele sai da vistoria de devolução, que este método
        /// já exige — receber o número por fora criava dois registros do mesmo fato, sem nada
        /// garantindo que concordassem, e a apuração usa o da vistoria de qualquer forma.
        /// </summary>
        public void RegistrarDevolucao(DateTime dataFimReal, int filialDevolucao)
        {
            if (!ComCarroNaRua.Contains(Status))
                throw new InvalidOperationException("Somente locações com o veículo na rua podem ser devolvidas");

            if (!_vistorias.Any(v => v.Tipo == TipoVistoria.Retirada))
                throw new InvalidOperationException("Contrato sem vistoria de retirada não pode ser devolvido");

            if (!_vistorias.Any(v => v.Tipo == TipoVistoria.Devolucao))
                throw new InvalidOperationException("Registre a vistoria de devolução antes de encerrar a posse");

            if (dataFimReal < DataInicio)
                throw new InvalidOperationException("Data de finalização não pode ser anterior à data de início");

            var kmFinal = VistoriaDe(TipoVistoria.Devolucao).KmVeiculo;

            if (kmFinal < KmInicial)
                throw new InvalidOperationException("Quilometragem final não pode ser menor que a inicial");

            DataFimReal = dataFimReal;
            KmFinal = kmFinal;
            IdFilialDevolucao = filialDevolucao;
            Status = StatusLocacao.Devolvida;

            // o carro entra na fila do pátio, e o odômetro e a filial do ativo avançam com o que a
            // vistoria mediu — sem isso a frota fica registrada na filial errada
            Veiculo.RegistrarDevolucao(kmFinal, filialDevolucao, this);

            AbrirManutencaoPorAvaria();
        }

        /// <summary>
        /// Apuração da conta (doc 07 §6, <c>Devolvida → Fechada</c>).
        ///
        /// Enquanto o bloco de fechamento (RN-27 a RN-34) não existir, <paramref name="valorFinal"/>
        /// chega pronto de quem chama — é o mesmo número que a devolução recebia antes, agora com o
        /// nome certo e no passo certo do ciclo. Quando a apuração real entrar (backlog A5–A10),
        /// é este método que passa a <b>calcular</b> em vez de receber, e a assinatura muda aqui,
        /// não no ciclo de vida.
        /// </summary>
        public void Fechar(decimal valorFinal)
        {
            if (Status != StatusLocacao.Devolvida)
                throw new InvalidOperationException("Somente locações devolvidas podem ser fechadas");

            // contrato que tem conta discriminada fecha pela conta, não por número informado —
            // senão o `ValorFinal` e o saldo das linhas passariam a discordar sem ninguém notar
            if (Fechamento != null)
                throw new InvalidOperationException(
                    "Este contrato tem apuração aberta: feche por ela, não informando o valor");

            ValorFinal = valorFinal;
            Status = StatusLocacao.Fechada;
        }

        /// <summary>
        /// RN-28: só pagamento em <c>Pago</c> abate. Pendente e falhado não entram — dinheiro que
        /// ainda não caiu não quita contrato.
        ///
        /// RN-29: o resultado pode ser negativo, e não trunca para zero: saldo negativo é crédito
        /// a devolver ao cliente, não zero.
        /// </summary>
        public decimal SaldoEmAberto()
        {
            // com a conta discriminada no lugar, os pagamentos já entraram nela como crédito
            // (RN-28) e subtraí-los outra vez cobraria o cliente ao contrário. O que ainda falta
            // receber é o saldo apurado menos o que a caução quitou (RN-30)
            if (Fechamento is { Selado: true })
                return (ValorFinal ?? 0m) - _caucao.Sum(c => c.ValorConsumido);

            return (ValorFinal ?? 0m) - _pagamentos
                .Where(p => p.Status == StatusPagamento.Pago)
                .Sum(p => p.Valor);
        }

        /// <summary>
        /// Doc 07 §6: <c>Fechada → Finalizada</c> quando o saldo está quitado, <c>→
        /// ComSaldoResidual</c> quando sobra o que cobrar. Idempotente de propósito — é chamado a
        /// cada pagamento confirmado, e o contrato pode ir e voltar entre os dois enquanto o
        /// cliente acerta a conta.
        /// </summary>
        public void LiquidarSaldo()
        {
            if (!AposApuracao.Contains(Status))
                throw new InvalidOperationException("Só há saldo a liquidar depois da apuração");

            Status = SaldoEmAberto() > 0
                ? StatusLocacao.ComSaldoResidual
                : StatusLocacao.Finalizada;
        }

        #region Fechamento discriminado

        // As quatro portas do doc 07 §1 (FECHAMENTO). Nenhuma delas calcula: o que apura período,
        // km, combustível, proteção e taxas é o backlog A5–A10, e vai escrever por aqui.
        //
        // Elas também ainda **não** mexem em `Status` nem em `ValorFinal` — `Fechar(valorFinal)`
        // continua sendo a porta do ciclo de vida, exatamente como estava. Juntar as duas é o A10,
        // e lá vai ser preciso relaxar a guarda de `valorFinal < 0` do `Fechar`: a RN-29 permite
        // saldo negativo, que é crédito a devolver, e hoje ele seria recusado.

        /// <summary>
        /// Abre a apuração da conta. <b>Idempotente por desenho</b> (RN-32 e doc 07 §5): chamar de
        /// novo devolve o fechamento que já existe, selado ou não — retentativa de rede no balcão
        /// não pode produzir duas contas para o mesmo contrato.
        ///
        /// A guarda de estado só vale para o primeiro: apurar exige o carro já recebido (doc 07 §6,
        /// <c>Devolvida → Fechada</c>), e <c>Criada → Fechada</c> está na lista de transições
        /// proibidas.
        /// </summary>
        public FechamentoLocacao AbrirFechamento(int idFuncionarioApuracao)
        {
            if (Fechamento != null)
                return Fechamento;

            if (!AposDevolucao.Contains(Status))
                throw new InvalidOperationException("Só é possível apurar a conta depois da devolução do veículo");

            Fechamento = FechamentoLocacao.Abrir(IdLocacao, idFuncionarioApuracao);
            return Fechamento;
        }

        /// <summary>
        /// Escreve uma linha na apuração em curso (RN-31).
        /// </summary>
        /// <param name="idFuncionarioLancamento">
        /// Só para <see cref="TipoLinhaFechamento.Isencao"/>, que pela RN-34 nunca é anônima. As
        /// linhas que a regra calculou não têm autor a registrar.
        /// </param>
        public LinhaFechamento LancarNoFechamento(
            TipoLinhaFechamento tipo,
            string baseCalculo,
            decimal quantidade,
            decimal valorUnitario,
            int? idFuncionarioLancamento = null,
            string? motivo = null)
        {
            var fechamento = ContaAberta();

            return fechamento.Lancar(tipo, baseCalculo, quantidade, valorUnitario, idFuncionarioLancamento, motivo);
        }

        /// <summary>
        /// RN-01 a RN-07: apura o <b>tempo</b> do contrato e escreve as linhas correspondentes —
        /// diárias, horas excedentes e, quando o teto da RN-05 entra, a diária que as substitui.
        ///
        /// A política vem da filial de <b>retirada</b>, que é quem vendeu: tolerância e percentual
        /// de hora excedente são termo do contrato, não custo de execução (esse sai da filial de
        /// devolução, e é o A6/A8 que o lê).
        /// </summary>
        /// <param name="filialRetirada">
        /// A filial em objeto, e não os dois números soltos, por dois motivos: <c>FilialRetirada</c>
        /// chega nula em qualquer chamador que não peça o <c>Include</c>, e dois <c>decimal</c> na
        /// assinatura são dois argumentos que alguém troca de ordem um dia sem o compilador
        /// reclamar. A guarda de identidade abaixo é o que impede aplicar a política da praça errada.
        /// </param>
        public ApuracaoDePeriodo ApurarPeriodo(Filial filialRetirada)
        {
            ArgumentNullException.ThrowIfNull(filialRetirada);

            var fechamento = ContaAberta();

            if (filialRetirada.IdFilial != IdFilialRetirada)
                throw new InvalidOperationException("A filial informada não é a filial de retirada do contrato");

            var dataFimReal = DataFimReal
                ?? throw new InvalidOperationException("Contrato sem devolução registrada não tem período a apurar");

            // guarda do mesmo ciclo de apuração: relançar o período dobraria a conta do cliente.
            // Só enxerga as linhas carregadas — a idempotência que sobrevive a duas requisições é
            // a do A10; esta pega o caso real, que é a apuração ser chamada duas vezes no mesmo
            // fluxo entre `AbrirFechamento` e `SelarFechamento`
            if (fechamento.Linhas.Any(l => TiposDePeriodo.Contains(l.Tipo)))
                throw new DomainException("O período deste contrato já foi apurado");

            var apuracao = ApuracaoDePeriodo.Calcular(
                DataInicio,
                dataFimReal,
                ValorDiariaContratada,
                filialRetirada.ToleranciaMinutos,
                filialRetirada.PercentualHoraExcedente);

            fechamento.Lancar(
                TipoLinhaFechamento.Diaria,
                apuracao.BaseCalculoDasDiarias(DataInicio, dataFimReal - DataInicio),
                apuracao.Diarias,
                apuracao.ValorDiaria);

            if (apuracao.DiariasPorTeto > 0)
                fechamento.Lancar(
                    TipoLinhaFechamento.DiariaPorTetoDeHoras,
                    apuracao.BaseCalculoDoTeto(filialRetirada.ToleranciaMinutos),
                    apuracao.DiariasPorTeto,
                    apuracao.ValorDiaria);
            else if (apuracao.HorasExcedentes > 0)
                fechamento.Lancar(
                    TipoLinhaFechamento.HoraExcedente,
                    apuracao.BaseCalculoDasHoras(filialRetirada.ToleranciaMinutos),
                    apuracao.HorasExcedentes,
                    apuracao.ValorHoraExcedente);

            return apuracao;
        }

        /// <summary>
        /// As linhas que representam tempo. Existem para a guarda de reapuração acima saber o que
        /// procurar — e para o A6/A7, que multiplicam pelas diárias cobradas, saberem onde lê-las.
        /// </summary>
        public static readonly TipoLinhaFechamento[] TiposDePeriodo =
        {
            TipoLinhaFechamento.Diaria,
            TipoLinhaFechamento.DiariaPorTetoDeHoras,
            TipoLinhaFechamento.HoraExcedente
        };

        /// <summary>
        /// RN-08 a RN-11: apura a <b>rodagem</b> e escreve a linha de km excedente.
        ///
        /// A linha é escrita mesmo valendo R$ 0,00 — em km livre, ou dentro da franquia. É o que o
        /// doc 07 §10 pede, e o motivo é de balcão: a linha zerada diz ao cliente que a
        /// quilometragem foi apurada e não gerou cobrança, o que a ausência dela não diz.
        /// </summary>
        /// <param name="periodo">
        /// O resultado de <see cref="ApurarPeriodo"/>. A franquia é km <b>por diária cobrada</b>
        /// (RN-09), então ela encolhe junto com a conta na devolução antecipada — e é por isso que
        /// o número tem de ser o que de fato foi cobrado, não o previsto no contrato.
        /// </param>
        public ApuracaoDeQuilometragem ApurarQuilometragem(
            Veiculo veiculo, CategoriaVeiculo categoria, ApuracaoDePeriodo periodo)
        {
            ArgumentNullException.ThrowIfNull(veiculo);
            ArgumentNullException.ThrowIfNull(categoria);
            ArgumentNullException.ThrowIfNull(periodo);

            var fechamento = ContaAberta();

            if (veiculo.IdVeiculo != IdVeiculo)
                throw new InvalidOperationException("O veículo informado não é o veículo do contrato");

            if (categoria.Id != veiculo.IdCategoria)
                throw new InvalidOperationException("A categoria informada não é a categoria do veículo");

            if (fechamento.Linhas.Any(l => l.Tipo == TipoLinhaFechamento.KmExcedente))
                throw new DomainException("A quilometragem deste contrato já foi apurada");

            // RN-11: os dois hodômetros saem das vistorias, e nenhum é digitado no fechamento.
            // `KmInicial`/`KmFinal` do contrato guardam os mesmos números, mas quem os informou foi
            // quem abriu e quem recebeu — a medição que sustenta a cobrança é a da vistoria
            var kmInicial = VistoriaDe(TipoVistoria.Retirada).KmVeiculo;
            var kmFinal = VistoriaDe(TipoVistoria.Devolucao).KmVeiculo;

            var apuracao = ApuracaoDeQuilometragem.Calcular(
                kmInicial, kmFinal, categoria.LimiteKm, categoria.ValorKmExcedente, periodo.DiariasCobradas);

            fechamento.Lancar(
                TipoLinhaFechamento.KmExcedente,
                apuracao.BaseCalculo(kmInicial, kmFinal, categoria.LimiteKm, periodo.DiariasCobradas),
                apuracao.KmExcedentes,
                apuracao.ValorKmExcedente);

            return apuracao;
        }

        /// <summary>
        /// RN-13 a RN-16: apura o <b>combustível</b> no regime full-to-full e escreve a linha —
        /// mais a da taxa de serviço, quando há litro a repor (RN-15).
        ///
        /// A política sai da filial de <b>devolução</b>, e não da de retirada: quem paga o posto e
        /// põe alguém para abastecer é a praça que recebeu o carro. É o outro lado do recorte do
        /// A5, onde tolerância e hora excedente saem da filial que vendeu.
        ///
        /// Não bloqueia por falta de cadastro. Tanque não cadastrado ou preço do litro zerado
        /// produzem linha de R$ 0,00 explicando o motivo, e a <see cref="ApuracaoDeCombustivel.Situacao"/>
        /// devolvida é o que permite a quem chama avisar alguém.
        /// </summary>
        public ApuracaoDeCombustivel ApurarCombustivel(Veiculo veiculo, Filial filialDevolucao)
        {
            ArgumentNullException.ThrowIfNull(veiculo);
            ArgumentNullException.ThrowIfNull(filialDevolucao);

            var fechamento = ContaAberta();

            if (veiculo.IdVeiculo != IdVeiculo)
                throw new InvalidOperationException("O veículo informado não é o veículo do contrato");

            if (filialDevolucao.IdFilial != IdFilialDevolucao)
                throw new InvalidOperationException("A filial informada não é a filial de devolução do contrato");

            if (fechamento.Linhas.Any(l => l.Tipo == TipoLinhaFechamento.Combustivel))
                throw new DomainException("O combustível deste contrato já foi apurado");

            // RN-11 vale para o tanque pelo mesmo motivo do hodômetro: o nível é medição de
            // vistoria, feita com o carro à frente de quem assina
            var apuracao = ApuracaoDeCombustivel.Calcular(
                VistoriaDe(TipoVistoria.Retirada).Combustivel,
                VistoriaDe(TipoVistoria.Devolucao).Combustivel,
                veiculo.CapacidadeTanqueLitros,
                filialDevolucao.PrecoLitroCombustivel,
                filialDevolucao.TaxaServicoAbastecimento);

            fechamento.Lancar(
                TipoLinhaFechamento.Combustivel,
                apuracao.BaseCalculoDoCombustivel(),
                apuracao.Cobravel ? apuracao.LitrosFaltantes : 0,
                apuracao.Cobravel ? apuracao.PrecoLitro : 0m);

            // linha separada, e não somada ao combustível: são coisas diferentes na conta do
            // cliente — litro é insumo, taxa é serviço —, e o indicador de receita acessória do
            // doc 07 §12 só fecha se elas puderem ser contadas em separado
            if (apuracao.TotalDaTaxa > 0)
                fechamento.Lancar(
                    TipoLinhaFechamento.TaxaServicoAbastecimento,
                    apuracao.BaseCalculoDaTaxa(),
                    1m,
                    apuracao.TaxaServico);

            return apuracao;
        }

        /// <summary>
        /// RN-18 e RN-19: apura as <b>proteções</b> contratadas e escreve uma linha por proteção.
        ///
        /// Uma linha cada, e não uma soma, porque um contrato pode ter tido mais de uma ao longo da
        /// vida — cancelar libera contratar outra —, e cada uma cobriu uma janela diferente. Somar
        /// esconderia justamente o que a RN-19 mede.
        ///
        /// Devolve o total das proteções.
        /// </summary>
        public decimal ApurarProtecoes(ApuracaoDePeriodo periodo)
        {
            ArgumentNullException.ThrowIfNull(periodo);

            var fechamento = ContaAberta();

            var dataFimReal = DataFimReal
                ?? throw new InvalidOperationException("Contrato sem devolução registrada não tem proteção a apurar");

            if (fechamento.Linhas.Any(l => l.Tipo == TipoLinhaFechamento.Protecao))
                throw new DomainException("As proteções deste contrato já foram apuradas");

            var total = 0m;

            foreach (var protecao in _seguros)
            {
                var apuracao = ApuracaoDeProtecao.Calcular(
                    DataInicio,
                    dataFimReal,
                    protecao.DataContratacao,
                    protecao.DataCancelamento,
                    protecao.ValorDiariaContratada,
                    periodo.DiariasCobradas);

                fechamento.Lancar(
                    TipoLinhaFechamento.Protecao,
                    apuracao.BaseCalculo(),
                    apuracao.Diarias,
                    apuracao.ValorDiaria);

                total += apuracao.Total;
            }

            return total;
        }

        /// <summary>
        /// RN-17: apura os <b>acessórios</b> pelas diárias <b>efetivas</b>, uma linha por item.
        ///
        /// <c>LocacaoAdicional.Dias</c> foi congelado na inclusão com base na previsão, e por isso
        /// erra em toda devolução antecipada ou atrasada — quem devolve no segundo dia de um
        /// contrato de cinco pagaria cinco diárias de cadeirinha. Aqui a quantidade é recalculada,
        /// e o registro do adicional continua guardando o que foi <b>vendido</b>, que é outra
        /// pergunta.
        ///
        /// Devolve o total dos acessórios.
        /// </summary>
        public decimal ApurarAcessorios(ApuracaoDePeriodo periodo)
        {
            ArgumentNullException.ThrowIfNull(periodo);

            var fechamento = ContaAberta();

            if (fechamento.Linhas.Any(l => l.Tipo == TipoLinhaFechamento.Acessorio))
                throw new DomainException("Os acessórios deste contrato já foram apurados");

            var total = 0m;

            foreach (var acessorio in _adicionais)
            {
                // unidades × diárias cobradas: é assim que o extrato se lê — "2 cadeirinhas por 3
                // diárias, a R$ 20,00" —, e o unitário continua sendo o preço de uma diária de um
                var quantidade = acessorio.Quantidade * periodo.DiariasCobradas;

                var linha = fechamento.Lancar(
                    TipoLinhaFechamento.Acessorio,
                    BaseCalculoDoAcessorio(acessorio, periodo.DiariasCobradas),
                    quantidade,
                    acessorio.ValorDiariaContratada);

                total += linha.Total;
            }

            return total;
        }

        /// <summary>
        /// RN-21 e RN-22: apura a <b>taxa de retorno one-way</b> — devolver em filial diferente da
        /// de retirada. Devolve o valor cobrado, e zero quando não houve one-way.
        ///
        /// Não há linha quando a devolução foi na própria filial de retirada: é o caso da maioria
        /// dos contratos, e "taxa one-way: R$ 0,00" em todo extrato seria ruído.
        ///
        /// <b>Não é cálculo, é decisão</b> — daí não ter tipo de apuração próprio como o período ou
        /// o combustível: o valor sai pronto da filial de destino, e o que existe aqui é a guarda
        /// da RN-22.
        /// </summary>
        /// <param name="idFuncionarioAlcada">
        /// RN-22: filial não habilitada <b>bloqueia</b> o fechamento. A saída é a alçada do gerente,
        /// e ela vem assinada — o carro já está no pátio dela, então recusar para sempre não é
        /// opção, mas liberar sem quem responda também não.
        /// </param>
        public decimal ApurarTaxaOneWay(
            Filial filialDevolucao, int? idFuncionarioAlcada = null, string? motivoAlcada = null)
        {
            ArgumentNullException.ThrowIfNull(filialDevolucao);

            var fechamento = ContaAberta();

            if (filialDevolucao.IdFilial != IdFilialDevolucao)
                throw new InvalidOperationException("A filial informada não é a filial de devolução do contrato");

            if (fechamento.Linhas.Any(l => l.Tipo == TipoLinhaFechamento.TaxaRetornoOneWay))
                throw new DomainException("A taxa de one-way deste contrato já foi apurada");

            // RN-21: o gatilho é a filial de devolução diferir da de retirada, e não a distância
            // entre elas — duas lojas da mesma cidade também desfalcam uma e sobrecarregam a outra
            if (IdFilialDevolucao == IdFilialRetirada)
                return 0m;

            var comAlcada = idFuncionarioAlcada is > 0 && !string.IsNullOrWhiteSpace(motivoAlcada);

            if (!filialDevolucao.HabilitadaOneWay && !comAlcada)
                throw new DomainException(
                    "Filial de devolução não habilitada para one-way: o fechamento exige alçada com responsável e motivo");

            var baseCalculo = filialDevolucao.HabilitadaOneWay
                ? $"devolução na filial {IdFilialDevolucao}, retirada na filial {IdFilialRetirada}"
                : $"devolução na filial {IdFilialDevolucao}, retirada na filial {IdFilialRetirada}; " +
                  "filial de destino não habilitada para one-way, liberada por alçada (RN-22)";

            var linha = fechamento.Lancar(
                TipoLinhaFechamento.TaxaRetornoOneWay,
                baseCalculo,
                1m,
                filialDevolucao.TaxaRetornoOneWay,
                comAlcada ? idFuncionarioAlcada : null,
                comAlcada ? motivoAlcada : null);

            return linha.Total;
        }

        /// <summary>
        /// RN-23: apura a <b>limpeza especial</b> — valor fixo da filial de devolução, cobrado só
        /// com declaração na vistoria de devolução <b>e ao menos uma foto</b>.
        ///
        /// As duas condições juntas, e não uma ou outra: a declaração sozinha é a palavra do
        /// vistoriador contra a do cliente, e a foto sozinha não diz que a sujeira era especial.
        /// Sem as duas não há linha — sujeira comum é custo da operação.
        ///
        /// Devolve o valor cobrado.
        /// </summary>
        public decimal ApurarLimpezaEspecial(Filial filialDevolucao)
        {
            ArgumentNullException.ThrowIfNull(filialDevolucao);

            var fechamento = ContaAberta();

            if (filialDevolucao.IdFilial != IdFilialDevolucao)
                throw new InvalidOperationException("A filial informada não é a filial de devolução do contrato");

            if (fechamento.Linhas.Any(l => l.Tipo == TipoLinhaFechamento.LimpezaEspecial))
                throw new DomainException("A limpeza especial deste contrato já foi apurada");

            var vistoria = VistoriaDe(TipoVistoria.Devolucao);

            if (!vistoria.RequerLimpezaEspecial || vistoria.Fotos.Count == 0)
                return 0m;

            // o valor zerado da filial significa "não configurado", como o preço do litro do A6 —
            // não adianta lançar linha de R$ 0,00 para uma cobrança que ninguém parametrizou
            if (filialDevolucao.ValorLimpezaEspecial <= 0)
                return 0m;

            var linha = fechamento.Lancar(
                TipoLinhaFechamento.LimpezaEspecial,
                $"declarada na vistoria de devolução, com {vistoria.Fotos.Count} foto(s) de suporte (RN-23)",
                1m,
                filialDevolucao.ValorLimpezaEspecial);

            return linha.Total;
        }

        /// <summary>
        /// RN-24 e RN-25: apura as <b>avarias</b> e escreve uma linha por avaria cobrável, mais a
        /// linha de crédito da proteção quando a franquia absorve o excedente.
        ///
        /// Uma linha por avaria, e não uma soma, porque é assim que ela se defende: "risco na porta
        /// R$ 1.500, amassado no paralama R$ 1.800, proteção absorve R$ 1.300". Somar esconderia o
        /// que o cliente quer conferir item a item.
        ///
        /// Devolve a apuração inteira — inclusive o que ficou <b>em análise</b>, que não entra na
        /// conta (RN-24) mas precisa chegar a quem vai avisar o cliente do prazo.
        /// </summary>
        public ApuracaoDeAvarias ApurarAvarias()
        {
            var fechamento = ContaAberta();

            var dataFimReal = DataFimReal
                ?? throw new InvalidOperationException("Contrato sem devolução registrada não tem avaria a apurar");

            if (fechamento.Linhas.Any(l => l.Tipo == TipoLinhaFechamento.Avaria))
                throw new DomainException("As avarias deste contrato já foram apuradas");

            var danos = _vistorias.SelectMany(v => v.Danos).ToList();

            var apuracao = ApuracaoDeAvarias.Calcular(danos, FranquiaQueCobre(dataFimReal), dataFimReal);

            foreach (var dano in danos.Where(d => d.Status is StatusDano.Aprovado or StatusDano.Cobrado or StatusDano.Pago))
                fechamento.Lancar(
                    TipoLinhaFechamento.Avaria,
                    $"{dano.Tipo}: {dano.Descricao}",
                    1m,
                    dano.ValorEstimado);

            if (apuracao.AbatimentoPorProtecao > 0)
                fechamento.Lancar(
                    TipoLinhaFechamento.AbatimentoPorProtecao,
                    apuracao.BaseCalculoDoAbatimento(),
                    1m,
                    apuracao.AbatimentoPorProtecao);

            return apuracao;
        }

        /// <summary>
        /// RN-25: a franquia da proteção que cobria o carro <b>no momento da devolução</b> — que é
        /// quando a avaria é registrada, já que <c>Vistoria.RegistrarDano</c> só aceita vistoria de
        /// devolução.
        ///
        /// Não usa <c>Ativo</c>, e sim a janela do A7, porque os dois discordam nos dois extremos
        /// que importam: proteção cancelada na véspera da devolução continuaria <c>Ativo = false</c>
        /// mas não cobria mais (certo), e proteção cancelada <b>depois</b> de devolver também fica
        /// <c>Ativo = false</c> — e essa cobria. Sem a janela o cliente perderia a franquia por ter
        /// cancelado tarde demais.
        /// </summary>
        private decimal? FranquiaQueCobre(DateTime dataFimReal)
            => _seguros
                .Where(s => s.DataContratacao <= dataFimReal
                            && (s.DataCancelamento is null || s.DataCancelamento >= dataFimReal))
                .Select(s => (decimal?)s.FranquiaContratada)
                .FirstOrDefault();

        /// <summary>
        /// RN-26: apura as <b>multas</b> conhecidas até o fechamento. Uma linha por multa.
        ///
        /// Só entram as <c>Pendente</c>: paga já saiu do caixa do cliente por outro caminho, e
        /// compensada com caução é da composição (A10). Multa que chega depois do fechamento é
        /// pós-contrato e <b>não reabre</b> a conta — vira cobrança nova.
        ///
        /// Devolve o total cobrado e as multas que foram <b>deixadas de fora</b> por redundância,
        /// para quem chama poder avisar em vez de descobrir num extrato contestado.
        /// </summary>
        public (decimal Total, IReadOnlyList<Multa> Redundantes) ApurarMultas()
        {
            var fechamento = ContaAberta();

            if (fechamento.Linhas.Any(l => l.Tipo == TipoLinhaFechamento.MultaTransito))
                throw new DomainException("As multas deste contrato já foram apuradas");

            var pendentes = _multas.Where(m => m.Status == StatusMulta.Pendente).ToList();

            // `TipoMulta` é anterior ao fechamento: era o jeito manual de cobrar o que a apuração
            // agora calcula sozinha. Atraso virou hora excedente (RN-04), Limpeza virou limpeza
            // especial (RN-23) e DanoVeiculo virou avaria (RN-24) — cobrar os três de novo seria
            // faturar duas vezes o mesmo fato, e o cliente contesta e ganha.
            //
            // Não somem em silêncio: voltam na resposta para quem chama avisar.
            var redundantes = pendentes
                .Where(m => m.Tipo is TipoMulta.Atraso or TipoMulta.Limpeza or TipoMulta.DanoVeiculo)
                .ToList();

            var total = 0m;

            foreach (var multa in pendentes.Except(redundantes))
            {
                var linha = fechamento.Lancar(
                    TipoLinhaFechamento.MultaTransito,
                    $"multa {multa.Tipo} conhecida até o fechamento (RN-26)",
                    1m,
                    multa.Valor);

                total += linha.Total;
            }

            return (total, redundantes);
        }

        /// <summary>
        /// O nome do acessório sai da navegação quando ela está carregada, e o id serve de reserva:
        /// linha de extrato dizendo "adicional #7" é ruim, mas apurar errado por causa de um
        /// <c>Include</c> que ninguém pediu seria pior.
        /// </summary>
        private static string BaseCalculoDoAcessorio(LocacaoAdicional acessorio, int diariasCobradas)
        {
            var nome = string.IsNullOrWhiteSpace(acessorio.Adicional?.Nome)
                ? $"adicional #{acessorio.IdAdicional}"
                : acessorio.Adicional!.Nome;

            return $"{nome}: {acessorio.Quantidade} unidade(s) × {diariasCobradas} diária(s) efetiva(s) " +
                   $"(contratado para {acessorio.Dias})";
        }

        /// <summary>
        /// A última vistoria do tipo pedido. Lança quando não há — e é o certo: sem a medição das
        /// duas pontas não existe cobrança de km, combustível ou avaria que sobreviva a uma
        /// contestação (RN-57), e `RegistrarDevolucao` já exigiu o par.
        /// </summary>
        private Vistoria VistoriaDe(TipoVistoria tipo)
            => _vistorias.LastOrDefault(v => v.Tipo == tipo)
               ?? throw new InvalidOperationException($"Contrato sem vistoria de {tipo} não pode ser apurado");

        /// <summary>A conta aberta deste contrato, ou a recusa de quem tentou apurar sem abrir.</summary>
        private FechamentoLocacao ContaAberta()
            => Fechamento
               ?? throw new InvalidOperationException("A apuração não foi aberta para esta locação");

        /// <summary>
        /// RN-27 e RN-28: abate da conta os pagamentos <b>já confirmados</b>, um crédito por
        /// pagamento. Pendente e falhado não abatem — dinheiro que ainda não caiu não quita
        /// contrato.
        ///
        /// Devolve o total abatido.
        /// </summary>
        public decimal ApurarPagamentos()
        {
            var fechamento = ContaAberta();

            if (fechamento.Linhas.Any(l => l.Tipo == TipoLinhaFechamento.PagamentoAbatido))
                throw new DomainException("Os pagamentos deste contrato já foram abatidos");

            var total = 0m;

            foreach (var pagamento in _pagamentos.Where(p => p.Status == StatusPagamento.Pago))
            {
                var linha = fechamento.Lancar(
                    TipoLinhaFechamento.PagamentoAbatido,
                    $"pagamento em {pagamento.FormaPagamento} confirmado em {pagamento.DataPagamento:dd/MM/yyyy}",
                    1m,
                    pagamento.Valor);

                total += linha.Total;
            }

            return total;
        }

        /// <summary>
        /// Encerra a apuração e leva o contrato a <c>Fechada</c> (doc 07 §6). Daqui em diante a
        /// conta é histórico e só aceita correção.
        ///
        /// É aqui que os dois trilhos que o A4 deixou correndo em paralelo se juntam:
        /// <see cref="ValorFinal"/> passa a ser o saldo apurado, e não um número que alguém
        /// informou. Ele pode ser <b>negativo</b> (RN-29) — crédito a devolver ao cliente.
        ///
        /// Devolve o saldo.
        /// </summary>
        public decimal SelarFechamento()
        {
            var fechamento = ContaAberta();

            // a checagem de selagem vem antes da de status para a recusa dizer a coisa certa: um
            // contrato já fechado falha as duas, e "já está selado" explica, enquanto "só devolvida
            // pode ser fechada" confundiria quem acabou de fechá-lo
            if (fechamento.Selado)
                throw new DomainException("Fechamento já está selado");

            if (Status != StatusLocacao.Devolvida)
                throw new InvalidOperationException("Somente locações devolvidas podem ser fechadas");

            fechamento.Selar();

            ValorFinal = fechamento.Saldo;
            Status = StatusLocacao.Fechada;

            return fechamento.Saldo;
        }

        /// <summary>
        /// RN-30: resolve a caução <b>depois</b> de o saldo estar apurado — nunca antes, que é
        /// transição proibida no doc 07 §6. Saldo coberto consome o necessário e o restante volta;
        /// saldo maior que a garantia consome tudo e o que sobra vira cobrança residual.
        ///
        /// Devolve quanto foi consumido no total.
        /// </summary>
        public decimal ResolverCaucao()
        {
            var fechamento = Fechamento
                ?? throw new InvalidOperationException("A apuração não foi aberta para esta locação");

            if (!fechamento.Selado)
                throw new DomainException("A caução só é resolvida depois de o fechamento ser selado");

            // RN-29: saldo negativo não consome caução nenhuma. O cliente pagou mais do que a conta
            // deu, e a garantia volta inteira — reter qualquer parte dela seria a casa segurando
            // dinheiro de quem já não deve nada
            var aConsumir = Math.Max(0m, fechamento.Saldo);
            var consumido = 0m;

            foreach (var caucao in _caucao.Where(c => c.Status != Caucao.StatusCaucao.Devolvida))
            {
                if (aConsumir <= 0)
                {
                    // nada a consumir desta: volta inteira
                    if (caucao.ValorConsumido == 0)
                        caucao.Devolver();
                    continue;
                }

                var parcela = Math.Min(caucao.ValorDisponivel, aConsumir);
                if (parcela <= 0)
                    continue;

                caucao.Consumir(parcela);
                consumido += parcela;
                aConsumir -= parcela;
            }

            return consumido;
        }

        /// <summary>
        /// A apuração inteira, na ordem em que as regras dependem umas das outras: período primeiro
        /// (a franquia de km e a proteção multiplicam as diárias cobradas), o resto depois, os
        /// pagamentos por último — porque RN-27 abate pagamento do total, não o contrário.
        ///
        /// <b>Idempotente</b> (RN-32): chamada de novo sobre um contrato já fechado devolve a mesma
        /// conta, sem criar linha nem consumir caução outra vez. É o que separa uma retentativa de
        /// rede de uma cobrança em dobro.
        ///
        /// Devolve a conta e o que não cabe nela: a avaria em análise, as multas recusadas por
        /// redundância e o saldo residual. Nada some em silêncio.
        /// </summary>
        public ResultadoDaApuracao ApurarFechamento(
            Veiculo veiculo,
            CategoriaVeiculo categoria,
            Filial filialRetirada,
            Filial filialDevolucao,
            int idFuncionarioApuracao,
            int? idFuncionarioAlcada = null,
            string? motivoAlcada = null)
        {
            // RN-32: a conta já selada é a resposta. Não reabre, não recalcula, não cobra de novo
            if (Fechamento is { Selado: true } selada)
                return new ResultadoDaApuracao
                {
                    Fechamento = selada,
                    CaucaoConsumida = _caucao.Sum(c => c.ValorConsumido),
                    JaEstavaApurado = true
                };

            AbrirFechamento(idFuncionarioApuracao);

            var periodo = ApurarPeriodo(filialRetirada);
            ApurarQuilometragem(veiculo, categoria, periodo);
            var combustivel = ApurarCombustivel(veiculo, filialDevolucao);
            ApurarProtecoes(periodo);
            ApurarAcessorios(periodo);
            ApurarTaxaOneWay(filialDevolucao, idFuncionarioAlcada, motivoAlcada);
            ApurarLimpezaEspecial(filialDevolucao);

            var avarias = ApurarAvarias();
            var (_, multasRecusadas) = ApurarMultas();

            // por último, porque a RN-27 abate pagamento do total — e o total só existe depois de
            // todas as linhas de cobrança estarem lançadas
            ApurarPagamentos();

            SelarFechamento();

            var caucaoConsumida = ResolverCaucao();

            // doc 07 §6: `Fechada → Finalizada` quando não sobra nada, `→ ComSaldoResidual` quando
            // sobra. Mora aqui e não em quem chama porque é o último passo do mesmo ato — deixá-lo
            // de fora produziria contrato `Fechada` que nunca sai de lá
            LiquidarSaldo();

            return new ResultadoDaApuracao
            {
                Fechamento = Fechamento!,
                Avarias = avarias,
                Combustivel = combustivel,
                MultasRecusadas = multasRecusadas,
                CaucaoConsumida = caucaoConsumida
            };
        }

        /// <summary>
        /// RN-31: ajusta uma conta já selada acrescentando uma linha, com autor e motivo. Nenhuma
        /// linha existente é tocada — reabrir fechamento é transição proibida (doc 07 §6).
        /// </summary>
        public LinhaFechamento CorrigirFechamento(
            TipoLinhaFechamento tipo,
            string baseCalculo,
            decimal quantidade,
            decimal valorUnitario,
            int idFuncionarioLancamento,
            string motivo)
        {
            var fechamento = ContaAberta();

            return fechamento.RegistrarCorrecao(
                tipo, baseCalculo, quantidade, valorUnitario, idFuncionarioLancamento, motivo);
        }

        #endregion Fechamento discriminado

        /// <summary>
        /// Avaria vira ordem corretiva no fechamento, não no ato do registro da vistoria: com o
        /// contrato aberto o veículo está locado e não pode entrar em oficina.
        /// </summary>
        private void AbrirManutencaoPorAvaria()
        {
            var houveAvaria = _vistorias
                .Where(v => v.Tipo == TipoVistoria.Devolucao)
                .Any(v => v.PossuiDanos());

            if (!houveAvaria)
                return;

            Veiculo.IniciarManutencao(
                TipoManutencao.Corretiva,
                "Manutenção gerada automaticamente por dano em vistoria");
        }


        /// <summary>
        /// RN-59: cancelar é anular um contrato que nunca chegou a valer. Só cabe em
        /// <c>Criada</c> — com o carro na rua a saída é registrar a devolução, não cancelar.
        /// Cancelamento depois da retirada apagaria o contrato e o quilômetro rodado junto, que é
        /// a porta de fraude mais óbvia do ciclo.
        /// </summary>
        public void Cancelar()
        {
            if (Status != StatusLocacao.Criada)
                throw new InvalidOperationException(
                    "Somente contrato ainda não retirado pode ser cancelado; com o veículo na rua, registre a devolução");

            Status = StatusLocacao.Cancelada;

            // contrato anulado: o carro não rodou, então volta à oferta sem passar pela preparação
            Veiculo.ReverterLocacao(this);
        }

        #region pagamento
        /// <summary>
        /// Pagamento cabe em qualquer ponto vivo do contrato: pré-pagamento na abertura, acerto no
        /// balcão depois da apuração, quitação do saldo residual. O teto só existe depois que há
        /// conta apurada — antes disso <c>ValorFinal</c> é nulo, e a comparação antiga
        /// (<c>valor &gt; ValorFinal</c>) era silenciosamente falsa contra nulo, ou seja, não
        /// existia.
        /// </summary>
        public void AdicionarPagamento(decimal valor, FormaPagamento formaPagamento)
        {
            if (StatusTerminais.Contains(Status))
                throw new DomainException("Locação encerrada não recebe pagamento");

            if (ValorFinal.HasValue && valor > SaldoEmAberto())
                throw new DomainException("Valor excede o saldo da locação");

            var pagamento = new Pagamento(valor, formaPagamento);
            _pagamentos.Add(pagamento);
        }

        public void ConfirmarPagamento(int idPagamento)
        {
            var pagamento = _pagamentos.FirstOrDefault(p=>p.IdPagamento ==idPagamento);
            if (pagamento == null)
                throw new DomainException("Pagamento não encontrado");

            pagamento.Confirmar();

            // é o dinheiro entrando que move o contrato apurado para o fim dele — antes da
            // apuração não há saldo contra o que comparar
            if (AposApuracao.Contains(Status))
                LiquidarSaldo();
        }

        public void CancelarPagamento(int idPagamento, string motivo)
        {
            var pagamento = _pagamentos.FirstOrDefault(p => p.IdPagamento == idPagamento);
            pagamento.Cancelar(motivo);
        }

        public void MarcarComoFalha(int idPagamento)
        {
            var pagamento = _pagamentos.FirstOrDefault(p => p.IdPagamento == idPagamento);
            pagamento.MarcarComoFalhou();
        }
        #endregion pagamento

        #region caucao
        public void RegistrarCaucao(decimal valor)
        {
            _caucao.Add(Caucao.Criar(valor));
        }

        public void BloquearCaucao(int idCaucao)
        {
            if (_caucao.Count == 0)
                throw new InvalidOperationException("Locação não possui caução");

            var Caucao = _caucao.FirstOrDefault(c => c.IdCaucao == idCaucao);
            Caucao.Bloquear();
        }

        public void DeduzirCaucao(int idCaucao,decimal valor)
        {
            if (_caucao.Count == 0)
                throw new InvalidOperationException("Locação não possui caução");

            var Caucao = _caucao.FirstOrDefault(c => c.IdCaucao == idCaucao);
            Caucao.Consumir(valor);
        }

        /// <summary>
        /// Doc 07 §6, transição proibida: liberar caução antes de <c>Fechada</c>. Caução é
        /// garantia, e devolvê-la antes de apurar a conta é abrir mão da garantia justamente no
        /// momento em que ela serve para alguma coisa.
        ///
        /// Devolve a caução que <b>não foi tocada</b> pelo fechamento. A que foi consumida em parte
        /// permanece <c>Utilizada</c>, e o estorno do restante é fato financeiro — é o que o doc 07
        /// §10 fixa. Quem resolve isso na apuração é <see cref="ResolverCaucao"/>; este método é a
        /// porta manual, para a caução dispensada por alçada.
        /// </summary>
        public void DevolverCaucao(int idCaucao)
        {
            if (!AposApuracao.Contains(Status))
                throw new InvalidOperationException("A caução só é resolvida depois da apuração do fechamento");

            if (_caucao.Count == 0)
                throw new InvalidOperationException("Locação não possui caução");

            var Caucao = _caucao.FirstOrDefault(c => c.IdCaucao == idCaucao);
            Caucao.Devolver();
        }
        #endregion caucao

        #region multa
        // Adicionar multa
        public void AdicionarMulta(TipoMulta tipo, decimal valor)
        {
            if (valor <= 0)
                throw new DomainException("Valor da multa deve ser maior que zero");

            // RN-26 e invariante do pós-contrato: multa nasce da devolução em diante, inclusive
            // depois de finalizada — multa de trânsito chega semanas depois e não reabre a conta
            if (!AposDevolucao.Contains(Status))
                throw new DomainException("Multa só pode ser gerada após a devolução");

            var multa = Multa.Criar(valor,tipo);
            _multas.Add(multa);
        }

        // Pagar multa
        public void PagarMulta(int idMulta)
        {
            var multa = _multas.FirstOrDefault(m => m.IdMulta == idMulta);
            if (multa == null)
                throw new DomainException("Multa não encontrada");

            multa.MarcarComoPaga();
        }

        // Compensar multa com caução
        public void CompensarMultaComCaucao(int idMulta)
        {
            var multa = _multas.FirstOrDefault(m => m.IdMulta == idMulta);
            if (multa == null)
                throw new DomainException("Multa não encontrada");

            if (_caucao.Sum(c => c.ValorDisponivel) < multa.Valor)
                throw new DomainException("Caução insuficiente para compensar a multa");

            // Deduz o valor da multa das cauções, usando múltiplas se necessário.
            //
            // O decremento do restante estava comentado, então o laço descontava a multa inteira de
            // **cada** caução com saldo: com uma só funcionava por acidente, com duas cobrava em
            // dobro. E a comparação era com `Valor`, que hoje é o depositado e não o disponível
            decimal valorRestante = multa.Valor;
            foreach (var caucao in _caucao.Where(c => c.ValorDisponivel > 0))
            {
                if (valorRestante <= 0)
                    break;

                var deduzir = Math.Min(caucao.ValorDisponivel, valorRestante);
                caucao.Consumir(deduzir);
                valorRestante -= deduzir;
            }

            multa.CompensarComCaucao();
        }

        // Cancelar multa
        public void CancelarMulta(int idMulta)
        {
            var multa = _multas.FirstOrDefault(m => m.IdMulta == idMulta);
            if (multa == null)
                throw new DomainException("Multa não encontrada");

            multa.Cancelar();
        }
        #endregion caucao

        #region Seguro

        /// <param name="valorDiaria">RN-18: preço da proteção hoje, congelado no contrato.</param>
        /// <param name="franquia">RN-25: teto da avaria coberta, congelado pelo mesmo motivo.</param>
        public void AdicionarSeguro(int seguro, decimal valorDiaria, decimal franquia)
        {
            // proteção contratada depois do início é caso normal (doc 07 §4, pró-rata) — o que não
            // cabe é contratar proteção para um contrato que já acabou
            if (!EmMontagem.Contains(Status))
                throw new DomainException("Seguro só pode ser adicionado com o contrato em curso");

            if (_seguros.Any(s => s.Ativo == true))
                throw new DomainException("Locação já possui seguro ativo");

            // proteção vendida junto com o contrato cobre desde a retirada; vendida com o carro já
            // na rua cobre a partir de agora, e a RN-19 cobra pró-rata a partir daí (doc 07 §4).
            // Usar sempre `UtcNow` faria a proteção de balcão nascer atrasada em alguns segundos
            // em relação ao início do contrato, e a pró-rata entraria onde não devia
            var contratadaEm = Status == StatusLocacao.Criada ? DataInicio : DateTime.UtcNow;

            var locacaoSeguro = LocacaoSeguro.Contratar(seguro, valorDiaria, franquia, contratadaEm);
            _seguros.Add(locacaoSeguro);
        }

        public void CancelarSeguro(int idLocacaoSeguro)
        {
            var seguro = _seguros.FirstOrDefault(s => s.IdLocacaoSeguro == idLocacaoSeguro);
            if (seguro == null)
                throw new DomainException("Locação nao tem esse seguro");

            if (seguro.Ativo == false)
                throw new DomainException("esse seguro já esta desativado esse seguro");

            seguro.Cancelar();
        }

        #endregion Seguro

        #region Vistoria

        /// <summary>
        /// RN-57: é a vistoria de retirada que promove o contrato a <c>EmAndamento</c> — ou seja,
        /// que libera o carro ao cliente. Antes dela o contrato existe e a placa está
        /// comprometida, mas o veículo não saiu.
        ///
        /// A vistoria de devolução <b>não</b> muda o status: ela é a prova, e quem encerra a posse
        /// é <see cref="RegistrarDevolucao"/>, que precisa dela para rodar. Isso mantém a ordem
        /// real do balcão — o carro chega, é vistoriado, e só então a devolução é lançada.
        /// </summary>
        public void RegistrarVistoria(int idFuncionario, TipoVistoria tipo,NivelCombustivel combustivel,int km, string? observacoes, bool requerLimpezaEspecial = false)
        {
            if (AposApuracao.Contains(Status) || Status == StatusLocacao.Cancelada)
                throw new DomainException("Não é possível vistoriar locação já fechada ou cancelada");

            if (tipo == TipoVistoria.Retirada && Status != StatusLocacao.Criada)
                throw new DomainException("A vistoria de retirada só cabe em contrato ainda não retirado");

            if (tipo == TipoVistoria.Devolucao && !ComCarroNaRua.Contains(Status))
                throw new DomainException("A vistoria de devolução exige o veículo na rua");

            var vistoria = Vistoria.Criar(IdLocacao, idFuncionario, tipo,combustivel, km,observacoes, requerLimpezaEspecial);

            _vistorias.Add(vistoria);

            if (tipo == TipoVistoria.Retirada)
                Status = StatusLocacao.EmAndamento;
        }

        public void RegistrarDanoVistoria(int idVistoria, string descricao,TipoDano tipo, decimal valor)
        {
            if (AposApuracao.Contains(Status) || Status == StatusLocacao.Cancelada)
                throw new DomainException("Não é possível vistoriar locação já fechada ou cancelada");

            var vistoria = _vistorias.FirstOrDefault(v => v.IdVistoria == idVistoria);
            if (vistoria == null)
                throw new DomainException("Vistoria não encontrada");  

            // a ordem corretiva não abre aqui: o veículo ainda está locado. Quem abre é o
            // fechamento, em AbrirManutencaoPorAvaria.
            vistoria.RegistrarDano(descricao, tipo, valor);
        }

        public void RemoverDanoVistoria(int idDano, int idVistoria)
        {
            if (AposApuracao.Contains(Status) || Status == StatusLocacao.Cancelada)
                throw new DomainException("Não é possível vistoriar locação já fechada ou cancelada");

            var vistoria = _vistorias.FirstOrDefault(v => v.IdVistoria == idVistoria);
            if (vistoria == null)
                throw new DomainException("Vistoria não encontrada");

            var dano = vistoria.Danos.FirstOrDefault(d => d.IdDano == idDano);
            if (dano == null)
                throw new DomainException("Dano não encontrado");

            vistoria.RemoverDano(idDano);
        }


        public void RegistrarFoto(List<FotoVistoria> foto, int idVistoria)
        {
            var vistoria = _vistorias.FirstOrDefault(v => v.IdVistoria == idVistoria);
            if (AposApuracao.Contains(Status) || Status == StatusLocacao.Cancelada)
                throw new DomainException("Não é possível vistoriar locação já fechada ou cancelada");
            foreach (var f in foto)
            {
                vistoria.AdicionarFoto(f);
            }
        }


        public void AtualizarDados(DateTime dataFimPrevista, int kmInicial, decimal valorPrevisto)
        {
            // extensão de contrato com o carro na rua é rotina de balcão; o que não se altera é
            // contrato devolvido ou apurado
            if (!EmMontagem.Contains(Status))
                throw new InvalidOperationException("Somente contrato em curso pode ser atualizado");

            if (dataFimPrevista <= DataInicio)
                throw new InvalidOperationException("Data fim prevista deve ser posterior à data de início");

            DataFimPrevista = dataFimPrevista;
            KmInicial = kmInicial;
            ValorPrevisto = valorPrevisto;
        }
        #endregion Vistoria

        #region Adicional
        public void AdicionarAdicional(int idAdicional,decimal valorDiaria,int quantidade)
        {
            if (!EmMontagem.Contains(Status))
                throw new DomainException("Locação não permite alteração");

            if (_adicionais.Any(a => a.IdAdicional == idAdicional))
                throw new DomainException("Adicional já incluído");

            var dias = CalcularDias();
            var valorTotal = CalcularTotalAdicionais();

            var adicional = LocacaoAdicional.Criar(
                idAdicional,
                valorDiaria,
                quantidade,
                dias);

            _adicionais.Add(adicional);
        }

        public void RemoverAdicional(int idAdicional)
        {
            var adicional = _adicionais
                .FirstOrDefault(a => a.IdAdicional == idAdicional);

            if (adicional == null)
                throw new DomainException("Adicional não encontrado");

            _adicionais.Remove(adicional);
        }

        public decimal CalcularTotalAdicionais()
        {
            return _adicionais.Sum(a => a.CalcularTotal());
        }

        public int CalcularDias()
        {
            var fim = DataFimReal ?? DataFimPrevista;

            if (fim <= DataInicio)
                throw new DomainException("Data inválida");

            var totalHoras = (fim - DataInicio).TotalHours;

            return (int)Math.Ceiling(totalHoras / 24);
        }

        #endregion Adicional

        /// <summary>
        /// RN-60: atraso é <c>EmAndamento</c> que passou do fim previsto — o carro está na rua
        /// além do combinado. E <c>Atrasada</c> tem saída: <see cref="RegistrarDevolucao"/> aceita
        /// os dois, senão o contrato que mais urge fechar seria justamente o que não fecha.
        ///
        /// A comparação é por <b>instante</b>, não por data: contrato que vence às 09:00 e volta
        /// às 23:00 do mesmo dia é atraso de 14 horas, e comparar <c>.Date</c> dizia que não era.
        /// A tolerância da casa (doc 07 §9, recomendação de 30 min) entra aqui quando o parâmetro
        /// existir — backlog A3. Enquanto ela não existe, o corte é o próprio fim previsto, que é o
        /// lado conservador: marca cedo demais, nunca tarde demais.
        ///
        /// Quem chama é <c>LocacaoService.MarcarAtrasadasAsync</c>, varrido pelo
        /// <c>AtrasoLocacaoBackgroundService</c> — atraso é fato do relógio, não de um clique, e o
        /// contrato que mais importa marcar é justamente o do cliente que sumiu.
        /// </summary>
        public void MarcarComoAtrasada(DateTime agora)
        {
            if (Status == StatusLocacao.EmAndamento && agora > DataFimPrevista)
            {
                Status = StatusLocacao.Atrasada;
            }
        }
    }

    /// <summary>
    /// Doc 07 §6. Os valores explícitos são cosméticos — a coluna é <c>varchar</c>
    /// (<c>HasConversion&lt;string&gt;</c>), então o que vai para o banco é o nome. Renomear um
    /// membro daqui é migração de dado, não refatoração.
    ///
    /// <c>Pendente</c> saiu: era a <c>Reserva</c> com outro nome. Como <c>Criar</c> já compromete
    /// a placa, não existe locação "pendente" — o que existe antes disso é reserva, que tem ciclo
    /// próprio.
    /// </summary>
    public enum StatusLocacao
    {
        /// <summary>
        /// Contrato aberto e placa comprometida; o carro ainda não foi liberado ao cliente. É a
        /// janela de check-out no balcão.
        /// </summary>
        Criada = 1,

        /// <summary>RN-57: vistoria de retirada registrada — carro na rua.</summary>
        EmAndamento = 2,

        /// <summary>Passou de <c>DataFimPrevista</c> sem devolução.</summary>
        Atrasada = 3,

        /// <summary>
        /// RN-58: carro recebido e vistoriado. A posse acabou; a conta ainda não foi apurada.
        /// </summary>
        Devolvida = 4,

        /// <summary>Conta apurada e aguardando acerto. Nenhuma linha muda daqui em diante.</summary>
        Fechada = 5,

        /// <summary>Conta apurada com saldo em aberto: segue para cobrança pós-contrato.</summary>
        ComSaldoResidual = 6,

        /// <summary>Saldo quitado. Terminal.</summary>
        Finalizada = 7,

        /// <summary>
        /// RN-59: contrato anulado antes da retirada — o carro não rodou. Terminal, e o único
        /// estado que devolve o período à oferta retroativamente.
        /// </summary>
        Cancelada = 8
    }

}
