using System;
using System.IO;
using Org.BouncyCastle.Bcpg;
using Org.BouncyCastle.Bcpg.OpenPgp;
using Org.BouncyCastle.Security;

namespace Locadora_Auto.Application.Configuration.UtilExtensions
{


    /// <summary>
    /// Classe utilitária para criptografia e descriptografia de arquivos usando OpenPGP com chaves públicas e privadas.
    /// Compatível com BouncyCastle >= 2.7.0.
    /// </summary>
    public static class CriptografiaArquivoPGPChave
    {
        /// <summary>
        /// Criptografa um arquivo usando chave pública (Public Key Encryption - PKE).
        /// O arquivo resultante terá extensão ".gpg" e poderá ser aberto por aplicações compatíveis com OpenPGP.
        /// </summary>
        /// <param name="caminhoArquivoEntrada">Caminho completo do arquivo a ser criptografado.</param>
        /// <param name="chavePublica">Objeto PgpPublicKey do destinatário.</param>
        /// <returns>FileInfo representando o arquivo criptografado.</returns>
        public static FileInfo CriptografarComChavePublica(string caminhoArquivoEntrada, PgpPublicKey chavePublica)
        {
            if (caminhoArquivoEntrada == null) throw new ArgumentNullException(nameof(caminhoArquivoEntrada));
            if (chavePublica == null) throw new ArgumentNullException(nameof(chavePublica));
            if (!File.Exists(caminhoArquivoEntrada))
                throw new FileNotFoundException("Arquivo de entrada não encontrado.", caminhoArquivoEntrada);

            string caminhoSaida = caminhoArquivoEntrada + ".gpg";

            using var outputStream = File.Create(caminhoSaida);

            // Cria o gerador de dados criptografados com AES256 e integridade
            var encryptedDataGenerator = new PgpEncryptedDataGenerator(
                SymmetricKeyAlgorithmTag.Aes256,
                withIntegrityPacket: true,
                random: new SecureRandom()
            );

            // Adiciona método de criptografia usando a chave pública
            encryptedDataGenerator.AddMethod(chavePublica);

            // Cria stream criptografado e escreve os dados do arquivo
            using var encryptedOut = encryptedDataGenerator.Open(outputStream, new byte[1 << 16]);
            PgpUtilities.WriteFileToLiteralData(
                encryptedOut,
                PgpLiteralData.Binary,
                new FileInfo(caminhoArquivoEntrada)
            );

            return new FileInfo(caminhoSaida);
        }

        /// <summary>
        /// Descriptografa um arquivo ".gpg" usando a chave privada do destinatário.
        /// </summary>
        /// <param name="caminhoArquivoCriptografado">Caminho do arquivo criptografado.</param>
        /// <param name="chavePrivada">Objeto PgpPrivateKey do destinatário.</param>
        /// <returns>FileInfo do arquivo descriptografado.</returns>
        public static FileInfo DescriptografarComChavePrivada(string caminhoArquivoCriptografado, PgpPrivateKey chavePrivada)
        {
            if (caminhoArquivoCriptografado == null) throw new ArgumentNullException(nameof(caminhoArquivoCriptografado));
            if (chavePrivada == null) throw new ArgumentNullException(nameof(chavePrivada));
            if (!File.Exists(caminhoArquivoCriptografado))
                throw new FileNotFoundException("Arquivo criptografado não encontrado.", caminhoArquivoCriptografado);
            if (!caminhoArquivoCriptografado.EndsWith(".gpg", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Arquivo não possui extensão .gpg.", nameof(caminhoArquivoCriptografado));

            string caminhoSaida = caminhoArquivoCriptografado.Substring(0, caminhoArquivoCriptografado.Length - 4);

            using var inputStream = File.OpenRead(caminhoArquivoCriptografado);
            using var decoderStream = PgpUtilities.GetDecoderStream(inputStream);

            var pgpFactory = new PgpObjectFactory(decoderStream);
            PgpObject pgpObject = pgpFactory.NextPgpObject();

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

            foreach (PgpEncryptedData encryptedData in encList.GetEncryptedDataObjects())
            {
                if (encryptedData is PgpPublicKeyEncryptedData pked)
                {
                    try
                    {
                        using var clearStream = pked.GetDataStream(chavePrivada);
                        var plainFactory = new PgpObjectFactory(clearStream);
                        var message = plainFactory.NextPgpObject();

                        if (message is PgpCompressedData compressedData)
                        {
                            using var compStream = compressedData.GetDataStream();
                            var of2 = new PgpObjectFactory(compStream);
                            message = of2.NextPgpObject();
                        }

                        if (message is PgpLiteralData literalData)
                        {
                            using var outputStream = File.Create(caminhoSaida);
                            using var unc = literalData.GetInputStream();
                            unc.CopyTo(outputStream);

                            return new FileInfo(caminhoSaida);
                        }
                    }
                    catch
                    {
                        // Ignora e tenta o próximo bloco criptografado
                    }
                }
            }

            throw new IOException("Não foi possível descriptografar com a chave privada fornecida.");
        }
    }
}
