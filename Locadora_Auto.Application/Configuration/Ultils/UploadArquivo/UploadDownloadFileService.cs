using Locadora_Auto.Application.Configuration.UtilExtensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Modelo.Infra.Services.UtilExtensions.Modelo.Infra.Services.UtilExtensions;

namespace Locadora_Auto.Application.Configuration.Ultils.UploadArquivo
{
    /// <summary>
    /// Serviço responsável por realizar upload e download de arquivos,
    /// com suporte a criptografia AES-256 e GPG, além de arquivos simples.
    /// </summary>
    public class UploadDownloadFileService : IUploadDownloadFileService
    {
        private readonly string _caminhoCriptografado; // Pasta onde os arquivos criptografados são armazenados
        private readonly string _caminhoSimples;       // Pasta onde os arquivos simples (sem criptografia) são armazenados
        private readonly string _senha;                // Senha usada para criptografia simétrica


        /// <summary>
        /// Construtor que injeta as configurações da aplicação via IConfiguration.
        /// </summary>
        public UploadDownloadFileService(IConfiguration config)
        {
            // Define os caminhos com base nas configurações do appsettings.json
            _caminhoCriptografado = Path.Combine(Directory.GetCurrentDirectory(), config["FileStorage:EncryptedPath"]);
            _caminhoSimples = Path.Combine(Directory.GetCurrentDirectory(), config["FileStorage:PlainPath"]);
            //_senha = VaultService.ObterChaveAsync("Andre", "Recadastro", "2026").Result; 
            _senha = config["CryptoPassword"];
            // Garante que os diretórios existem
            Directory.CreateDirectory(_caminhoCriptografado);
            Directory.CreateDirectory(_caminhoSimples);
        }

        /// <summary>
        /// Realiza o upload de um arquivo criptografado com AES-256.
        /// </summary>
        //public async Task<string> EnviarArquivoCriptografadoAsync(IFormFile arquivo)
        //{
        //    if (arquivo == null || arquivo.Length == 0)
        //        throw new ArgumentException("Arquivo inválido.");

        //    // Extrai o nome original do arquivo
        //    string nomeOriginal = Path.GetFileName(arquivo.FileName);

        //    // Cria um arquivo temporário para salvar o conteúdo recebido
        //    string caminhoTemporario = Path.GetTempFileName();
        //    using (var stream = new FileStream(caminhoTemporario, FileMode.Create))
        //    {
        //        await arquivo.CopyToAsync(stream);
        //    }

        //    // Criptografa o arquivo usando AES-256
        //    var arquivoCriptografado = CriptografiaArquivoAes256.CriptografarArquivoAes256(caminhoTemporario, nomeOriginal, _senha);

        //    // Define o nome final e move o arquivo criptografado para a pasta de destino
        //    string nomeFinal = nomeOriginal + ".aes";
        //    string caminhoFinal = Path.Combine(_caminhoCriptografado, nomeFinal);
        //    File.Move(arquivoCriptografado.FullName, caminhoFinal);

        //    // Remove o arquivo temporário original
        //    File.Delete(caminhoTemporario);

        //    return nomeFinal;
        //}

        public async Task<string> EnviarArquivoCriptografadoAsync(IFormFile arquivo)
        {
            if (arquivo == null || arquivo.Length == 0)
                throw new ArgumentException("Arquivo inválido.");

            // Extrai o nome original do arquivo
            string nomeOriginal = Path.GetFileName(arquivo.FileName);

            // Cria um arquivo temporário para salvar o conteúdo recebido
            string caminhoTemporario = Path.GetTempFileName();
            using (var stream = new FileStream(caminhoTemporario, FileMode.Create))
            {
                await arquivo.CopyToAsync(stream);
            }

            // Criptografa o arquivo usando AES-256
            var arquivoCriptografado = CriptografiaArquivoAes256Preserva.Criptografar(caminhoTemporario, _senha);

            // Salva o nome original no .meta
            string caminhoMetaOrigem = Path.ChangeExtension(arquivoCriptografado.FullName, ".meta");
            File.WriteAllText(caminhoMetaOrigem, nomeOriginal);

            // Define o nome final e move o arquivo criptografado para a pasta de destino
            string nomeFinal = nomeOriginal + ".aes";
            string caminhoFinal = Path.Combine(_caminhoCriptografado, nomeFinal);
            File.Move(arquivoCriptografado.FullName, caminhoFinal);

            // Move o .meta para a mesma pasta
            string caminhoMetaDestino = Path.Combine(_caminhoCriptografado, Path.GetFileNameWithoutExtension(nomeFinal) + ".meta");
            File.Move(caminhoMetaOrigem, caminhoMetaDestino);

            // Remove o arquivo temporário original
            File.Delete(caminhoTemporario);

            return nomeFinal;
        }



