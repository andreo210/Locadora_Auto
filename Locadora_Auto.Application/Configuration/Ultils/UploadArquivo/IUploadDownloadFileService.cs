using Microsoft.AspNetCore.Http;

namespace Locadora_Auto.Application.Configuration.Ultils.UploadArquivo
{
    /// <summary>
    /// Interface para serviços de upload e download de arquivos,
    /// com suporte a criptografia AES-256, GPG e arquivos simples.
    /// </summary>
    public interface IUploadDownloadFileService
    {
        /// <summary>
        /// Envia um arquivo criptografado usando AES-256, preservando o nome original.
        /// </summary>
        /// <param name="arquivo">Arquivo enviado via formulário.</param>
        /// <returns>Nome do arquivo criptografado gerado.</returns>
        Task<string> EnviarArquivoCriptografadoAsync(IFormFile arquivo);

        /// <summary>
        /// Baixa e descriptografa um arquivo criptografado com AES-256,
        /// restaurando seu conteúdo e nome original.
        /// </summary>
        /// <param name="nomeArquivo">Nome do arquivo criptografado (.aes).</param>
        /// <param name="nomeOriginalArquivo">Nome original restaurado do arquivo.</param>
        /// <param name="tipoConteudo">Tipo MIME detectado do arquivo restaurado.</param>
        /// <returns>Conteúdo binário do arquivo restaurado.</returns>
        byte[] BaixarArquivoDescriptografadoAes256(string nomeArquivo, out string nomeOriginalArquivo, out string tipoConteudo);

        /// <summary>
        /// Envia um arquivo simples, sem criptografia.
        /// </summary>
        /// <param name="arquivo">Arquivo enviado via formulário.</param>
        /// <returns>Nome do arquivo salvo.</returns>
        Task<string> EnviarArquivoSimplesAsync(IFormFile arquivo);

        /// <summary>
        /// Baixa um arquivo simples, sem descriptografia.
        /// </summary>
        /// <param name="nomeArquivo">Nome do arquivo a ser baixado.</param>
        /// <param name="tipoConteudo">Tipo MIME detectado do arquivo.</param>
        /// <returns>Conteúdo binário do arquivo.</returns>
        byte[] BaixarArquivoSimples(string nomeArquivo, out string tipoConteudo);

        /// <summary>
        /// Baixa e descriptografa um arquivo criptografado com GPG,
        /// restaurando seu conteúdo e nome original via arquivo .meta.
        /// </summary>
        /// <param name="nomeArquivo">Nome do arquivo criptografado (.gpg).</param>
        /// <param name="nomeOriginalArquivo">Nome original restaurado do arquivo.</param>
        /// <param name="tipoConteudo">Tipo MIME detectado do arquivo restaurado.</param>
        /// <returns>Conteúdo binário do arquivo restaurado.</returns>
        byte[] BaixarArquivoDescriptografadoGPG(string nomeArquivo, out string nomeOriginalArquivo, out string tipoConteudo);

        /// <summary>
        /// Envia um arquivo criptografado usando GPG, gerando também o arquivo .meta.
        /// </summary>
        /// <param name="arquivo">Arquivo enviado via formulário.</param>
        /// <returns>Nome do arquivo criptografado gerado (.gpg).</returns>
        Task<string> EnviarArquivoCriptografadoAsyncGPG(IFormFile arquivo);


        /// <summary>
        /// Realiza o upload de um arquivo e criptografa usando PGP AES256 com senha.
        /// </summary>
        /// <param name="arquivo">Arquivo enviado pelo usuário (IFormFile)</param>
        /// <returns>Nome final do arquivo criptografado (.gpg)</returns>
        Task<string> EnviarArquivoCriptografadoPGPAsync(IFormFile arquivo);


        /// <summary>
        /// Baixa um arquivo criptografado, descriptografa usando PGP e retorna o conteúdo em bytes.
        /// </summary>
        /// <param name="nomeArquivo">Nome do arquivo .gpg a ser baixado</param>
        /// <param name="nomeOriginalArquivo">Saída: nome original do arquivo descriptografado</param>
        /// <param name="tipoConteudo">Saída: tipo MIME do arquivo</param>
        /// <returns>Conteúdo do arquivo descriptografado em byte[]</returns>
        byte[] BaixarArquivoDescriptografadoPGP(string nomeArquivo, out string nomeOriginalArquivo, out string tipoConteudo);
    }

}
