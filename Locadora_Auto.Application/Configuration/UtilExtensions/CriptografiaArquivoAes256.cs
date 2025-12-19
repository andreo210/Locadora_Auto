using System.Security.Cryptography;
using System.Text;

namespace Locadora_Auto.Application.Configuration.UtilExtensions
{
    /// <summary>
    /// Classe utilitária para criptografar e descriptografar arquivos usando AES-256.
    /// O nome original do arquivo é embutido no conteúdo criptografado.
    /// </summary>
    public static class CriptografiaArquivoAes256
    {
        /// <summary>
        /// Criptografa um arquivo usando AES-256, incluindo o nome original no cabeçalho do arquivo criptografado.
        /// </summary>
        /// <param name="caminhoArquivoEntrada">Caminho completo do arquivo que será criptografado.</param>
        /// <param name="nomeOriginalArquivo">Nome original do arquivo, que será embutido no conteúdo criptografado.</param>
        /// <param name="senha">Senha utilizada para derivar a chave de criptografia via PBKDF2.</param>
        /// <returns>Arquivo criptografado com extensão ".aes".</returns>
        public static FileInfo CriptografarArquivoAes256(string caminhoArquivoEntrada, string nomeOriginalArquivo, string senha)
        {
            // Gera um salt aleatório de 16 bytes
            byte[] salt = new byte[16];
            RandomNumberGenerator.Fill(salt);

            // Deriva a chave de 256 bits usando PBKDF2 com SHA256 e 100 mil iterações
            using var kdf = new Rfc2898DeriveBytes(senha, salt, 100_000, HashAlgorithmName.SHA256);
            byte[] chave = kdf.GetBytes(32);

            // Cria uma instância de AES e configura os parâmetros
            using var aes = Aes.Create();
            aes.Key = chave;
            aes.GenerateIV(); // Gera IV aleatório
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            // Define o caminho de saída com extensão .aes
            string caminhoSaida = caminhoArquivoEntrada + ".aes";

            // Abre o arquivo original para leitura
            using var entrada = File.OpenRead(caminhoArquivoEntrada);

            // Cria o arquivo de saída criptografado
            using var saida = File.Create(caminhoSaida);

            // Escreve o salt e o IV no início do arquivo
            saida.Write(salt, 0, salt.Length);
            saida.Write(aes.IV, 0, aes.IV.Length);

            // Escreve o nome original do arquivo (tamanho + conteúdo)
            var nomeBytes = Encoding.UTF8.GetBytes(nomeOriginalArquivo);
            var tamanhoNome = BitConverter.GetBytes(nomeBytes.Length);
            saida.Write(tamanhoNome, 0, tamanhoNome.Length);
            saida.Write(nomeBytes, 0, nomeBytes.Length);

            // Cria o fluxo de criptografia e copia o conteúdo do arquivo original
            using var fluxoCriptografado = new CryptoStream(saida, aes.CreateEncryptor(), CryptoStreamMode.Write);
            entrada.CopyTo(fluxoCriptografado);

            // Retorna o arquivo criptografado
            return new FileInfo(caminhoSaida);
        }







        /// <summary>
        /// Descriptografa um arquivo previamente criptografado com AES-256, restaurando seu conteúdo e nome original.
        /// </summary>
        /// <param name="caminhoArquivoEntrada">Caminho completo do arquivo criptografado (.aes).</param>
        /// <param name="diretorioSaida">Diretório onde o arquivo restaurado será salvo.</param>
        /// <param name="senha">Senha utilizada para derivar a chave de descriptografia via PBKDF2.</param>
        /// <returns>Arquivo restaurado com seu nome original.</returns>
        /// <exception cref="CryptographicException">Lançada se a senha estiver incorreta ou se o conteúdo estiver corrompido.</exception>
        public static FileInfo DescriptografarArquivoAes256(string caminhoArquivoEntrada, string diretorioSaida, string senha)
        {
            // Abre o arquivo criptografado para leitura
            using var entrada = File.OpenRead(caminhoArquivoEntrada);

            // Lê o salt e o IV do início do arquivo
            byte[] salt = new byte[16];
            byte[] iv = new byte[16];
            entrada.Read(salt, 0, salt.Length);
            entrada.Read(iv, 0, iv.Length);

            // Deriva a chave de 256 bits usando os mesmos parâmetros da criptografia
            using var kdf = new Rfc2898DeriveBytes(senha, salt, 100_000, HashAlgorithmName.SHA256);
            byte[] chave = kdf.GetBytes(32);

            // Lê o tamanho e o conteúdo do nome original do arquivo
            byte[] tamanhoNomeBytes = new byte[4];
            entrada.Read(tamanhoNomeBytes, 0, 4);
            int tamanhoNome = BitConverter.ToInt32(tamanhoNomeBytes, 0);

            byte[] nomeBytes = new byte[tamanhoNome];
            entrada.Read(nomeBytes, 0, tamanhoNome);
            string nomeOriginal = Encoding.UTF8.GetString(nomeBytes);

            // Define o caminho de saída com o nome original
            string caminhoSaida = Path.Combine(diretorioSaida, nomeOriginal);

            // Configura o AES para descriptografar
            using var aes = Aes.Create();
            aes.Key = chave;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            // Cria o arquivo restaurado e copia o conteúdo descriptografado
            using var saida = File.Create(caminhoSaida);
            using var fluxoDescriptografado = new CryptoStream(entrada, aes.CreateDecryptor(), CryptoStreamMode.Read);
            fluxoDescriptografado.CopyTo(saida);

            // Retorna o arquivo restaurado
            return new FileInfo(caminhoSaida);
        }
    }
}