        /// <summary>
        /// Realiza o download de um arquivo criptografado com AES-256, restaurando seu conteúdo e nome original.
        /// </summary>
        public byte[] BaixarArquivoDescriptografadoAes256(string nomeArquivoCriptografado, out string nomeOriginalArquivo, out string tipoConteudo)
        {
            string caminhoCriptografado = Path.Combine(_caminhoCriptografado, nomeArquivoCriptografado);
            if (!File.Exists(caminhoCriptografado))
                throw new FileNotFoundException("Arquivo criptografado não encontrado.", caminhoCriptografado);

            // Cria uma pasta temporária para restaurar o arquivo
            string pastaTemporaria = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(pastaTemporaria);

            // Descriptografa o arquivo
            //var arquivoDescriptografado = CriptografiaArquivoAes256.DescriptografarArquivoAes256(caminhoCriptografado, pastaTemporaria, _senha);
            var arquivoDescriptografado = CriptografiaArquivoAes256Preserva.Descriptografar(caminhoCriptografado, pastaTemporaria, _senha);

            // Lê o conteúdo do arquivo restaurado
            byte[] conteudoArquivo = File.ReadAllBytes(arquivoDescriptografado.FullName);
            nomeOriginalArquivo = Path.GetFileName(arquivoDescriptografado.FullName);
            tipoConteudo = ObterTipoMime(nomeOriginalArquivo);

            // Remove o arquivo restaurado e a pasta temporária
            File.Delete(arquivoDescriptografado.FullName);
            Directory.Delete(pastaTemporaria, recursive: true);

            return conteudoArquivo;
        }

        /// <summary>
        /// Realiza o upload de um arquivo simples, sem criptografia.
        /// </summary>
        public async Task<string> EnviarArquivoSimplesAsync(IFormFile arquivo)
        {
            if (arquivo == null || arquivo.Length == 0)
                throw new ArgumentException("Arquivo inválido.");

            string nomeArquivo = Path.GetFileName(arquivo.FileName);
            string caminhoCompleto = Path.Combine(_caminhoSimples, nomeArquivo);

            using (var stream = new FileStream(caminhoCompleto, FileMode.Create))
            {
                await arquivo.CopyToAsync(stream);
            }

            return nomeArquivo;
        }

        /// <summary>
        /// Realiza o download de um arquivo simples, sem descriptografia.
        /// </summary>
        public byte[] BaixarArquivoSimples(string nomeArquivo, out string tipoConteudo)
        {
            string caminhoCompleto = Path.Combine(_caminhoSimples, nomeArquivo);
            if (!File.Exists(caminhoCompleto))
                throw new FileNotFoundException("Arquivo não encontrado.");

            tipoConteudo = ObterTipoMime(nomeArquivo);
            return File.ReadAllBytes(caminhoCompleto);
        }

        /// <summary>
        /// Detecta o tipo MIME com base na extensão do arquivo.
        /// </summary>
        private static string ObterTipoMime(string nomeArquivo)
        {
            var provider = new FileExtensionContentTypeProvider();
            return provider.TryGetContentType(nomeArquivo, out string tipo)
                ? tipo
                : "application/octet-stream"; // Tipo genérico caso não seja reconhecido
        }

        /// <summary>
        /// Realiza o download de um arquivo criptografado com GPG, restaurando seu nome original via .meta.
        /// </summary>
        public byte[] BaixarArquivoDescriptografadoGPG(string nomeArquivo, out string nomeOriginalArquivo, out string tipoConteudo)
        {
            string caminhoCriptografado = Path.Combine(_caminhoCriptografado, nomeArquivo);
            if (!File.Exists(caminhoCriptografado))
                throw new FileNotFoundException("Arquivo não encontrado.");

            string pastaTemporaria = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(pastaTemporaria);

            // Descriptografa usando GPG
            //var arquivoDescriptografado = CriptografiaArquivoGPG.DescriptografarArquivoGPG(caminhoCriptografado, pastaTemporaria, _senha);//com meta
            var arquivoDescriptografado = CriptografiaArquivoGPG.DescriptografarArquivo(caminhoCriptografado, _senha);

            byte[] conteudoArquivo = File.ReadAllBytes(arquivoDescriptografado.FullName);
            nomeOriginalArquivo = Path.GetFileName(arquivoDescriptografado.FullName);
            tipoConteudo = ObterTipoMime(nomeOriginalArquivo);

            File.Delete(arquivoDescriptografado.FullName);
            Directory.Delete(pastaTemporaria, recursive: true);

            return conteudoArquivo;
        }

