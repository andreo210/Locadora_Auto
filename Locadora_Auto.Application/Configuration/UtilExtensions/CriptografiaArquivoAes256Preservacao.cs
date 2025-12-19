using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modelo.Infra.Services.UtilExtensions
{
    using System.Security.Cryptography;
    using System.IO;

    namespace Modelo.Infra.Services.UtilExtensions
    {
        /// <summary>
        /// Criptografa e descriptografa arquivos usando AES-256,
        /// preservando a integridade binária do conteúdo original.
        /// </summary>
        public static class CriptografiaArquivoAes256Preserva
        {
            /// <summary>
            /// Criptografa um arquivo usando AES-256 sem modificar o conteúdo original.
            /// </summary>
            /// <param name="caminhoArquivoEntrada">Caminho do arquivo original.</param>
            /// <param name="senha">Senha para derivar a chave de criptografia.</param>
            /// <returns>Arquivo criptografado com extensão ".aes".</returns>
            public static FileInfo Criptografar(string caminhoArquivoEntrada, string senha)
            {
                byte[] salt = new byte[16];
                RandomNumberGenerator.Fill(salt);

                using var kdf = new Rfc2898DeriveBytes(senha, salt, 100_000, HashAlgorithmName.SHA256);
                byte[] chave = kdf.GetBytes(32);

                using var aes = Aes.Create();
                aes.Key = chave;
                aes.GenerateIV();
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                string caminhoSaida = caminhoArquivoEntrada + ".aes";

                using var entrada = File.OpenRead(caminhoArquivoEntrada);
                using var saida = File.Create(caminhoSaida);

                // Escreve salt e IV no início do arquivo criptografado
                saida.Write(salt, 0, salt.Length);
                saida.Write(aes.IV, 0, aes.IV.Length);

                using var fluxoCriptografado = new CryptoStream(saida, aes.CreateEncryptor(), CryptoStreamMode.Write);
                entrada.CopyTo(fluxoCriptografado);

                // Salva nome e extensão originais em um arquivo .meta
                string nomeOriginal = Path.GetFileName(caminhoArquivoEntrada);
                string caminhoMeta = Path.ChangeExtension(caminhoSaida, ".meta");
                File.WriteAllText(caminhoMeta, nomeOriginal);

                return new FileInfo(caminhoSaida);
            }


            /// <summary>
            /// Descriptografa um arquivo criptografado com AES-256,
            /// restaurando o conteúdo original exatamente como era.
            /// </summary>
            /// <param name="caminhoArquivoEntrada">Caminho do arquivo criptografado (.aes).</param>
            /// <param name="diretorioSaida">Diretório onde o arquivo restaurado será salvo.</param>
            /// <param name="nomeRestaurado">Nome do arquivo restaurado.</param>
            /// <param name="senha">Senha para derivar a chave de descriptografia.</param>
            /// <returns>Arquivo restaurado com conteúdo idêntico ao original.</returns>
            public static FileInfo Descriptografar(string caminhoArquivoEntrada, string diretorioSaida, string senha)
            {
                using var entrada = File.OpenRead(caminhoArquivoEntrada);

                byte[] salt = new byte[16];
                byte[] iv = new byte[16];
                entrada.Read(salt, 0, salt.Length);
                entrada.Read(iv, 0, iv.Length);

                using var kdf = new Rfc2898DeriveBytes(senha, salt, 100_000, HashAlgorithmName.SHA256);
                byte[] chave = kdf.GetBytes(32);

                using var aes = Aes.Create();
                aes.Key = chave;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                // Lê o nome original do arquivo a partir do .meta
                string caminhoMeta = Path.ChangeExtension(caminhoArquivoEntrada, ".meta");
                if (!File.Exists(caminhoMeta))
                    throw new FileNotFoundException("Arquivo de metadados (.meta) não encontrado.", caminhoMeta);

                string nomeOriginal = File.ReadAllText(caminhoMeta).Trim();
                string caminhoSaida = Path.Combine(diretorioSaida, nomeOriginal);

                using var saida = File.Create(caminhoSaida);
                using var fluxoDescriptografado = new CryptoStream(entrada, aes.CreateDecryptor(), CryptoStreamMode.Read);
                fluxoDescriptografado.CopyTo(saida);

                return new FileInfo(caminhoSaida);
            }

        }
    }

}
