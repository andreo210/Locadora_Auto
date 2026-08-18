namespace Locadora_Auto.Domain.Entidades
{
    public class Veiculo
    {
        public int IdVeiculo { get; private set; }
        public string Placa { get; private set; } = null!;
        public string Marca { get; private set; } = null!;
        public string Modelo { get; private set; } = null!;
        public int Ano { get; private set; }
        public string Chassi { get; private set; } = null!;
        public int IdCategoria { get; private set; }
        public int KmAtual { get; private set; }
        public bool Ativo { get; private set; }
        public bool Disponivel { get; private set; }
        public int FilialAtualId { get; private set; }
        public StatusVeiculo Status { get; private set; }

        /// <summary>
        /// RN-56: por que o carro saiu da frota — idade, quilometragem, custo acumulado de
        /// manutenção, perda total, queda de demanda do grupo.
        ///
        /// Ficam como colunas do próprio veículo, e não como entidade filha ao lado de
        /// <see cref="Bloqueios"/> e <see cref="Transferencias"/>, porque desmobilização não tem
        /// duas pontas nem se repete: é <b>terminal</b>. Uma tabela para uma linha por veículo, que
        /// nunca é encerrada nem cancelada, seria estrutura sem regra atrás.
        ///
        /// A venda em si (canal, comprador, valor, baixa do ativo) é outro processo e não está no
        /// escopo desta especificação — o que a RN-56 fecha é o ativo parar de ser frota.
        /// </summary>
        public string? MotivoDesmobilizacao { get; private set; }
        public DateTime? DataDesmobilizacao { get; private set; }
        public int? IdFuncionarioDesmobilizacao { get; private set; }

        //navegação
        public CategoriaVeiculo Categoria { get; set; } = null!;
        public Filial FilialAtual { get; set; } = null!;

        public ICollection<Locacao> Locacoes { get; set; } = new List<Locacao>();

        //manutenção
        private readonly List<Manutencao> _manutencoes = new();
        public IReadOnlyCollection<Manutencao> Manutencoes => _manutencoes;

        //RN-37: histórico de transições do ativo, escrito só por AplicarStatus
        private readonly List<MovimentoVeiculo> _movimentos = new();
        public IReadOnlyCollection<MovimentoVeiculo> Movimentos => _movimentos;

        //RN-52: bloqueios com motivo, prazo e responsável. Mesmo desenho de Manutencao — filho do
        //agregado, criado só por Bloquear()
        private readonly List<BloqueioVeiculo> _bloqueios = new();
        public IReadOnlyCollection<BloqueioVeiculo> Bloqueios => _bloqueios;

        //RN-48/RN-49: transferências programadas entre filiais, criadas só por
        //EnviarParaTransferencia()
        private readonly List<TransferenciaVeiculo> _transferencias = new();
        public IReadOnlyCollection<TransferenciaVeiculo> Transferencias => _transferencias;

        public static Veiculo Criar(string placa, string marca, string modelo, int ano, string chassi, int kmAtual,int idCategoria, int idFilialAtual)
        {
            if (string.IsNullOrWhiteSpace(placa))
                throw new InvalidOperationException("placa é obrigatório");
            if (string.IsNullOrWhiteSpace(marca))
                throw new InvalidOperationException("marca é obrigatório");
            if (string.IsNullOrWhiteSpace(modelo))
                throw new InvalidOperationException("modelo é obrigatório");
            if (!int.IsPositive(ano))
                throw new InvalidOperationException("ano tem que ser um numero positivo");
            if (string.IsNullOrWhiteSpace(chassi))
                throw new InvalidOperationException("chassi é obrigatório");
            // veículo zero km é válido, então só km negativo é recusado
            if (kmAtual < 0)
                throw new InvalidOperationException("kmAtual não pode ser negativo");
            if (!int.IsPositive(idCategoria))
                throw new InvalidOperationException("idCategoria tem que ser um numero positivo");
            if (!int.IsPositive(idFilialAtual))
                throw new InvalidOperationException("idFilialAtual tem que ser um numero positivo");

            var veiculo = new Veiculo
            {
                Placa = placa.Trim().ToUpper(),
                Marca = marca.Trim().ToUpper(),
                Modelo = modelo.Trim().ToUpper(),
                Ano = ano,
                Chassi = chassi.Trim().ToUpper(),
                KmAtual = kmAtual,
                IdCategoria = idCategoria,
                FilialAtualId = idFilialAtual,
                Ativo = true
            };

            veiculo.AplicarStatus(StatusVeiculo.Disponivel, TipoDocumentoOrigem.Cadastro);

            return veiculo;
        }

        public void Atualizar(int kmAtual,int idFilialAtual, string? marca = null, string? modelo = null, int? ano = null)
        {
            // RN-56: o ativo baixado não é mais frota. Mexer no hodômetro ou na filial dele seria
            // corromper dado de um carro que já não é da casa — e a guarda do AplicarStatus não
            // pega este caminho, porque Atualizar não é transição de situação
            if (Status == StatusVeiculo.Desmobilizado)
                throw new DomainException("Veículo desmobilizado não pode ser alterado");

            if (!int.IsPositive(idFilialAtual))
                throw new InvalidOperationException("idFilialAtual tem que ser um numero positivo");
            RegistrarKm(kmAtual);
            FilialAtualId = idFilialAtual;

            // marca/modelo/ano são opcionais: só mudam quando vêm preenchidos
            if (!string.IsNullOrWhiteSpace(marca))
                Marca = marca.Trim().ToUpper();
            if (!string.IsNullOrWhiteSpace(modelo))
                Modelo = modelo.Trim().ToUpper();
            if (ano.HasValue)
            {
                if (!int.IsPositive(ano.Value))
                    throw new InvalidOperationException("ano tem que ser um numero positivo");
                Ano = ano.Value;
            }
        }

        /// <summary>
        /// Reativação cadastral. Devolve à oferta o carro que estava fora dela <b>só</b> por estar
        /// inativo; locado, em preparação, em oficina ou com bloqueio da RN-52 continuam onde
        /// estão até a transição própria de cada um — reativar não libera bloqueio.
        /// </summary>
        public void Ativar()
        {
            Ativo = true;

            // só volta à oferta quem não tem bloqueio em aberto: senão a reativação furaria a
            // RN-52 pela porta dos fundos, devolvendo à venda um carro que alguém tirou dela com
            // motivo, prazo e responsável registrados
            var destino = Status == StatusVeiculo.Bloqueado && !TemBloqueioEmAberto()
                ? StatusVeiculo.Disponivel
                : Status;

            AplicarStatus(destino, TipoDocumentoOrigem.Cadastro);
        }

        /// <summary>
        /// Desativação cadastral: o carro sai do registro ativo da frota.
        ///
        /// Não é bloqueio da RN-52 e de propósito não tem prazo nem responsável — ela não é
        /// temporária, a saída dela é <see cref="Ativar"/>, e ela aparece em qualquer filtro por
        /// <c>Ativo</c>. O que a RN-52 quer evitar é o carro que some da oferta <b>sem ninguém
        /// perceber</b>, e este não some: some do cadastro, à vista. Por isso a origem na trilha é
        /// <see cref="TipoDocumentoOrigem.Cadastro"/> e ele fica fora do indicador de bloqueios
        /// vencidos.
        /// </summary>
        public void Desativar()
        {
            Ativo = false;

            // desativar um carro que está na rua não o traz de volta: ele sai da oferta agora
            // (Disponivel é derivado de Ativo) e o status só muda na devolução
            AplicarStatus(
                Status == StatusVeiculo.Disponivel ? StatusVeiculo.Bloqueado : Status,
                TipoDocumentoOrigem.Cadastro);
        }


        public void Valida(string placa, string marca, string modelo, int ano, string chassi, int kmAtual, int idCategoria, int idFilialAtual)
        {
            if (string.IsNullOrWhiteSpace(placa))
                throw new InvalidOperationException("placa é obrigatório");
            if (string.IsNullOrWhiteSpace(marca))
                throw new InvalidOperationException("marca é obrigatório");
            if (string.IsNullOrWhiteSpace(modelo))
                throw new InvalidOperationException("modelo é obrigatório");
            if (!int.IsPositive(ano))
                throw new InvalidOperationException("ano tem que ser um numero positivo");
            if (string.IsNullOrWhiteSpace(chassi))
                throw new InvalidOperationException("chassi é obrigatório");
            if (!int.IsPositive(kmAtual))
                throw new InvalidOperationException("kmAtual tem que ser um numero positivo");
            if (!int.IsPositive(idCategoria))
                throw new InvalidOperationException("idCategoria tem que ser um numero positivo");
            if (!int.IsPositive(idFilialAtual))
                throw new InvalidOperationException("idFilialAtual tem que ser um numero positivo");
        }

        #region Ciclo do ativo

        /// <summary>
        /// Abrir contrato consome a placa. Só sai da oferta quem estava nela — é aqui que o
        /// veículo passa a <see cref="StatusVeiculo.Locado"/>.
        /// </summary>
        /// <param name="contrato">
        /// RN-37: o documento que autoriza a saída da oferta. Vem como entidade, e não como id,
        /// porque na abertura o contrato ainda não foi gravado.
        /// </param>
        public void Locar(Locacao contrato)
        {
            if (contrato == null)
                throw new DomainException("Locação do veículo exige o contrato de origem");

            if (!Ativo)
                throw new DomainException("Veículo inativo não pode ser locado");

            if (Status != StatusVeiculo.Disponivel)
                throw new DomainException($"Veículo não está disponível para locação (status atual: {Status})");

            AplicarStatus(StatusVeiculo.Locado, TipoDocumentoOrigem.Contrato, contrato: contrato);
        }

        /// <summary>
        /// Devolução leva à preparação, nunca direto à oferta: o carro devolvido às 10h ainda
        /// precisa de vistoria, limpeza e abastecimento. O odômetro e a filial do ativo avançam
        /// aqui, que é o único ponto em que a devolução é conhecida.
        /// </summary>
        public void RegistrarDevolucao(int kmFinal, int idFilialDevolucao, Locacao contrato)
        {
            if (contrato == null)
                throw new DomainException("Devolução do veículo exige o contrato de origem");

            if (Status != StatusVeiculo.Locado)
                throw new DomainException($"Só veículo locado pode ser devolvido (status atual: {Status})");

            if (!int.IsPositive(idFilialDevolucao))
                throw new DomainException("idFilialDevolucao tem que ser um numero positivo");

            RegistrarKm(kmFinal);
            FilialAtualId = idFilialDevolucao;

            AplicarStatus(StatusVeiculo.EmPreparacao, TipoDocumentoOrigem.Contrato, contrato: contrato);
        }

        /// <summary>
        /// O pátio declara o carro pronto. Como toda saída de indisponibilidade, só devolve à
        /// oferta se o veículo estiver ativo.
        ///
        /// Não recebe documento: aqui a origem é o próprio ato do pátio, e o que responde "quem
        /// liberou" é o autor gravado no movimento.
        /// </summary>
        public void LiberarDaPreparacao() => SairDaPreparacao(TipoDocumentoOrigem.Patio);

        /// <summary>
        /// RN-45, parte automática: o <c>TempoPreparacaoMinutos</c> da filial venceu e o carro
        /// volta à oferta sem o pátio ter declarado nada. Existe para o veículo não ficar preso na
        /// fila por esquecimento — frota que some da oferta e ninguém percebe.
        ///
        /// É transição idêntica à do pátio, com um único ponto de diferença que é justamente o que
        /// importa: a origem gravada é <see cref="TipoDocumentoOrigem.Prazo"/>, porque aqui
        /// ninguém conferiu o carro.
        /// </summary>
        public void LiberarDaPreparacaoPorPrazo() => SairDaPreparacao(TipoDocumentoOrigem.Prazo);

        private void SairDaPreparacao(TipoDocumentoOrigem origem)
        {
            if (Status != StatusVeiculo.EmPreparacao)
                throw new DomainException($"Veículo não está em preparação (status atual: {Status})");

            SairParaOferta(origem);
        }

        /// <summary>
        /// Cancelamento da abertura: o contrato foi anulado e o carro não rodou, então volta à
        /// oferta sem passar pela preparação.
        /// </summary>
        public void ReverterLocacao(Locacao contrato)
        {
            if (contrato == null)
                throw new DomainException("Reversão da locação exige o contrato de origem");

            if (Status != StatusVeiculo.Locado)
                throw new DomainException($"Veículo não está locado (status atual: {Status})");

            SairParaOferta(TipoDocumentoOrigem.Contrato, contrato: contrato);
        }

        /// <summary>
        /// Única escrita de <see cref="Status"/> e <see cref="Disponivel"/>. O booleano continua
        /// existindo porque o filtro de disponibilidade precisa traduzir para SQL, mas é
        /// derivado — quem manda é o status, e assim os dois nunca divergem.
        ///
        /// RN-37: por ser o gargalo único, é também o único ponto que registra
        /// <see cref="MovimentoVeiculo"/> — e como a origem é parâmetro obrigatório, transição sem
        /// documento não compila, em vez de depender de quem chama lembrar de registrar.
        /// </summary>
        private void AplicarStatus(
            StatusVeiculo novoStatus,
            TipoDocumentoOrigem tipoOrigem,
            Locacao? contrato = null,
            Manutencao? ordemServico = null,
            BloqueioVeiculo? bloqueio = null,
            TransferenciaVeiculo? transferencia = null)
        {
            var statusAnterior = Status;
            var disponivelAnterior = Disponivel;

            // RN-56: Desmobilizado é terminal. A guarda mora aqui, e não em cada operação, porque
            // AplicarStatus é o gargalo único de escrita do status — assim nenhuma transição nova
            // pode ressuscitar um carro vendido por esquecimento de quem a escreveu.
            //
            // Vale inclusive para Ativar() e Desativar(): reativar um ativo baixado recolocaria na
            // frota um carro que já não é da casa, e ainda esbarraria na placa que a RN-55 liberou
            // para recadastro quando ele saiu.
            if (statusAnterior == StatusVeiculo.Desmobilizado)
                throw new DomainException("Veículo desmobilizado não muda mais de situação");

            Status = novoStatus;
            Disponivel = Ativo && novoStatus == StatusVeiculo.Disponivel;

            // reaplicar o mesmo estado não é transição: Ativar() num carro que já está na oferta,
            // ou Desativar() num que já estava fora, não vira linha de auditoria
            if (statusAnterior == Status && disponivelAnterior == Disponivel)
                return;

            _movimentos.Add(MovimentoVeiculo.Criar(
                IdVeiculo,
                // no cadastro não havia situação anterior: o enum começa em 1, então o default
                // aqui é o veículo que ainda não existia
                statusAnterior == default ? null : statusAnterior,
                novoStatus,
                tipoOrigem,
                contrato,
                ordemServico,
                bloqueio,
                transferencia));
        }

        /// <summary>
        /// Hodômetro não anda para trás: km menor que o registrado é adulteração ou erro de
        /// digitação, e nos dois casos o caso é de apuração, não de gravação.
        /// </summary>
        private void RegistrarKm(int km)
        {
            if (km < 0)
                throw new InvalidOperationException("kmAtual não pode ser negativo");

            if (km < KmAtual)
                throw new DomainException($"Quilometragem não pode retroceder: atual {KmAtual}, informada {km}");

            KmAtual = km;
        }

        /// <summary>
        /// Saída de manutenção, preparação ou bloqueio. O veículo inativo não volta para a oferta.
        /// </summary>
        private void SairParaOferta(
            TipoDocumentoOrigem tipoOrigem,
            Locacao? contrato = null,
            Manutencao? ordemServico = null,
            BloqueioVeiculo? bloqueio = null,
            TransferenciaVeiculo? transferencia = null)
        {
            AplicarStatus(
                Ativo ? StatusVeiculo.Disponivel : StatusVeiculo.Bloqueado,
                tipoOrigem,
                contrato,
                ordemServico,
                bloqueio,
                transferencia);
        }

        #endregion Ciclo do ativo

        #region Bloqueio

        /// <summary>
        /// Situações a partir das quais o carro pode ser bloqueado (RN-52).
        ///
        /// <c>Disponivel</c> é o caso normal — segurar oferta. <c>EmPreparacao</c> entra porque a
        /// pendência costuma aparecer justamente na conferência do pátio. <c>Locado</c> entra pelos
        /// dois casos do doc 08 §5: o carro não devolvido além do limiar e o carro sinistrado
        /// durante o contrato — nos dois o contrato segue aberto, mas deixar o ativo em
        /// <c>Locado</c> indefinidamente contaminaria a utilização da seção 12.
        ///
        /// <c>EmManutencao</c> fica de fora de propósito: o carro já está fora da oferta com uma
        /// ordem de serviço respondendo por ele, e sobrepor um bloqueio apagaria de que OS ele
        /// depende. Quem quiser bloquear encerra ou cancela a ordem primeiro.
        /// </summary>
        private static readonly StatusVeiculo[] Bloqueaveis =
        {
            StatusVeiculo.Disponivel,
            StatusVeiculo.EmPreparacao,
            StatusVeiculo.Locado
        };

        /// <summary>
        /// A lista de <see cref="Bloqueaveis"/> exposta para o serviço repetir a guarda antes de
        /// chamar o domínio — é o que faz a recusa sair como ProblemDetails 4xx em vez de 500.
        /// </summary>
        public static bool PodeSerBloqueado(StatusVeiculo status) => Bloqueaveis.Contains(status);

        /// <summary>
        /// RN-52: tira o carro da oferta com motivo, prazo e responsável.
        /// </summary>
        /// <returns>
        /// O bloqueio aberto — é ele o documento de origem do movimento (RN-37) e é dele que sai o
        /// id que a liberação vai usar.
        /// </returns>
        public BloqueioVeiculo Bloquear(
            MotivoBloqueio motivo,
            DateTime dataPrevistaLiberacao,
            int idFuncionarioResponsavel,
            string? observacao = null)
        {
            if (TemBloqueioEmAberto())
                throw new DomainException("Veículo já possui bloqueio em aberto");

            if (!Bloqueaveis.Contains(Status))
                throw new DomainException($"Veículo não pode ser bloqueado na situação atual ({Status})");

            var bloqueio = BloqueioVeiculo.Criar(
                IdVeiculo,
                motivo,
                dataPrevistaLiberacao,
                idFuncionarioResponsavel,
                Status,
                observacao);

            _bloqueios.Add(bloqueio);

            AplicarStatus(StatusVeiculo.Bloqueado, TipoDocumentoOrigem.Bloqueio, bloqueio: bloqueio);

            return bloqueio;
        }

        /// <summary>
        /// Encerra o bloqueio e devolve o veículo à situação em que ele estava.
        ///
        /// Bloqueio <b>suspende</b>, não apaga: liberar tudo direto para a oferta devolveria à
        /// venda um carro que ainda não foi limpo (estava em preparação) ou que nem está na filial
        /// (estava locado e não foi devolvido). Por isso o destino sai do
        /// <see cref="BloqueioVeiculo.StatusAnterior"/>, e não de uma constante.
        /// </summary>
        public void LiberarBloqueio(int idBloqueio)
        {
            if (Status != StatusVeiculo.Bloqueado)
                throw new DomainException($"Veículo não está bloqueado (situação atual: {Status})");

            var bloqueio = _bloqueios.FirstOrDefault(b => b.IdBloqueioVeiculo == idBloqueio && b.EmAberto)
                ?? throw new DomainException($"Bloqueio {idBloqueio} não encontrado em aberto para este veículo");

            bloqueio.Encerrar();

            switch (bloqueio.StatusAnterior)
            {
                // o contrato continua aberto e o carro continua na rua: ele volta para Locado
                // mesmo se o veículo estiver inativo, como manda o doc 08 §5 — a desativação só
                // surte efeito na saída da preparação, depois da devolução
                case StatusVeiculo.Locado:
                    AplicarStatus(StatusVeiculo.Locado, TipoDocumentoOrigem.Bloqueio, bloqueio: bloqueio);
                    return;

                // volta para a fila do pátio: o carro continua sem conferência, e o relógio da
                // RN-45 recomeça agora — durante o bloqueio o pátio não tinha como trabalhar nele
                case StatusVeiculo.EmPreparacao when Ativo:
                    AplicarStatus(StatusVeiculo.EmPreparacao, TipoDocumentoOrigem.Bloqueio, bloqueio: bloqueio);
                    return;

                // RN-53: as demais saídas só devolvem à oferta se o veículo estiver ativo
                default:
                    SairParaOferta(TipoDocumentoOrigem.Bloqueio, bloqueio: bloqueio);
                    return;
            }
        }

        /// <summary>
        /// Um bloqueio em aberto por vez. Dois bloqueios simultâneos deixariam a liberação sem
        /// resposta para "voltar para onde", e o carro só pode estar fora da oferta por um motivo
        /// de cada vez — o segundo motivo é observação do primeiro, ou é bloqueio novo depois de
        /// liberar este.
        /// </summary>
        public bool TemBloqueioEmAberto() => _bloqueios.Any(b => b.EmAberto);

        /// <summary>O bloqueio que está segurando o carro agora, se houver.</summary>
        public BloqueioVeiculo? BloqueioEmAberto() => _bloqueios.FirstOrDefault(b => b.EmAberto);

        #endregion Bloqueio

        #region Transferencia entre filiais

        /// <summary>
        /// RN-49: manda o carro para outra filial. Ele sai da oferta da origem <b>agora</b> e só
        /// entra na do destino quando chegar — durante o trecho não conta em filial nenhuma, que é
        /// o ponto inteiro da regra.
        ///
        /// Só sai da oferta quem estava nela: <c>Disponivel</c>. Carro locado está com o cliente,
        /// em preparação está sujo, em oficina está desmontado e bloqueado tem motivo próprio —
        /// nenhum deles pode simplesmente pegar a estrada, e transferir um deles esconderia a
        /// situação real atrás de um status de trânsito.
        ///
        /// <c>FilialAtualId</c> <b>não</b> muda aqui, de propósito: enquanto o carro roda, a filial
        /// que responde por ele é a de origem. Trocar no envio faria o destino contá-lo como frota
        /// antes de ele existir lá — o overbooking que a RN-49 existe para impedir, só que do outro
        /// lado.
        /// </summary>
        /// <param name="idFilialDestino">
        /// Se a filial existe, está ativa e aceita transferência (<c>PermiteTransferencia</c>) é o
        /// serviço que confere: são outro agregado, e o veículo não os enxerga.
        /// </param>
        public TransferenciaVeiculo EnviarParaTransferencia(
            int idFilialDestino,
            DateTime dataPrevistaChegada,
            int idFuncionarioResponsavel,
            string? observacao = null)
        {
            if (!Ativo)
                throw new DomainException("Veículo inativo não pode ser transferido");

            if (TemTransferenciaEmTransito())
                throw new DomainException("Veículo já está em transferência");

            if (Status != StatusVeiculo.Disponivel)
                throw new DomainException($"Só veículo disponível pode ser transferido (situação atual: {Status})");

            var transferencia = TransferenciaVeiculo.Criar(
                IdVeiculo,
                FilialAtualId,
                idFilialDestino,
                dataPrevistaChegada,
                idFuncionarioResponsavel,
                observacao);

            _transferencias.Add(transferencia);

            AplicarStatus(StatusVeiculo.EmTransferencia, TipoDocumentoOrigem.Transferencia, transferencia: transferencia);

            return transferencia;
        }

        /// <summary>
        /// O carro chegou: a filial de destino passa a ser a atual e ele volta à oferta — de lá.
        ///
        /// O hodômetro avança pela mesma porta de sempre (<see cref="RegistrarKm"/>), porque o
        /// trecho é rodado: não registrar o km do deslocamento faria a próxima devolução parecer
        /// ter percorrido a viagem inteira, e a RN-54 recusaria a leitura correta como retrocesso.
        /// </summary>
        public void ConfirmarChegadaTransferencia(int idTransferencia, int kmChegada)
        {
            if (Status != StatusVeiculo.EmTransferencia)
                throw new DomainException($"Veículo não está em transferência (situação atual: {Status})");

            var transferencia = ObterTransferenciaEmTransito(idTransferencia);

            RegistrarKm(kmChegada);
            transferencia.ConfirmarChegada();
            FilialAtualId = transferencia.IdFilialDestino;

            // RN-53: chegar não devolve à oferta um carro que foi desativado no meio do caminho
            SairParaOferta(TipoDocumentoOrigem.Transferencia, transferencia: transferencia);
        }

        /// <summary>
        /// A transferência foi abortada e o carro volta à oferta da origem — onde
        /// <c>FilialAtualId</c> nunca deixou de apontar.
        /// </summary>
        public void CancelarTransferencia(int idTransferencia)
        {
            if (Status != StatusVeiculo.EmTransferencia)
                throw new DomainException($"Veículo não está em transferência (situação atual: {Status})");

            var transferencia = ObterTransferenciaEmTransito(idTransferencia);

            transferencia.Cancelar();

            SairParaOferta(TipoDocumentoOrigem.Transferencia, transferencia: transferencia);
        }

        public bool TemTransferenciaEmTransito() => _transferencias.Any(t => t.EmTransito);

        /// <summary>A viagem em curso, se houver.</summary>
        public TransferenciaVeiculo? TransferenciaEmTransito()
            => _transferencias.FirstOrDefault(t => t.EmTransito);

        private TransferenciaVeiculo ObterTransferenciaEmTransito(int idTransferencia)
            => _transferencias.FirstOrDefault(t => t.IdTransferenciaVeiculo == idTransferencia && t.EmTransito)
               ?? throw new DomainException($"Transferência {idTransferencia} não encontrada em trânsito para este veículo");

        #endregion Transferencia entre filiais

        #region Desmobilizacao

        /// <summary>
        /// Situações a partir das quais o carro pode ser desmobilizado (RN-56).
        ///
        /// <c>Disponivel</c> e <c>EmPreparacao</c> são o caminho normal — o carro voltou da última
        /// locação e vai para a venda. <c>Bloqueado</c> entra porque é o próprio caminho que a
        /// operação usa: a desmobilização começa tirando o carro da agenda
        /// (<see cref="MotivoBloqueio.Desmobilizacao"/>) e termina aqui.
        ///
        /// Ficam de fora <c>Locado</c> (vender carro com cliente dentro é o pior desfecho
        /// possível), <c>EmManutencao</c> (a ordem está aberta e tem custo a apurar) e
        /// <c>EmTransferencia</c> (o carro está na estrada e nem chegou a lugar nenhum).
        /// </summary>
        private static readonly StatusVeiculo[] Desmobilizaveis =
        {
            StatusVeiculo.Disponivel,
            StatusVeiculo.EmPreparacao,
            StatusVeiculo.Bloqueado
        };

        /// <summary>
        /// Exposta para o serviço repetir a guarda antes de chamar o domínio, e a recusa sair como
        /// ProblemDetails 4xx.
        /// </summary>
        public static bool PodeSerDesmobilizado(StatusVeiculo status) => Desmobilizaveis.Contains(status);

        /// <summary>
        /// RN-56: o ativo deixa a frota. Estado <b>terminal</b> — nenhuma transição sai daqui.
        ///
        /// Sai também do cadastro ativo (<c>Ativo = false</c>), e não por conveniência: carro
        /// vendido não é frota, então ele some de toda consulta por <c>Ativo</c> e — pela RN-55 —
        /// libera a placa e o chassi para o dia em que o Detran os reemitir.
        ///
        /// A ausência de contrato aberto é checada pelo serviço: contrato <b>futuro</b> já vendido
        /// não deixa rastro no status de agora, e é justamente ele o caso perigoso.
        /// </summary>
        public void Desmobilizar(string motivo, int idFuncionarioResponsavel)
        {
            if (string.IsNullOrWhiteSpace(motivo))
                throw new DomainException("Desmobilização exige o motivo");

            // comparação explícita, e não int.IsPositive: aquele devolve true para zero
            if (idFuncionarioResponsavel <= 0)
                throw new DomainException("Desmobilização exige um funcionário responsável");

            if (Status == StatusVeiculo.Desmobilizado)
                throw new DomainException("Veículo já está desmobilizado");

            if (!Desmobilizaveis.Contains(Status))
                throw new DomainException($"Veículo não pode ser desmobilizado na situação atual ({Status})");

            // o bloqueio da etapa de preparação para venda acaba aqui: deixá-lo em aberto o
            // deixaria para sempre no indicador de bloqueios vencidos, contando como carro
            // esquecido quando na verdade ele foi vendido
            BloqueioEmAberto()?.Encerrar();

            MotivoDesmobilizacao = motivo.Trim();
            DataDesmobilizacao = DateTime.UtcNow;
            IdFuncionarioDesmobilizacao = idFuncionarioResponsavel;

            Ativo = false;

            AplicarStatus(StatusVeiculo.Desmobilizado, TipoDocumentoOrigem.Desmobilizacao);
        }

        #endregion Desmobilizacao

        #region Manutenção do veiculo
        public void IniciarManutencao(TipoManutencao tipo, string descricao)
        {
            if (Status == StatusVeiculo.Locado)
                throw new DomainException("Veículo locado não pode entrar em manutenção");

            // a ordem recém-aberta é o documento de origem do próprio movimento (RN-37)
            var manutencao = Manutencao.Criar(tipo, descricao);
            _manutencoes.Add(manutencao);

            AplicarStatus(StatusVeiculo.EmManutencao, TipoDocumentoOrigem.OrdemServico, ordemServico: manutencao);
        }
        public void TerminaManutencao(decimal custo, int idManutencao)
        {
            if (Status == StatusVeiculo.Locado)
                throw new DomainException("Veículo locado não pode entrar em manutenção");

            var manutencao = ObterManutencao(idManutencao);
            manutencao.Encerrar(custo);
            SairParaOferta(TipoDocumentoOrigem.OrdemServico, ordemServico: manutencao);
        }

        public void CancelarManutencao(int idManutencao)
        {
            if (Status == StatusVeiculo.Locado)
                throw new DomainException("Veículo locado não pode entrar em manutenção");

            var manutencao = ObterManutencao(idManutencao);
            manutencao.Cancelar();
            SairParaOferta(TipoDocumentoOrigem.OrdemServico, ordemServico: manutencao);
        }

        public void AtualizarDescricaoManutencao(int idManutencao, string descricao)
        {
            if (Status == StatusVeiculo.Locado)
                throw new DomainException("Veículo locado não pode entrar em manutenção");

            var manutencao = ObterManutencao(idManutencao);
            // só a descrição muda: o veículo continua no status em que estava
            manutencao.AtualizarDescricao(descricao);
        }

        private Manutencao ObterManutencao(int idManutencao)
        {
            var manutencao = _manutencoes.FirstOrDefault(x => x.IdManutencao == idManutencao);
            if (manutencao == null)
                throw new DomainException($"Manutenção {idManutencao} não encontrada para este veículo");

            return manutencao;
        }
        #endregion Manutenção do veiculo
    }

    /// <summary>
    /// Situação do ativo, fonte única de verdade da RN-35. A coluna é <c>int</c>
    /// (<c>HasConversion&lt;int&gt;</c>), então os <b>valores</b> é que são o contrato com o banco
    /// e com o front — renomear um membro é refatoração, mudar o número é migração de dado.
    /// </summary>
    public enum StatusVeiculo
    {
        Disponivel = 1,

        /// <summary>
        /// Fora da oferta por decisão da casa. Chega-se aqui por dois atos diferentes, e a trilha
        /// da RN-37 os separa pelo <c>TipoDocumentoOrigem</c>: o bloqueio da RN-52
        /// (<c>Bloqueio</c>), que tem motivo, prazo e responsável, e a desativação cadastral
        /// (<c>Cadastro</c>), que não é temporária e sai por <c>Ativar()</c>.
        ///
        /// Chamava-se <c>Indisponivel</c>: o nome antigo descrevia o efeito e escondia a causa, e
        /// era ele que deixava um carro sumir da oferta sem ninguém saber por quê nem até quando.
        /// </summary>
        Bloqueado = 2,

        Locado = 3,
        EmManutencao = 4,

        /// <summary>
        /// Devolvido e ainda não conferido: fila do pátio entre a devolução e a volta à oferta.
        /// </summary>
        EmPreparacao = 5,

        /// <summary>
        /// RN-49: a caminho de outra filial por remanejamento programado. Não conta na oferta de
        /// nenhuma das duas — nem da origem, de onde já saiu, nem do destino, onde ainda não
        /// chegou. Contá-lo nas duas é overbooking involuntário.
        ///
        /// <b>Devolução one-way não passa por aqui</b> (RN-48): naquele caso o carro fica
        /// disponível no destino, porque a taxa de retorno já pagou o desequilíbrio.
        /// </summary>
        EmTransferencia = 6,

        /// <summary>
        /// RN-56: o ativo deixou a frota. <b>Terminal</b> — nenhuma transição sai daqui, nem a
        /// reativação cadastral.
        ///
        /// Existe como situação própria, e não como <c>Ativo = false</c>, porque a pergunta que o
        /// gestor faz é outra: carro parado por escolha (venda) e carro parado por falha
        /// (aguardando peça) custam o mesmo e exigem decisões opostas. Agrupá-los em "inativo" é o
        /// que faz o indicador de utilização mentir.
        /// </summary>
        Desmobilizado = 7
    }

}
