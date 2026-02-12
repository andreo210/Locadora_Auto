namespace Locadora_Auto.Domain.Entidades
{
    public class Adicional
    {
        public int IdAdicional { get; private set; }
        public string Nome { get; private set; } = null!;
        public decimal ValorDiaria { get; private set; }
        public bool Ativo { get; private set; }

        protected Adicional() { }

        public static Adicional Criar(string nome, decimal valorDiaria)
        {
            if (valorDiaria < 0)
                throw new DomainException("Valor inválido");

            return new Adicional
            {
                Nome = nome,
                ValorDiaria = valorDiaria,
                Ativo = true
            };
        }

        public void Atualizar(string nome, decimal valorDiaria)
        {
            if (valorDiaria < 0)
                throw new DomainException("Valor inválido");

            if (string.IsNullOrEmpty(nome))
                throw new DomainException("nome inválido");
            Nome = nome;
            ValorDiaria = valorDiaria;

        }

        public void Desativar() => Ativo = false;

        public void Ativar() => Ativo = true;
    }


}
