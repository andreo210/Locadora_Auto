namespace Locadora_Auto.Domain.Entidades
{
    public class FotoBase
    {
        public int? IdFoto { get; set; }
        public string? NomeArquivo { get; set; }
        public string? Raiz { get;set; }
        public string? Diretorio { get;  set; }
        public string? Extensao { get; set; }
        public long? QuantidadeBytes { get; set; }
        public DateTime DataUpload { get;set; }
    }
}
