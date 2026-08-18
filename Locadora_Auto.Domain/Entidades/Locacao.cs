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
        /// </summary>
        public void RegistrarDevolucao(DateTime dataFimReal, int kmFinal, int filialDevolucao)
        {
            if (!ComCarroNaRua.Contains(Status))
                throw new InvalidOperationException("Somente locações com o veículo na rua podem ser devolvidas");

            if (!_vistorias.Any(v => v.Tipo == TipoVistoria.Retirada))
                throw new InvalidOperationException("Contrato sem vistoria de retirada não pode ser devolvido");

            if (!_vistorias.Any(v => v.Tipo == TipoVistoria.Devolucao))
                throw new InvalidOperationException("Registre a vistoria de devolução antes de encerrar a posse");

            if (dataFimReal < DataInicio)
                throw new InvalidOperationException("Data de finalização não pode ser anterior à data de início");

            if (kmFinal < KmInicial)
                throw new InvalidOperationException("Quilometragem final não pode ser menor que a inicial");

            DataFimReal = dataFimReal;
            KmFinal = kmFinal;
            IdFilialDevolucao = filialDevolucao;
            Status = StatusLocacao.Devolvida;

            // o carro entra na fila do pátio, e o odômetro e a filial do ativo avançam com o que a
            // devolução informou — sem isso a frota fica registrada na filial errada
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

            if (valorFinal < 0)
                throw new InvalidOperationException("Valor final não pode ser negativo");

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
            => (ValorFinal ?? 0m) - _pagamentos
                .Where(p => p.Status == StatusPagamento.Pago)
                .Sum(p => p.Valor);

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
            Caucao.Deduzir(valor);
        }

        /// <summary>
        /// Doc 07 §6, transição proibida: liberar caução antes de <c>Fechada</c>. Caução é
        /// garantia, e devolvê-la antes de apurar a conta é abrir mão da garantia justamente no
        /// momento em que ela serve para alguma coisa.
        ///
        /// A máquina interna da <c>Caucao</c> continua quebrada (só <c>Pendente</c> devolve, e
        /// <c>Utilizada</c> nunca é atribuída) — conserto é do backlog A10.
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

            if (Caucoes == null || Caucoes.Sum(c => c.Valor) < multa.Valor)
                throw new DomainException("Caução insuficiente para compensar a multa");

            // Deduz o valor da multa das cauções, usando múltiplas se necessário
            decimal valorRestante = multa.Valor;
            foreach (var caucao in Caucoes.Where(c => c.Valor > 0))
            {
                if (valorRestante <= 0)
                    break;

                var deduzir = Math.Min(caucao.Valor, valorRestante);
                caucao.Deduzir(deduzir);
                //valorRestante -= deduzir;
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

            var locacaoSeguro = LocacaoSeguro.Contratar(seguro, valorDiaria, franquia);
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
        public void RegistrarVistoria(int idFuncionario, TipoVistoria tipo,NivelCombustivel combustivel,int km, string? observacoes)
        {
            if (AposApuracao.Contains(Status) || Status == StatusLocacao.Cancelada)
                throw new DomainException("Não é possível vistoriar locação já fechada ou cancelada");

            if (tipo == TipoVistoria.Retirada && Status != StatusLocacao.Criada)
                throw new DomainException("A vistoria de retirada só cabe em contrato ainda não retirado");

            if (tipo == TipoVistoria.Devolucao && !ComCarroNaRua.Contains(Status))
                throw new DomainException("A vistoria de devolução exige o veículo na rua");

            var vistoria = Vistoria.Criar(IdLocacao, idFuncionario, tipo,combustivel, km,observacoes);

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
