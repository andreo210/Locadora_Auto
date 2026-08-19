namespace Locadora_Auto.Domain.Entidades
{
    public class CategoriaVeiculo
    {
        public int Id { get; private set; }

        public string? Nome { get; private set; }

        public decimal ValorDiaria { get; private set; }

        public int? LimiteKm { get; private set; }

        public decimal? ValorKmExcedente { get; private set; }

        public ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
        public ICollection<Veiculo> Veiculos { get; set; } = new List<Veiculo>();
        private readonly List<FotoCategoriaVeiculo> _fotos = new();
        public IReadOnlyCollection<FotoCategoriaVeiculo> Fotos => _fotos;

        /// <param name="limiteKm">
        /// RN-08: <c>null</c> é <b>quilometragem livre</b> — o plano mais vendido no varejo, e até
        /// aqui o único que o cadastro não conseguia expressar: as colunas sempre foram anuláveis,
        /// mas a guarda exigia número, e o serviço fazia <c>.Value</c> por cima disso (500 certo
        /// para quem omitisse o campo).
        /// </param>
        /// <param name="valorKmExcedente">
        /// Obrigatório quando há limite, e ignorado quando não há: preço de km excedente sem
        /// franquia é preço que nada aciona, e guardá-lo faria a categoria parecer controlada num
        /// filtro por "tem valor de km".
        /// </param>
        public static CategoriaVeiculo Criar(string nome, decimal valorDiaria, int? limiteKm, decimal? valorKmExcedente)
        {
            ValidarCategoria(nome, valorDiaria, limiteKm, valorKmExcedente);

            return new CategoriaVeiculo
            {
                Nome = nome.Trim(),
                ValorDiaria = valorDiaria,
                LimiteKm = limiteKm,
                ValorKmExcedente = limiteKm.HasValue ? valorKmExcedente : null
            };
        }

        public void Atualizar(string nome, decimal valorDiaria, int? limiteKm, decimal? valorKmExcedente)
        {
            ValidarCategoria(nome, valorDiaria, limiteKm, valorKmExcedente);

            Nome = nome.Trim();
            ValorDiaria = valorDiaria;
            LimiteKm = limiteKm;
            ValorKmExcedente = limiteKm.HasValue ? valorKmExcedente : null;
        }

        /// <summary>
        /// RN-08 e doc 07 §4: os dois campos de quilometragem andam juntos. Limite sem preço é o
        /// "cadastro inconsistente" que bloqueia o fechamento — e bloquear na devolução, com o
        /// cliente esperando, é tarde demais para descobrir isso.
        /// </summary>
        private static void ValidarCategoria(string nome, decimal valorDiaria, int? limiteKm, decimal? valorKmExcedente)
        {
            if (string.IsNullOrWhiteSpace(nome))
                throw new InvalidOperationException("nome é obrigatório");

            if (!decimal.IsPositive(valorDiaria))
                throw new InvalidOperationException("valorDiaria tem que ser um numero positivo");

            if (!limiteKm.HasValue)
                return;

            // comparação explícita, e não int.IsPositive: aquele devolve true para zero (o contrato
            // de INumberBase trata 0 como positivo), e limite zero passaria a valer "toda a rodagem
            // é excedente" — que ninguém cadastrou de propósito
            if (limiteKm.Value <= 0)
                throw new InvalidOperationException("limiteKm tem que ser um numero positivo, ou nulo para quilometragem livre");

            if (!valorKmExcedente.HasValue || valorKmExcedente.Value <= 0)
                throw new InvalidOperationException("categoria com limite de km exige valorKmExcedente positivo");
        }

        public void AdicionarFoto(List<FotoCategoriaVeiculo> fotos)
        {
            if (fotos == null)
                throw new DomainException("Foto inválida");
            foreach (var foto in fotos)
            {
                _fotos.Add(FotoCategoriaVeiculo.Criar(foto.NomeArquivo, foto.Raiz, foto.Diretorio, foto.Extensao, foto.QuantidadeBytes.Value));
            }
        }

        public void RemoverFoto(int idFoto)
        {
            var foto = _fotos.FirstOrDefault(f => f.IdFoto == idFoto);
            if (foto == null)
                throw new DomainException("Foto não encontrada");
            _fotos.Remove(foto);
        }

    }


}
