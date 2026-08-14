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
        /// Um dia. Acima disso não é preparação e sim indisponibilidade, que tem estado próprio
        /// (`EmManutencao`, `Indisponivel`) — o teto existe para barrar erro de digitação que
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
