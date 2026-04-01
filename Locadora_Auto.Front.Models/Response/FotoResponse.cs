namespace Locadora_Auto.Front.Models.Response
{
    public class FotoResponse
    {
        public int IdFoto { get; set; }
        public int? IdEntidade { get; set; }
        public string? Entidade { get; set; }
        public string? NomeArquivo { get; set; }
        public string? Raiz { get; set; }
        public string? Diretorio { get; set; }
        public string? Extensao { get; set; }
        public DateTime DataUpload { get; set; }
        public long QuantidadeBytes { get; set; }

        // Propriedade para URL da imagem (seu endpoint para servir a imagem)
        public string UrlImagem => $"/api/v1/fotos/{IdFoto}";

        // Propriedade para formatação do tamanho
        public string TamanhoFormatado => FormatarTamanho(QuantidadeBytes);

        private string FormatarTamanho(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}