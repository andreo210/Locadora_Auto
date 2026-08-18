namespace Locadora_Auto.Domain.Entidades
{
    public class Filial
    {
        public int IdFilial { get; private set; }
        public string Nome { get; private set; }
        public string Cidade { get; private set; }
        public bool Ativo { get; private set; }

        public int IdEndereco { get; set; }
        public Endereco Endereco { get; private set; } = null!;

        /// <summary>
        /// Minutos entre a devolução e o carro voltar à oferta: vistoria, limpeza, abastecimento e
        /// revisão de entrega (RN-45/RN-46).
        ///
        /// É parâmetro de <b>filial</b>, e não de categoria, porque quem executa a preparação é o
        /// pátio dela: o mesmo modelo tem giro diferente num aeroporto com equipe dedicada e numa
        /// loja de rua. Em devolução one-way vale o parâmetro da filial de <b>destino</b>, que é
        /// onde o carro efetivamente é preparado — e é para lá que a RN-47 move o ativo.
        /// </summary>
        public int TempoPreparacaoMinutos { get; private set; } = PreparacaoPadraoMinutos;

        /// <summary>Duas horas — meio da faixa praticada em loja de rua. Política da casa, não norma.</summary>
        public const int PreparacaoPadraoMinutos = 120;

        /// <summary>
        /// RN-49: a filial participa do remanejamento programado de frota, mandando e recebendo
        /// veículos.
        ///
        /// Nasce <c>true</c> porque o caso normal é participar — a exceção é a loja que não tem
        /// pátio para receber carro sem contrato, o ponto de atendimento dentro de um hotel, ou a
        /// filial em abertura. Marcar a exceção é o trabalho de quem a conhece; obrigar toda filial
        /// já cadastrada a se habilitar deixaria a rede inteira sem transferência até alguém
        /// perceber.
        ///
        /// Não se confunde com <c>HabilitadaOneWay</c> (backlog A2): aquela é sobre o
        /// <b>cliente</b> devolver em outra filial e paga taxa de retorno; esta é sobre a
        /// <b>casa</b> mover o próprio ativo.
        /// </summary>
        public bool PermiteTransferencia { get; private set; } = true;

        /// <summary>
        /// Um dia. Acima disso não é preparação e sim indisponibilidade, que tem estado próprio
        /// (`EmManutencao`, `Bloqueado`) — o teto existe para barrar erro de digitação que
        /// esconderia frota inteira da oferta sem ninguém perceber.
        /// </summary>
        public const int PreparacaoMaximaMinutos = 1440;

        public ICollection<Veiculo> Veiculos { get; set; } = new List<Veiculo>();

        public ICollection<Reserva> Reserva { get; set; } = new List<Reserva>();
        public ICollection<Locacao> LocacoesRetirada { get; set; } = new List<Locacao>();
        public ICollection<Locacao> LocacoesDevolucao { get; set; } = new List<Locacao>();

        private readonly List<FotoFilial> _fotos = new();
        public IReadOnlyCollection<FotoFilial> Fotos => _fotos;

        public static Filial Criar(string nome, string cidade, Endereco endereco, int? tempoPreparacaoMinutos = null)
        {
            if (string.IsNullOrWhiteSpace(nome))
                throw new InvalidOperationException("Nome é obrigatório");
            if (string.IsNullOrWhiteSpace(cidade))
                throw new InvalidOperationException("cidade é obrigatório");

            if (endereco == null)
                throw new InvalidOperationException("endereço não pode ser nulo");

            var filial = new Filial
            {
                Nome = nome,
                Cidade = cidade,
                Ativo = true,
                Endereco = Endereco.Criar(endereco.Logradouro, endereco.Numero, endereco.Bairro, endereco.Cidade, endereco.Estado, endereco.Cep, endereco.Complemento)
            };

            filial.DefinirTempoPreparacao(tempoPreparacaoMinutos ?? PreparacaoPadraoMinutos);

            return filial;
        }

        /// <param name="tempoPreparacaoMinutos">
        /// <c>null</c> mantém o valor atual. É proposital: quem não conhece o parâmetro não pode
        /// zerá-lo sem querer, e um zero acidental colocaria o carro na oferta no instante da
        /// devolução — exatamente o defeito que a RN-44 existe para corrigir.
        /// </param>
        public void Atualizar(string nome, string cidade, Endereco endereco, int? tempoPreparacaoMinutos = null)
        {
            if (string.IsNullOrWhiteSpace(nome))
                throw new InvalidOperationException("Nome é obrigatório");
            if (string.IsNullOrWhiteSpace(cidade))
                throw new InvalidOperationException("cidade é obrigatório");
            if (endereco == null)
                throw new InvalidOperationException("endereço não pode ser nulo");

            // validado junto com as outras guardas: preparação inválida não pode deixar o resto da
            // filial já alterado
            if (tempoPreparacaoMinutos.HasValue)
                ValidarPreparacao(tempoPreparacaoMinutos.Value);

            Nome = nome;
            Cidade = cidade;
            Endereco.Atualizar(endereco.Logradouro, endereco.Numero, endereco.Bairro, endereco.Cidade, endereco.Estado, endereco.Cep, endereco.Complemento);

            if (tempoPreparacaoMinutos.HasValue)
                TempoPreparacaoMinutos = tempoPreparacaoMinutos.Value;
        }

        /// <summary>
        /// Zero é permitido — é a filial que declara não ter preparação — mas é decisão consciente,
        /// não padrão.
        /// </summary>
        public void DefinirTempoPreparacao(int minutos)
        {
            ValidarPreparacao(minutos);
            TempoPreparacaoMinutos = minutos;
        }

        /// <summary>
        /// Instante em que a preparação começada em <paramref name="inicioPreparacao"/> vence e o
        /// carro pode voltar à oferta sozinho (RN-45, parte automática).
        ///
        /// O cálculo mora aqui, e não em quem varre o pátio, porque o parâmetro é da filial: quem
        /// conhece o prazo é quem o define.
        /// </summary>
        public DateTime PrazoDePreparacao(DateTime inicioPreparacao)
            => inicioPreparacao.AddMinutes(TempoPreparacaoMinutos);

        /// <summary>
        /// O prazo já venceu? Comparação por instante, não por dia — preparação se mede em minutos.
        ///
        /// Filial com preparação zero vence no mesmo instante da devolução: é a filial declarando
        /// que não tem preparação, a escolha consciente que <see cref="DefinirTempoPreparacao"/>
        /// permite.
        /// </summary>
        public bool PreparacaoVencida(DateTime inicioPreparacao, DateTime agora)
            => agora >= PrazoDePreparacao(inicioPreparacao);

        private static void ValidarPreparacao(int minutos)
        {
            if (minutos < 0)
                throw new InvalidOperationException("Tempo de preparação não pode ser negativo");

            if (minutos > PreparacaoMaximaMinutos)
                throw new InvalidOperationException(
                    $"Tempo de preparação não pode passar de {PreparacaoMaximaMinutos} minutos ({PreparacaoMaximaMinutos / 60}h)");
        }

        public void AdicionarFoto(List<FotoFilial> fotos)
        {
            if (fotos == null)
                throw new DomainException("Foto inválida");
            foreach (var foto in fotos)
            {
                _fotos.Add(FotoFilial.Criar(foto.NomeArquivo, foto.Raiz, foto.Diretorio, foto.Extensao, foto.QuantidadeBytes.Value));
            }                
        }
        public void RemoverFoto(int idFoto)
        {
            var foto = _fotos.FirstOrDefault(f => f.IdFoto == idFoto);
            if (foto == null)
                throw new DomainException("Foto não encontrada");
            _fotos.Remove(foto);
        }

        /// <summary>
        /// Liga ou desliga a participação da filial no remanejamento de frota (RN-49). Desligar
        /// não mexe em transferência já em trânsito: o carro que está na estrada precisa chegar em
        /// algum lugar.
        /// </summary>
        public void DefinirPermiteTransferencia(bool permite) => PermiteTransferencia = permite;

        public void Ativar()
        {
            Ativo = true;
        }
        public void Desativar()
        {
            Ativo = false;
        }
    }

}
