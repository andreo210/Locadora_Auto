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

        #region Parâmetros do fechamento (doc 07 §9)

        // Os sete abaixo são política da casa, não exigência legal, e ficam na filial pelo mesmo
        // motivo do TempoPreparacaoMinutos: quem conhece o número é quem opera a praça. Preço de
        // combustível e custo de limpeza variam de cidade para cidade; tolerância e percentual de
        // hora excedente na prática se repetem em toda a rede, e é para isso que existe o default.
        //
        // NENHUM é lido ainda — o backlog A2/A3 é só o dado. Quem os consome é a apuração
        // (A5–A8), e lá vale este recorte:
        //   · termo de contrato (tolerância, hora excedente)  → filial de RETIRADA, que vendeu;
        //   · custo de execução (combustível, limpeza, one-way) → filial de DEVOLUÇÃO, que gastou.

        /// <summary>
        /// RN-21: a filial aceita receber contrato aberto em outra filial (devolução one-way).
        ///
        /// Nasce <c>true</c> porque hoje o sistema já aceita one-way em qualquer filial — o modelo
        /// tem <c>IdFilialDevolucao</c> livre desde sempre. Nascer <c>false</c> faria o A8, ao
        /// entrar, bloquear no balcão um serviço que a casa vende hoje, em toda a rede, até alguém
        /// habilitar filial por filial. Marcar a exceção é trabalho de quem a conhece.
        ///
        /// Não se confunde com <see cref="PermiteTransferencia"/>: aquela é a <b>casa</b> movendo o
        /// próprio ativo; esta é o <b>cliente</b> devolvendo longe de onde retirou, e pagando por
        /// isso.
        /// </summary>
        public bool HabilitadaOneWay { get; private set; } = true;

        /// <summary>
        /// RN-21: quanto a devolução em filial diferente da de retirada custa ao cliente. É valor
        /// da filial de <b>destino</b> — é ela que fica com um carro que não vendeu e precisa
        /// recolocá-lo. O doc 07 §9 só recomenda matriz origem × destino acima de 4–5 filiais.
        ///
        /// Zero é válido e significa one-way de cortesia, não parâmetro esquecido: o A8 é que
        /// decide se avisa quando cobra zero.
        /// </summary>
        public decimal TaxaRetornoOneWay { get; private set; }

        /// <summary>
        /// RN-03: atraso até este limite não vira cobrança. Fila de balcão e trânsito são rotina —
        /// cobrar cinco minutos custa mais em atrito do que rende.
        /// </summary>
        public int ToleranciaMinutos { get; private set; } = ToleranciaPadraoMinutos;

        /// <summary>
        /// RN-04: fração da diária cobrada por hora excedente iniciada, depois da tolerância. Com
        /// o teto de 1 diária da RN-05 no lugar, a hora excedente nunca produz conta maior que
        /// prorrogar o contrato.
        /// </summary>
        public decimal PercentualHoraExcedente { get; private set; } = PercentualHoraExcedentePadrao;

        /// <summary>
        /// RN-15: preço do litro usado para repor o tanque no regime full-to-full.
        ///
        /// Nasce zero de propósito, e zero <b>não</b> é "combustível de graça" — é parâmetro não
        /// configurado. Vale aqui a mesma escolha da RN-14 sobre tanque não cadastrado: melhor
        /// perder a cobrança que inventar número. Quem trata isso é o A6.
        /// </summary>
        public decimal PrecoLitroCombustivel { get; private set; }

        /// <summary>
        /// RN-15: o que a casa cobra pelo serviço de abastecer, uma vez por contrato e só quando
        /// há litro a repor.
        /// </summary>
        public decimal TaxaServicoAbastecimento { get; private set; }

        /// <summary>
        /// RN-23: valor fixo da limpeza especial. Só entra no fechamento com registro na vistoria
        /// de devolução e ao menos uma foto — a foto é a defesa da cobrança, não formalidade.
        /// </summary>
        public decimal ValorLimpezaEspecial { get; private set; }

        /// <summary>Trinta minutos — equilíbrio dominante no Brasil (doc 07 §9).</summary>
        public const int ToleranciaPadraoMinutos = 30;

        /// <summary>
        /// Um dia. Tolerância maior que o ciclo de 24h da RN-01 engoliria a diária seguinte
        /// inteira, e aí não é tolerância: é contrato de graça.
        /// </summary>
        public const int ToleranciaMaximaMinutos = 1440;

        /// <summary>
        /// Um terço da diária (doc 07 §9). Não é 1/3 exato porque decimal não representa a
        /// dízima — e não precisa ser: o teto de 1 diária da RN-05 corta o acumulado antes de a
        /// quarta casa decimal significar centavo em qualquer diária praticável.
        /// </summary>
        public const decimal PercentualHoraExcedentePadrao = 0.3333m;

        #endregion Parâmetros do fechamento

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

        /// <summary>
        /// Ajusta os parâmetros de fechamento da filial (doc 07 §9). <b>Todo argumento nulo mantém
        /// o valor atual</b> — a mesma regra do <c>tempoPreparacaoMinutos</c> do
        /// <see cref="Atualizar"/>, e pelo mesmo motivo: quem não conhece o parâmetro não pode
        /// zerá-lo sem querer, e um zero acidental aqui vale dinheiro em toda devolução da praça.
        ///
        /// Valida tudo antes de atribuir qualquer coisa, para que um parâmetro recusado não deixe
        /// os outros já gravados.
        /// </summary>
        public void DefinirParametrosFinanceiros(
            bool? habilitadaOneWay = null,
            decimal? taxaRetornoOneWay = null,
            int? toleranciaMinutos = null,
            decimal? percentualHoraExcedente = null,
            decimal? precoLitroCombustivel = null,
            decimal? taxaServicoAbastecimento = null,
            decimal? valorLimpezaEspecial = null)
        {
            if (toleranciaMinutos.HasValue)
            {
                if (toleranciaMinutos.Value < 0)
                    throw new InvalidOperationException("Tolerância não pode ser negativa");
                if (toleranciaMinutos.Value > ToleranciaMaximaMinutos)
                    throw new InvalidOperationException(
                        $"Tolerância não pode passar de {ToleranciaMaximaMinutos} minutos ({ToleranciaMaximaMinutos / 60}h)");
            }

            // acima de 1 a hora excedente custaria mais que a diária inteira, o que a RN-05 existe
            // justamente para impedir — o teto lá embaixo não salvaria um parâmetro absurdo aqui
            if (percentualHoraExcedente.HasValue &&
                (percentualHoraExcedente.Value <= 0 || percentualHoraExcedente.Value > 1))
                throw new InvalidOperationException("Percentual de hora excedente deve estar entre 0 (exclusivo) e 1");

            ValidarNaoNegativo(taxaRetornoOneWay, "Taxa de retorno one-way");
            ValidarNaoNegativo(precoLitroCombustivel, "Preço do litro de combustível");
            ValidarNaoNegativo(taxaServicoAbastecimento, "Taxa de serviço de abastecimento");
            ValidarNaoNegativo(valorLimpezaEspecial, "Valor da limpeza especial");

            if (habilitadaOneWay.HasValue) HabilitadaOneWay = habilitadaOneWay.Value;
            if (taxaRetornoOneWay.HasValue) TaxaRetornoOneWay = taxaRetornoOneWay.Value;
            if (toleranciaMinutos.HasValue) ToleranciaMinutos = toleranciaMinutos.Value;
            if (percentualHoraExcedente.HasValue) PercentualHoraExcedente = percentualHoraExcedente.Value;
            if (precoLitroCombustivel.HasValue) PrecoLitroCombustivel = precoLitroCombustivel.Value;
            if (taxaServicoAbastecimento.HasValue) TaxaServicoAbastecimento = taxaServicoAbastecimento.Value;
            if (valorLimpezaEspecial.HasValue) ValorLimpezaEspecial = valorLimpezaEspecial.Value;
        }

        private static void ValidarNaoNegativo(decimal? valor, string nome)
        {
            if (valor.HasValue && valor.Value < 0)
                throw new InvalidOperationException($"{nome} não pode ser negativo");
        }

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
