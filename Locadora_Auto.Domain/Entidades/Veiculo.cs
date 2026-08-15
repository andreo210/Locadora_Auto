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

        public void Ativar()
        {
            Ativo = true;

            // o bloqueio administrativo cai junto com a reativação; locado, em preparação ou em
            // oficina continuam onde estão até a transição própria de cada um
            AplicarStatus(
                Status == StatusVeiculo.Indisponivel ? StatusVeiculo.Disponivel : Status,
                TipoDocumentoOrigem.Cadastro);
        }
        public void Desativar()
        {
            Ativo = false;

            // desativar um carro que está na rua não o traz de volta: ele sai da oferta agora
            // (Disponivel é derivado de Ativo) e o status só muda na devolução
            AplicarStatus(
                Status == StatusVeiculo.Disponivel ? StatusVeiculo.Indisponivel : Status,
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
        public void LiberarDaPreparacao()
        {
            if (Status != StatusVeiculo.EmPreparacao)
                throw new DomainException($"Veículo não está em preparação (status atual: {Status})");

            SairParaOferta(TipoDocumentoOrigem.Patio);
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
            Manutencao? ordemServico = null)
        {
            var statusAnterior = Status;
            var disponivelAnterior = Disponivel;

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
                ordemServico));
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
            Manutencao? ordemServico = null)
        {
            AplicarStatus(
                Ativo ? StatusVeiculo.Disponivel : StatusVeiculo.Indisponivel,
                tipoOrigem,
                contrato,
                ordemServico);
        }

        #endregion Ciclo do ativo

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

    public enum StatusVeiculo
    {
        Disponivel = 1,
        Indisponivel = 2,
        Locado = 3,
        EmManutencao = 4,
        /// <summary>
        /// Devolvido e ainda não conferido: fila do pátio entre a devolução e a volta à oferta.
        /// </summary>
        EmPreparacao = 5
    }

}
