namespace Locadora_Auto.Domain.Entidades
{
    public class Seguro
    {
        public int IdSeguro { get; set; }
        public string Nome { get; set; } = null!;
        public decimal ValorDiaria { get; set; }
        public decimal Franquia { get; set; }
        public string Cobertura { get; set; } = null!;
        public bool Ativo { get; set; } = true;
    }
}

