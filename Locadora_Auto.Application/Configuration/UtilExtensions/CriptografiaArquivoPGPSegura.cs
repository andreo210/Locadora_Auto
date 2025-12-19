using System;
using System.IO;
using Org.BouncyCastle.Bcpg;
using Org.BouncyCastle.Bcpg.OpenPgp;
using Org.BouncyCastle.Security;

namespace Locadora_Auto.Application.Configuration.UtilExtensions
{
    /// <summary>
    /// Classe utilitária para criptografia e descriptografia de arquivos usando OpenPGP.
    /// Usa criptografia simétrica AES-256 com derivação de chave baseada em senha (SHA-256).
    /// Inclui limpeza automática da senha da memória após uso.
    /// </summary>
    public static class CriptografiaArquivoPGPSegura
    {
        /// <summary>
        /// Criptografa um arquivo usando senha (Password-Based Encryption - PBE) com AES-256 e SHA-256.
        /// O arquivo criptografado terá extensão ".gpg".
        /// </summary>
        /// <param name="caminhoArquivoEntrada">Caminho completo do arquivo a ser criptografado.</param>
        /// <param name="senha">Senha usada para criptografia.</param>
        /// <returns>FileInfo representando o arquivo criptografado.</returns>
        /// <exception cref="ArgumentNullException">Se o caminho ou senha forem nulos.</exception>
        /// <exception cref="FileNotFoundException">Se o arquivo de entrada não existir.</exception>
        public static FileInfo CriptografarArquivo(string caminhoArquivoEntrada, string senha)
        {
            // Valida parâmetros
            if (caminhoArquivoEntrada == null) throw new ArgumentNullException(nameof(caminhoArquivoEntrada));
            if (senha == null) throw new ArgumentNullException(nameof(senha));
            if (!File.Exists(caminhoArquivoEntrada))
                throw new FileNotFoundException("Arquivo de entrada não encontrado.", caminhoArquivoEntrada);

            // Caminho de saída adicionando extensão ".gpg"
            string caminhoArquivoSaida = caminhoArquivoEntrada + ".gpg";

            // Abre o arquivo de saída
            using var outputStream = File.Create(caminhoArquivoSaida);

            // Cria gerador de dados criptografados
            var encryptedDataGenerator = new PgpEncryptedDataGenerator(
                SymmetricKeyAlgorithmTag.Aes256,  // AES-256 simétrico
                withIntegrityPacket: true,        // Adiciona verificação de integridade (MDC)
                random: new SecureRandom()        // Gerador seguro de números aleatórios
            );

            // Converte senha em char[] para permitir limpeza da memória após uso
            char[] senhaChars = senha.ToCharArray();
            try
            {
                // Adiciona método de criptografia baseado em senha com derivação SHA-256
                encryptedDataGenerator.AddMethod(senhaChars, HashAlgorithmTag.Sha256);

                // Abre stream criptografado com buffer de 64 KB
                using var encryptedOut = encryptedDataGenerator.Open(outputStream, new byte[1 << 16]);

                // Escreve os dados do arquivo original como PgpLiteralData (binário)
                PgpUtilities.WriteFileToLiteralData(
                    encryptedOut,
                    PgpLiteralData.Binary,
                    new FileInfo(caminhoArquivoEntrada)
                );
            }
            finally
            {
                // Limpa a senha da memória imediatamente após uso
                Array.Clear(senhaChars, 0, senhaChars.Length);
            }

            return new FileInfo(caminhoArquivoSaida);
        }

        /// <summary>
        /// Descriptografa um arquivo ".gpg" usando a senha fornecida.
        /// Retorna o arquivo original restaurado.
        /// </summary>
        /// <param name="caminhoArquivoCriptografado">Caminho do arquivo criptografado.</param>
        /// <param name="senha">Senha usada na criptografia original.</param>
        /// <returns>FileInfo representando o arquivo restaurado.</returns>
        /// <exception cref="ArgumentNullException">Se o caminho ou senha forem nulos.</exception>
        /// <exception cref="FileNotFoundException">Se o arquivo criptografado não existir.</exception>
        /// <exception cref="ArgumentException">Se o arquivo não tiver extensão .gpg.</exception>
        /// <exception cref="IOException">Se não for possível descriptografar com a senha fornecida.</exception>
        public static FileInfo DescriptografarArquivo(string caminhoArquivoCriptografado, string senha)
        {
            // Valida parâmetros
            if (caminhoArquivoCriptografado == null) throw new ArgumentNullException(nameof(caminhoArquivoCriptografado));
            if (senha == null) throw new ArgumentNullException(nameof(senha));
            if (!File.Exists(caminhoArquivoCriptografado))
                throw new FileNotFoundException("Arquivo criptografado não encontrado.", caminhoArquivoCriptografado);

            // Verifica extensão ".gpg"
            if (!caminhoArquivoCriptografado.EndsWith(".gpg", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Arquivo não possui extensão .gpg.", nameof(caminhoArquivoCriptografado));

            // Remove ".gpg" para gerar arquivo de saída
            string caminhoArquivoSaida = caminhoArquivoCriptografado.Substring(0, caminhoArquivoCriptografado.Length - 4);

            using var inputStream = File.OpenRead(caminhoArquivoCriptografado);
            using var decoderStream = PgpUtilities.GetDecoderStream(inputStream);

            var pgpFactory = new PgpObjectFactory(decoderStream);
            PgpObject pgpObject = pgpFactory.NextPgpObject();

            // Obtém lista de objetos criptografados
            PgpEncryptedDataList encList;
            if (pgpObject is PgpEncryptedDataList)
            {
                encList = (PgpEncryptedDataList)pgpObject;
            }
            else
            {
                pgpObject = pgpFactory.NextPgpObject();
                encList = pgpObject as PgpEncryptedDataList ?? throw new IOException("Formato PGP inválido.");
            }

            // Converte senha em char[] para limpeza posterior
            char[] senhaChars = senha.ToCharArray();
            try
            {
                foreach (PgpEncryptedData encryptedData in encList.GetEncryptedDataObjects())
                {
                    if (encryptedData is PgpPbeEncryptedData pbe)
                    {
                        try
                        {
                            // Tenta descriptografar usando a senha
                            using var clearStream = pbe.GetDataStream(senhaChars);
                            var plainFactory = new PgpObjectFactory(clearStream);
                            var message = plainFactory.NextPgpObject();

                            // Se houver compressão, descomprime
                            if (message is PgpCompressedData compressedData)
                            {
                                using var compStream = compressedData.GetDataStream();
                                var of2 = new PgpObjectFactory(compStream);
                                message = of2.NextPgpObject();
                            }

                            // Extrai dados literais (conteúdo original)
                            if (message is PgpLiteralData literalData)
                            {
                                using var outputStream = File.Create(caminhoArquivoSaida);
                                using var unc = literalData.GetInputStream();
                                unc.CopyTo(outputStream);

                                return new FileInfo(caminhoArquivoSaida);
                            }
                        }
                        catch
                        {
                            // Ignora falha e tenta próximo bloco (senha incorreta ou bloco inválido)
                        }
                    }
                }
            }
            finally
            {
                // Limpa a senha da memória imediatamente
                Array.Clear(senhaChars, 0, senhaChars.Length);
            }

            // Se nenhum bloco descriptografado com sucesso, lança exceção
            throw new IOException("Não foi possível descriptografar com a senha fornecida.");
        }
    }
}
