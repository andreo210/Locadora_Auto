namespace Locadora_Auto.Domain.Entidades
{
    public class Endereco
    {
        public int IdEndereco { get; set; }

        public string Logradouro { get; set; } = null!;
        public string Numero { get; set; } = null!;
        public string? Complemento { get; set; }
        public string Bairro { get; set; } = null!;
        public string Cidade { get; set; } = null!;
        public string Estado { get; set; } = null!;
        public string Cep { get; set; } = null!;

        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    }
}
