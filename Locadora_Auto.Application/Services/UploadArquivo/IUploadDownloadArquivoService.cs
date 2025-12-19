using Locadora_Auto.Domain.Entidades;
using Microsoft.AspNetCore.Http;

namespace Locadora_Auto.Application.Services.UploadArquivo
{
    /// <summary>
    /// Interface para serviços de upload e download de arquivos,
    /// com suporte a criptografia AES-256, GPG e arquivos simples.
    /// </summary>
    public interface IUploadDownloadFileService
    {
        /// <summary>
        /// Envia um arquivo simples
        /// </summary>
        /// <param name="arquivo">Arquivo enviado via formulário.</param>
        /// <returns>Nome do arquivo salvo.</returns>
        Task<Arquivo> EnviarArquivoSimplesAsync(IFormFile arquivo, string cpf);

        /// <summary>
        /// Baixa um arquivo simples
        /// </summary>
        /// <param name="nomeArquivo">Nome do arquivo a ser baixado.</param>
        /// <param name="tipoConteudo">Tipo MIME detectado do arquivo.</param>
        /// <returns>Conteúdo binário do arquivo.</returns>
        byte[] BaixarArquivoSimples(string nomeArquivo, out string tipoConteudo);
    }
}
