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

        public static CategoriaVeiculo Criar(string nome, decimal valorDiaria, int limiteKm, decimal valorKmExcedente)
        {
            if (string.IsNullOrWhiteSpace(nome))
                throw new InvalidOperationException("nome é obrigatório");
            if (!decimal.IsPositive(valorDiaria))
                throw new InvalidOperationException("valorDiaria tem que ser um numero positivo");
            if (!decimal.IsPositive(valorKmExcedente))
                throw new InvalidOperationException("valorKmExcedente tem que ser um numero positivo");
            if (!int.IsPositive(limiteKm))
                throw new InvalidOperationException("limiteKm tem que ser um numero positivo");

            return new CategoriaVeiculo
            {
                Nome = nome.Trim(),
                ValorDiaria = valorDiaria,
                LimiteKm = limiteKm,
                ValorKmExcedente = valorKmExcedente
            };
        }

        public void Atualizar(string nome, decimal valorDiaria, int limiteKm, decimal valorKmExcedente)
        {
            if (string.IsNullOrWhiteSpace(nome))
                throw new InvalidOperationException("nome é obrigatório");
            if (!decimal.IsPositive(valorDiaria))
                throw new InvalidOperationException("valorDiaria tem que ser um numero positivo");
            if (!decimal.IsPositive(valorKmExcedente))
                throw new InvalidOperationException("valorKmExcedente tem que ser um numero positivo");
            if (!int.IsPositive(limiteKm))
                throw new InvalidOperationException("limiteKm tem que ser um numero positivo");
            Nome = nome.Trim();
            ValorDiaria = valorDiaria;
            LimiteKm = limiteKm;
            ValorKmExcedente = valorKmExcedente;
        }

    }


}
