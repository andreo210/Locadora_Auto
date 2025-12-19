namespace Locadora_Auto.Domain.Entidades
{
    public class Arquivo
    {
        public int? Id { get; set; }
        public string? Nome { get; set; }
        public string? Raiz { get; set; }
        public string? Diretorio { get; set; }
        public string? Extensao { get; set; }
        public long? QuantidadeBytes { get; set; }
    }
}
