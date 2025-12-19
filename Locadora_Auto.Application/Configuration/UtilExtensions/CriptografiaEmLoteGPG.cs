using System;
using System.Diagnostics;
using System.IO;

namespace Locadora_Auto.Application.Configuration.UtilExtensions
{
    /// <summary>
    /// Classe utilitária para criptografar e descriptografar arquivos em lote usando GPG com AES256.
    /// </summary>
    public static class CriptografiaEmLoteGPG
    {
        /// <summary>
        /// Criptografa todos os arquivos de uma pasta usando GPG com AES256.
        /// Os arquivos criptografados são salvos com a extensão .gpg no mesmo diretório.
        /// </summary>
        /// <param name="pastaOrigem">Caminho da pasta contendo os arquivos originais.</param>
        /// <param name="senha">Senha utilizada para criptografia simétrica.</param>
        public static void CriptografarTodos(string pastaOrigem, string senha)
        {
            // Obtém todos os arquivos da pasta (não inclui subpastas)
            var arquivos = Directory.GetFiles(pastaOrigem);

            foreach (var arquivo in arquivos)
            {
                // Define o nome do arquivo criptografado (.gpg)
                string criptografado = arquivo + ".gpg";

                // Configura o processo para executar o comando GPG
                var psi = new ProcessStartInfo
                {
                    FileName = "gpg", // Comando GPG
                    Arguments = $"--yes --batch --pinentry-mode loopback --passphrase \"{senha}\" " +
                                $"--symmetric --cipher-algo AES256 --output \"{criptografado}\" \"{arquivo}\"",
                    RedirectStandardError = true, // Captura erros
                    UseShellExecute = false,      // Não usa o shell do sistema
                    CreateNoWindow = true         // Oculta a janela do terminal
                };

                // Executa o processo de criptografia
                using var process = Process.Start(psi);
                process.WaitForExit();

                // Lê a saída de erro (se houver)
                string error = process.StandardError.ReadToEnd();

                // Verifica se houve erro na execução
                if (process.ExitCode != 0)
                {
                    Console.WriteLine($"❌ Erro ao criptografar: {Path.GetFileName(arquivo)} → {error}");
                }
                else
                {
                    Console.WriteLine($"✅ Criptografado: {Path.GetFileName(arquivo)}");
                }
            }
        }

        /// <summary>
        /// Descriptografa todos os arquivos .gpg de uma pasta, restaurando com o nome original.
        /// Os arquivos restaurados são salvos em uma pasta de destino.
        /// </summary>
        /// <param name="pastaOrigem">Caminho da pasta contendo os arquivos .gpg.</param>
        /// <param name="pastaDestino">Caminho da pasta onde os arquivos restaurados serão salvos.</param>
        /// <param name="senha">Senha utilizada para descriptografia simétrica.</param>
        public static void DescriptografarTodos(string pastaOrigem, string pastaDestino, string senha)
        {
            // Garante que a pasta de destino existe
            Directory.CreateDirectory(pastaDestino);

            // Obtém todos os arquivos .gpg da pasta de origem
            var arquivos = Directory.GetFiles(pastaOrigem, "*.gpg");

            foreach (var arquivoGpg in arquivos)
            {
                // Remove a extensão .gpg para obter o nome original
                string nomeOriginal = Path.GetFileNameWithoutExtension(arquivoGpg);

                // Define o caminho completo do arquivo restaurado
                string restaurado = Path.Combine(pastaDestino, nomeOriginal);

                // Configura o processo para executar o comando GPG de descriptografia
                var psi = new ProcessStartInfo
                {
                    FileName = "gpg", // Comando GPG
                    Arguments = $"--yes --batch --pinentry-mode loopback --passphrase \"{senha}\" " +
                                $"--output \"{restaurado}\" --decrypt \"{arquivoGpg}\"",
                    RedirectStandardError = true, // Captura erros
                    UseShellExecute = false,      // Não usa o shell do sistema
                    CreateNoWindow = true         // Oculta a janela do terminal
                };

                // Executa o processo de descriptografia
                using var process = Process.Start(psi);
                process.WaitForExit();

                // Lê a saída de erro (se houver)
                string error = process.StandardError.ReadToEnd();

                // Verifica se houve erro na execução
                if (process.ExitCode != 0)
                {
                    Console.WriteLine($"❌ Erro ao descriptografar: {Path.GetFileName(arquivoGpg)} → {error}");
                }
                else
                {
                    Console.WriteLine($"✅ Restaurado: {Path.GetFileName(restaurado)}");
                }
            }
        }
    }
}
