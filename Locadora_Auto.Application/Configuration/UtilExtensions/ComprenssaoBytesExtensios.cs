namespace Locadora_Auto.Application.Configuration.UtilExtensions
{
    using Microsoft.AspNetCore.Http;
    using System;
    using System.IO;
    using System.IO.Compression;
    using System.Threading.Tasks;

    /// <summary>
    /// Classe utilitária para compressão e descompressão de dados usando GZip.
    /// </summary>
    public static class ConversaoCompressaoBytesExtensions
    {
        /// <summary>
        /// Comprime o conteúdo de um arquivo enviado via formulário (IFormFile) usando GZip.
        /// </summary>
        /// <param name="file">Arquivo a ser comprimido.</param>
        /// <returns>Array de bytes contendo os dados comprimidos.</returns>
        /// <exception cref="ArgumentException">Lançado se o arquivo for nulo ou estiver vazio.</exception>
        public static async Task<byte[]> CompressBytesAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("Arquivo inválido.");

            // Copia o conteúdo do arquivo para um MemoryStream
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            byte[] input = memoryStream.ToArray();

            // Cria um novo MemoryStream para armazenar os dados comprimidos
            using var output = new MemoryStream();
            // Usa GZipStream para comprimir os dados
            using (var gzip = new GZipStream(output, CompressionLevel.Optimal, leaveOpen: true))
            {
                gzip.Write(input, 0, input.Length);
            }

            // Retorna os dados comprimidos como array de bytes
            return output.ToArray();
        }

        /// <summary>
        /// Descomprime um array de bytes que foi comprimido com GZip.
        /// </summary>
        /// <param name="input">Array de bytes comprimido.</param>
        /// <returns>Array de bytes original descomprimido.</returns>
        public static byte[] DecompressBytes(byte[] input)
        {
            // Cria um MemoryStream com os dados comprimidos
            using var inputStream = new MemoryStream(input);
            // Usa GZipStream para descomprimir os dados
            using var gzip = new GZipStream(inputStream, CompressionMode.Decompress);
            using var outputStream = new MemoryStream();

            // Copia os dados descomprimidos para o outputStream
            gzip.CopyTo(outputStream);

            // Retorna os dados descomprimidos como array de bytes
            return outputStream.ToArray();
        }
    }

}
