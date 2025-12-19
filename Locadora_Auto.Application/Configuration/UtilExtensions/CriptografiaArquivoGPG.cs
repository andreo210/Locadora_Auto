//using System.Diagnostics;

//namespace Modelo.Infra.Services.UtilExtensions
//{
//    /// <summary>
//    /// Classe utilitária para criptografar e descriptografar arquivos usando GPG com metadados.
//    /// O nome original do arquivo é salvo separadamente em um arquivo .meta.
//    /// </summary>
//    public static class CriptografiaArquivoGPG
//    {
//        /// <summary>
//        /// Criptografa um arquivo usando GPG (AES256) e salva o nome original em um arquivo .meta.
//        /// </summary>
//        /// <param name="caminhoArquivoEntrada">Caminho completo do arquivo que será criptografado.</param>
//        /// <param name="nomeOriginalArquivo">Nome original do arquivo, que será salvo no arquivo .meta.</param>
//        /// <param name="senha">Senha utilizada para criptografia simétrica via GPG.</param>
//        /// <returns>Um objeto <see cref="FileInfo"/> representando o arquivo criptografado (.gpg).</returns>
//        public static FileInfo CriptografarArquivoGPG(string caminhoArquivoEntrada, string nomeOriginalArquivo, string senha)
//        {
//            // Define o caminho do arquivo .meta que armazenará o nome original
//            string metaFile = caminhoArquivoEntrada + ".meta";

//            // Define o caminho do arquivo criptografado (.gpg)
//            string encryptedFile = caminhoArquivoEntrada + ".gpg";

//            // Salva o nome original do arquivo no arquivo .meta
//            File.WriteAllText(metaFile, nomeOriginalArquivo);

//            // Configura o processo para executar o comando GPG via linha de comando
//            var psi = new ProcessStartInfo
//            {
//                FileName = "gpg", // Comando GPG
//                Arguments = $"--yes --batch --pinentry-mode loopback --passphrase \"{senha}\" " +
//                            $"--symmetric --cipher-algo AES256 --output \"{encryptedFile}\" \"{caminhoArquivoEntrada}\"",
//                RedirectStandardError = true, // Captura erros
//                UseShellExecute = false,      // Não usa o shell do sistema
//                CreateNoWindow = true         // Oculta a janela do terminal
//            };

//            // Executa o processo de criptografia
//            using var process = Process.Start(psi);
//            process.WaitForExit();

//            // Lê a saída de erro (se houver)
//            string error = process.StandardError.ReadToEnd();

//            // Se o processo falhar, lança uma exceção com a mensagem de erro
//            if (process.ExitCode != 0)
//                throw new IOException($"Erro ao criptografar com GPG: {error}");

//            // Retorna o arquivo criptografado como FileInfo
//            return new FileInfo(encryptedFile);
//        }

//        /// <summary>
//        /// Descriptografa um arquivo .gpg usando GPG e restaura o nome original a partir do arquivo .meta.
//        /// </summary>
//        /// <param name="caminhoArquivoEntrada">Caminho completo do arquivo criptografado (.gpg).</param>
//        /// <param name="diretorioSaida">Diretório onde o arquivo restaurado será salvo.</param>
//        /// <param name="senha">Senha utilizada para descriptografia via GPG.</param>
//        /// <returns>Um objeto <see cref="FileInfo"/> representando o arquivo restaurado com seu nome original.</returns>
//        public static FileInfo DescriptografarArquivoGPG(string caminhoArquivoEntrada, string diretorioSaida, string senha)
//        {
//            // Localiza o arquivo .meta correspondente ao .gpg
//            string metaFile = caminhoArquivoEntrada.Replace(".gpg", ".meta");

//            // Verifica se o arquivo .meta existe
//            if (!File.Exists(metaFile))
//                throw new FileNotFoundException("Arquivo .meta não encontrado.", metaFile);

//            // Lê o nome original do arquivo a partir do .meta
//            string nomeOriginal = File.ReadAllText(metaFile).Trim();

//            // Define o caminho completo onde o arquivo restaurado será salvo
//            string caminhoSaida = Path.Combine(diretorioSaida, nomeOriginal);

