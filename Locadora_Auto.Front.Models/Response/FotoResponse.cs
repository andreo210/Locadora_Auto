namespace Locadora_Auto.Front.Models.Response
{
    public class FotoResponse
    {        
        public int? IdFoto { get; set; }
        public int? IdEntidade { get; set; }
        public string? Entidade { get; set; }
        public string? NomeArquivo { get; set; }
        public string? Raiz { get; set; }
        public string? Diretorio { get; set; }
        public string? Extensao { get; set; }
        public DateTime DataUpload { get; set; }
        public long? QuantidadeBytes { get; set; }        
    }
}