        /// <summary>
        /// Realiza o upload de um arquivo criptografado com GPG, salvando também o arquivo .meta.
        /// </summary>
        public async Task<string> EnviarArquivoCriptografadoAsyncGPG(IFormFile arquivo)
        {
            if (arquivo == null || arquivo.Length == 0)
                throw new ArgumentException("Arquivo inválido.");

            string nomeOriginal = Path.GetFileName(arquivo.FileName);
            //string caminhoTemporario = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + Path.GetExtension(nomeOriginal));
            string caminhoTemporario = Path.Combine(Path.GetTempPath(), nomeOriginal);
            using (var stream = new FileStream(caminhoTemporario, FileMode.Create))
            {
                await arquivo.CopyToAsync(stream);
            }

            // Criptografa usando GPG
            // var arquivoCriptografado = CriptografiaArquivoGPG.CriptografarArquivoGPG(caminhoTemporario, nomeOriginal, _senha);
            //var arquivoCriptografado = CriptografiaArquivoGPG.CriptografarArquivo(caminhoTemporario, _senha);
            var arquivoCriptografado = CriptografiaArquivoGPG.CriptografarArquivo(caminhoTemporario, _senha);
            string nomeFinal = Path.GetFileName(arquivoCriptografado.FullName);
            string caminhoFinal = Path.Combine(_caminhoCriptografado, nomeFinal);

            File.Move(arquivoCriptografado.FullName, caminhoFinal, overwrite: true);

            // Move o .meta para o destino
            string caminhoMeta = caminhoTemporario + ".meta";
            string caminhoMetaFinal = Path.Combine(_caminhoCriptografado, nomeFinal.Replace(".gpg", ".meta"));
            if (File.Exists(caminhoMeta))
            {
                File.Move(caminhoMeta, caminhoMetaFinal, overwrite: true);
            }

            File.Delete(caminhoTemporario);

            return nomeFinal;
        }

        /// <summary>
        /// Realiza o upload de um arquivo e criptografa usando PGP AES256 com senha.
        /// </summary>
        /// <param name="arquivo">Arquivo enviado pelo usuário (IFormFile)</param>
        /// <returns>Nome final do arquivo criptografado (.gpg)</returns>
        public async Task<string> EnviarArquivoCriptografadoPGPAsync(IFormFile arquivo)
        {
            if (arquivo == null || arquivo.Length == 0)
                throw new ArgumentException("Arquivo inválido.");

            string nomeOriginal = Path.GetFileName(arquivo.FileName);
            string caminhoTemporario = Path.Combine(Path.GetTempPath(), nomeOriginal);

            // Salva temporariamente o arquivo no disco
            using (var stream = new FileStream(caminhoTemporario, FileMode.Create))
            {
                await arquivo.CopyToAsync(stream);
            }

            // Criptografa o arquivo temporário usando a classe PGP segura
            var arquivoCriptografado = CriptografiaArquivoPGPSegura.CriptografarArquivo(caminhoTemporario, _senha);

            string nomeFinal = Path.GetFileName(arquivoCriptografado.FullName);
            string caminhoFinal = Path.Combine(_caminhoCriptografado, nomeFinal);

            // Move o arquivo criptografado para a pasta final
            File.Move(arquivoCriptografado.FullName, caminhoFinal, overwrite: true);

            // Remove o arquivo temporário
            File.Delete(caminhoTemporario);

            return nomeFinal;
        }