//            // Configura o processo para executar o comando GPG de descriptografia
//            var psi = new ProcessStartInfo
//            {
//                FileName = "gpg", // Comando GPG
//                Arguments = $"--yes --batch --pinentry-mode loopback --passphrase \"{senha}\" " +
//                            $"--output \"{caminhoSaida}\" --decrypt \"{caminhoArquivoEntrada}\"",
//                RedirectStandardError = true, // Captura erros
//                UseShellExecute = false,      // Não usa o shell do sistema
//                CreateNoWindow = true         // Oculta a janela do terminal
//            };

//            // Executa o processo de descriptografia
//            using var process = Process.Start(psi);
//            process.WaitForExit();

//            // Lê a saída de erro (se houver)
//            string error = process.StandardError.ReadToEnd();

//            // Se o processo falhar, lança uma exceção com a mensagem de erro
//            if (process.ExitCode != 0)
//                throw new IOException($"Erro ao descriptografar com GPG: {error}");

//            // Retorna o arquivo restaurado como FileInfo
//            return new FileInfo(caminhoSaida);
//        }
//    }
//}


using System;
using System.Diagnostics;
using System.IO;

namespace Locadora_Auto.Application.Configuration.UtilExtensions
{
    /// <summary>
    /// Classe utilitária para criptografar e descriptografar arquivos usando GPG (AES256),
    /// preservando o nome original com base na extensão .gpg.
    /// </summary>
    public static class CriptografiaArquivoGPG
    {
        /// <summary>
        /// Criptografa um arquivo usando GPG (AES256) com criptografia simétrica.
        /// O arquivo de saída terá a extensão .gpg.
        /// </summary>
        /// <param name="caminhoArquivoEntrada">Caminho completo do arquivo original.</param>
        /// <param name="senha">Senha usada para criptografia.</param>
        /// <returns>Arquivo criptografado (.gpg).</returns>
        public static FileInfo CriptografarArquivo(string caminhoArquivoEntrada, string senha)
        {
            if (!File.Exists(caminhoArquivoEntrada))
                throw new FileNotFoundException("Arquivo de entrada não encontrado.", caminhoArquivoEntrada);

            string caminhoArquivoSaida = caminhoArquivoEntrada + ".gpg";

            var psi = new ProcessStartInfo
            {
                FileName = "gpg",
                Arguments = $"--yes --batch --pinentry-mode loopback --passphrase \"{senha}\" " +
                            $"--symmetric --cipher-algo AES256 --output \"{caminhoArquivoSaida}\" \"{caminhoArquivoEntrada}\"",
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var processo = Process.Start(psi);
            processo.WaitForExit();

            string erro = processo.StandardError.ReadToEnd();
            if (processo.ExitCode != 0)
                throw new IOException($"Erro ao criptografar com GPG: {erro}");

            return new FileInfo(caminhoArquivoSaida);
        }

        /// <summary>
        /// Descriptografa um arquivo .gpg usando GPG e restaura o arquivo original,
        /// removendo apenas a extensão .gpg.
        /// </summary>
        /// <param name="caminhoArquivoCriptografado">Caminho completo do arquivo .gpg.</param>
        /// <param name="senha">Senha usada para descriptografia.</param>
        /// <returns>Arquivo restaurado com nome original.</returns>
        public static FileInfo DescriptografarArquivo(string caminhoArquivoCriptografado, string senha)
        {
            if (!File.Exists(caminhoArquivoCriptografado))
                throw new FileNotFoundException("Arquivo criptografado não encontrado.", caminhoArquivoCriptografado);

            if (!caminhoArquivoCriptografado.EndsWith(".gpg", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Arquivo não possui extensão .gpg.", nameof(caminhoArquivoCriptografado));

            string caminhoArquivoSaida = caminhoArquivoCriptografado.Substring(0, caminhoArquivoCriptografado.Length - 4);

            var psi = new ProcessStartInfo
            {
                FileName = "gpg",
                Arguments = $"--yes --batch --pinentry-mode loopback --passphrase \"{senha}\" " +
                            $"--output \"{caminhoArquivoSaida}\" --decrypt \"{caminhoArquivoCriptografado}\"",
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var processo = Process.Start(psi);
            processo.WaitForExit();

            string erro = processo.StandardError.ReadToEnd();
            if (processo.ExitCode != 0)
                throw new IOException($"Erro ao descriptografar com GPG: {erro}");

            return new FileInfo(caminhoArquivoSaida);
        }
    }
}
