using Org.BouncyCastle.Bcpg;
using Org.BouncyCastle.Bcpg.OpenPgp;
using Org.BouncyCastle.Security;

namespace Locadora_Auto.Application.Configuration.UtilExtensions
{
    /// <summary>
    /// Classe utilitária para criptografia e descriptografia de arquivos 
    /// usando o padrão OpenPGP (AES256 + senha).
    /// Compatível com a versão 2.6.2 do BouncyCastle.
    /// </summary>
    public static class CriptografiaArquivoPGP
    {


        /// <summary>
        /// Criptografa um arquivo no formato OpenPGP utilizando criptografia simétrica (senha).
        /// O algoritmo de criptografia utilizado é o AES256.
        /// O arquivo resultante terá a extensão <c>.gpg</c>.
        /// </summary>
        /// <param name="caminhoArquivoEntrada">Caminho do arquivo original a ser criptografado.</param>
        /// <param name="senha">Senha que será usada na criptografia (PBE - Password Based Encryption).</param>
        /// <returns>Um objeto <see cref="FileInfo"/> representando o arquivo criptografado.</returns>
        /// <exception cref="ArgumentNullException">Se o caminho ou senha forem nulos.</exception>
        /// <exception cref="FileNotFoundException">Se o arquivo de entrada não for encontrado.</exception>
        public static FileInfo CriptografarArquivo(string caminhoArquivoEntrada, string senha)
        {
            // Validações de parâmetros
            if (caminhoArquivoEntrada == null) throw new ArgumentNullException(nameof(caminhoArquivoEntrada));
            if (senha == null) throw new ArgumentNullException(nameof(senha));
            if (!File.Exists(caminhoArquivoEntrada))
                throw new FileNotFoundException("Arquivo de entrada não encontrado.", caminhoArquivoEntrada);

            // Define o caminho do arquivo de saída (.gpg)
            string caminhoArquivoSaida = caminhoArquivoEntrada + ".gpg";

            // Cria o arquivo de saída no disco
            using (var outputStream = File.Create(caminhoArquivoSaida))
            {
                // Inicializa o gerador de dados criptografados com AES256
                var encryptedDataGenerator = new PgpEncryptedDataGenerator(
                    SymmetricKeyAlgorithmTag.Aes256,   // algoritmo de criptografia
                    withIntegrityPacket: true,         // adiciona pacote de integridade
                    random: new SecureRandom()         // gerador de números aleatórios seguro
                );

                // Na versão 2.6.2 do BouncyCastle é necessário passar um HashAlgorithmTag junto da senha
                // SHA1 é o mais compatível/aceito
                encryptedDataGenerator.AddMethod(senha.ToCharArray(), HashAlgorithmTag.Sha1);

                // Abre o stream de criptografia e escreve o arquivo original como LiteralData
                using (var encryptedOut = encryptedDataGenerator.Open(outputStream, new byte[1 << 16]))
                {
                    PgpUtilities.WriteFileToLiteralData(
                        encryptedOut,
                        PgpLiteralData.Binary,                 // grava os dados em formato binário
                        new FileInfo(caminhoArquivoEntrada)    // arquivo original
                    );
                }
            }

            return new FileInfo(caminhoArquivoSaida);
        }

        /// <summary>
        /// Descriptografa um arquivo no formato OpenPGP (gerado por <see cref="CriptografarArquivo"/>)
        /// utilizando a senha fornecida.
        /// O arquivo restaurado será gravado com o mesmo nome do original (sem a extensão <c>.gpg</c>).
        /// </summary>
        /// <param name="caminhoArquivoCriptografado">Caminho completo do arquivo criptografado (.gpg).</param>
        /// <param name="senha">Senha usada para a descriptografia.</param>
        /// <returns>Um objeto <see cref="FileInfo"/> representando o arquivo descriptografado.</returns>
        /// <exception cref="ArgumentNullException">Se o caminho ou senha forem nulos.</exception>
        /// <exception cref="FileNotFoundException">Se o arquivo criptografado não for encontrado.</exception>
        /// <exception cref="ArgumentException">Se o arquivo não tiver extensão .gpg.</exception>
        /// <exception cref="IOException">Se não for possível descriptografar com a senha fornecida.</exception>
        public static FileInfo DescriptografarArquivo(string caminhoArquivoCriptografado, string senha)
        {
            // Validações de parâmetros
            if (caminhoArquivoCriptografado == null) throw new ArgumentNullException(nameof(caminhoArquivoCriptografado));
            if (senha == null) throw new ArgumentNullException(nameof(senha));
            if (!File.Exists(caminhoArquivoCriptografado))
                throw new FileNotFoundException("Arquivo criptografado não encontrado.", caminhoArquivoCriptografado);

            // Garante que a extensão seja .gpg
            if (!caminhoArquivoCriptografado.EndsWith(".gpg", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Arquivo não possui extensão .gpg.", nameof(caminhoArquivoCriptografado));

            // Remove a extensão .gpg para gerar o nome do arquivo restaurado
            string caminhoArquivoSaida = caminhoArquivoCriptografado.Substring(0, caminhoArquivoCriptografado.Length - 4);

            // Abre o arquivo criptografado e inicializa o parser de objetos PGP
            using (var inputStream = File.OpenRead(caminhoArquivoCriptografado))
            using (var decoderStream = PgpUtilities.GetDecoderStream(inputStream))
            {
                var pgpFactory = new PgpObjectFactory(decoderStream);
                PgpObject pgpObject = pgpFactory.NextPgpObject();

                // O objeto raiz pode ser diretamente uma lista de dados criptografados
                // ou pode haver outro objeto antes
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

                // Percorre todos os objetos criptografados encontrados
                foreach (PgpEncryptedData encryptedData in encList.GetEncryptedDataObjects())
                {
                    // Só interessa a criptografia por senha (PBE)
                    if (encryptedData is PgpPbeEncryptedData pbe)
                    {
                        try
                        {
                            // Obtém o stream descriptografado usando a senha
                            using var clearStream = pbe.GetDataStream(senha.ToCharArray());
                            var plainFactory = new PgpObjectFactory(clearStream);
                            var message = plainFactory.NextPgpObject();

                            // Se os dados vierem comprimidos, é necessário descompactar
                            if (message is PgpCompressedData compressedData)
                            {
                                using var compStream = compressedData.GetDataStream();
                                var of2 = new PgpObjectFactory(compStream);
                                message = of2.NextPgpObject();
                            }

                            // O objeto final esperado é do tipo LiteralData (conteúdo real do arquivo)
                            if (message is PgpLiteralData literalData)
                            {
                                // Restaura o arquivo no disco
                                using var outputStream = File.Create(caminhoArquivoSaida);
                                using var unc = literalData.GetInputStream();
                                unc.CopyTo(outputStream);

                                return new FileInfo(caminhoArquivoSaida);
                            }
                        }
                        catch
                        {
                            // Ignora e tenta o próximo objeto criptografado, caso exista
                        }
                    }
                }
            }

            // Se nenhum bloco foi descriptografado com sucesso, lança exceção
            throw new IOException("Não foi possível descriptografar com a senha fornecida.");
        }
    }
}