        /// <summary>
        /// Baixa um arquivo criptografado, descriptografa usando PGP e retorna o conteúdo em bytes.
        /// </summary>
        /// <param name="nomeArquivo">Nome do arquivo .gpg a ser baixado</param>
        /// <param name="nomeOriginalArquivo">Saída: nome original do arquivo descriptografado</param>
        /// <param name="tipoConteudo">Saída: tipo MIME do arquivo</param>
        /// <returns>Conteúdo do arquivo descriptografado em byte[]</returns>
        public byte[] BaixarArquivoDescriptografadoPGP(string nomeArquivo, out string nomeOriginalArquivo, out string tipoConteudo)
        {
            string caminhoCriptografado = Path.Combine(_caminhoCriptografado, nomeArquivo);
            if (!File.Exists(caminhoCriptografado))
                throw new FileNotFoundException("Arquivo não encontrado.", caminhoCriptografado);

            // Descriptografa o arquivo usando a classe PGP segura
            var arquivoDescriptografado = CriptografiaArquivoPGPSegura.DescriptografarArquivo(caminhoCriptografado, _senha);

            byte[] conteudoArquivo = File.ReadAllBytes(arquivoDescriptografado.FullName);
            nomeOriginalArquivo = Path.GetFileName(arquivoDescriptografado.FullName);
            tipoConteudo = ObterTipoArquivo(nomeOriginalArquivo);

            // Limpa arquivos temporários
            File.Delete(arquivoDescriptografado.FullName);

            return conteudoArquivo;
        }



        /// <summary>
        /// Exemplo simples de obtenção do tipo MIME baseado na extensão do arquivo.
        /// </summary>
        private string ObterTipoArquivo(string nomeArquivo)
        {
            string ext = Path.GetExtension(nomeArquivo).ToLowerInvariant();
            return ext switch
            {
                ".txt" => "text/plain",
                ".pdf" => "application/pdf",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                _ => "application/octet-stream"
            };
        }


        /// <summary>
        /// Recebe um arquivo via IFormFile, criptografa usando chave pública e salva no armazenamento.
        /// </summary>
        /// <param name="arquivo">Arquivo enviado pelo usuário</param>
        /// <returns>Nome do arquivo salvo criptografado</returns>
        //public async Task<string> EnviarArquivoCriptografadoAsyncPgpChave(IFormFile arquivo)
        //{
        //    if (arquivo == null || arquivo.Length == 0)
        //        throw new ArgumentException("Arquivo inválido.", nameof(arquivo));

        //    string nomeOriginal = Path.GetFileName(arquivo.FileName);
        //    string caminhoTemporario = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + Path.GetExtension(nomeOriginal));

        //    // Salva arquivo temporário
        //    await using (var stream = new FileStream(caminhoTemporario, FileMode.Create))
        //    {
        //        await arquivo.CopyToAsync(stream);
        //    }

        //    // Criptografa usando chave pública
        //    var arquivoCriptografado = CriptografiaArquivoPGPChave.CriptografarComChavePublica(caminhoTemporario, _senha);

        //    string nomeFinal = Path.GetFileName(arquivoCriptografado.FullName);
        //    string caminhoFinal = Path.Combine(_caminhoCriptografado, nomeFinal);

        //    // Move arquivo criptografado para destino final
        //    File.Move(arquivoCriptografado.FullName, caminhoFinal, overwrite: true);

        //    // Remove arquivo temporário
        //    File.Delete(caminhoTemporario);

        //    return nomeFinal;
        //}

        /// <summary>
        /// Baixa um arquivo criptografado do armazenamento, descriptografa usando chave privada e retorna os bytes.
        /// </summary>
        /// <param name="nomeArquivo">Nome do arquivo criptografado (.gpg)</param>
        /// <returns>Conteúdo descriptografado do arquivo</returns>
        //public byte[] BaixarArquivoDescriptografadoPgpChave(string nomeArquivo)
        //{
        //    string caminhoArquivo = Path.Combine(_caminhoCriptografado, nomeArquivo);
        //    if (!File.Exists(caminhoArquivo))
        //        throw new FileNotFoundException("Arquivo não encontrado.", caminhoArquivo);

        //    string pastaTemporaria = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        //    Directory.CreateDirectory(pastaTemporaria);

        //    try
        //    {
        //        // Descriptografa o arquivo usando a chave privada
        //        var arquivoDescriptografado = CriptografiaArquivoPGPChave.DescriptografarComChavePrivada(caminhoArquivo, _chavePrivada);

        //        byte[] conteudoArquivo = File.ReadAllBytes(arquivoDescriptografado.FullName);
        //        File.Delete(arquivoDescriptografado.FullName); // limpa temporário

        //        return conteudoArquivo;
        //    }
        //    finally
        //    {
        //        Directory.Delete(pastaTemporaria, recursive: true);
        //    }
        //}
    }
}
