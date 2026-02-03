namespace Locadora_Auto.Domain.Entidades
{
    public class Seguro
    {
        public int IdSeguro { get; private set; }
        public string Descricao { get; private set; } = null!;
        public string Nome { get; private set; } = null!;
        public decimal ValorDiaria { get; private set; }
        public decimal Franquia { get; private set; }
        public string Cobertura { get; private set; } = null!;
        public bool Ativo { get; private set; } = true;

        protected Seguro() { } // EF
        public static Seguro Criar(string nome,string descricao, decimal valorDiaria, decimal franquia, string cobertura)
        {
            if (string.IsNullOrWhiteSpace(nome))
                throw new InvalidOperationException("Nome do seguro é obrigatório.");
            if (string.IsNullOrWhiteSpace(descricao))
                throw new InvalidOperationException("Descricao do seguro é obrigatório.");
            if (valorDiaria <= 0)
                throw new InvalidOperationException("Valor da diária deve ser maior que zero.");
            if (franquia < 0)
                throw new InvalidOperationException("Franquia não pode ser negativa.");
            if (string.IsNullOrWhiteSpace(cobertura))
                throw new InvalidOperationException("Cobertura do seguro é obrigatória.");
            return new Seguro
            {
                Nome = nome,
                Descricao = descricao,
                ValorDiaria = valorDiaria,
                Franquia = franquia,
                Cobertura = cobertura,
                Ativo = true
            };
        }
        public void Atualizar(string nome,string descricao, decimal valorDiaria, decimal franquia, string cobertura)
        {
            if (string.IsNullOrWhiteSpace(nome))
                throw new InvalidOperationException("Nome do seguro é obrigatório.");
            if (string.IsNullOrWhiteSpace(descricao))
                throw new InvalidOperationException("Descricao do seguro é obrigatório.");
            if (valorDiaria <= 0)
                throw new InvalidOperationException("Valor da diária deve ser maior que zero.");
            if (franquia < 0)
                throw new InvalidOperationException("Franquia não pode ser negativa.");
            if (string.IsNullOrWhiteSpace(cobertura))
                throw new InvalidOperationException("Cobertura do seguro é obrigatória.");
            Nome = nome;
            ValorDiaria = valorDiaria;
            Franquia = franquia;
            Cobertura = cobertura;
        }
        public void Desativar()
        {
            Ativo = false;
        }

        public void Ativar()
        {
            Ativo = true;
        }
    }
}

