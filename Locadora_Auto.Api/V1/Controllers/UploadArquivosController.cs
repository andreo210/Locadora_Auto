using Locadora_Auto.Application.Configuration.Ultils.UploadArquivo;
using Locadora_Auto.Application.Configuration.Ultils.UploadArquivoDataBase;
using Locadora_Auto.Application.Configuration.UtilExtensions;
using Locadora_Auto.Application.Extensions;
using Locadora_Auto.Application.Handlers;
using Locadora_Auto.Application.Jobs;
using Locadora_Auto.Application.Models.Dto;
using Microsoft.AspNetCore.Mvc;

namespace Locadora_Auto.Api.V1.Controllers
{
    /// <summary>
    /// Controller responsável por gerenciar o envio e o recebimento de arquivos,
    /// com ou sem criptografia, via API REST.
    /// </summary>
    [ApiController]
    [Route("api/arquivos")]
    public class UploadArquivosController : ControllerBase
    {
        private readonly IUploadDownloadFileService _servicoArquivos;
        private readonly IPdfStorageService _pdfStorageService;
        private readonly ISolicitacaoService _solicitacaoService;
        private readonly IMessageCollector _messageCollector;
        /// <summary>
        /// Construtor que injeta o serviço de arquivos via injeção de dependência.
        /// </summary>
        /// <param name="servicoArquivos">Serviço responsável pelas operações de upload e download.</param>
        public UploadArquivosController(IUploadDownloadFileService servicoArquivos, IPdfStorageService pdfStorageService, ISolicitacaoService solicitacaoService, IMessageCollector messageCollector)
        {
            _servicoArquivos = servicoArquivos;
            _pdfStorageService = pdfStorageService;
            _solicitacaoService = solicitacaoService;
            _messageCollector = messageCollector;
        }

        /// <summary>
        /// Endpoint para enviar um arquivo com criptografia AES-256.
        /// </summary>
        /// <param name="arquivo">Arquivo enviado via formulário.</param>
        /// <returns>Mensagem de sucesso e nome do arquivo criptografado.</returns>
        [HttpPost("enviar/criptografadoAes256")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> EnviarCriptografado(IFormFile arquivo)
        {
            try
            {
                var cpf = "33087186855";
                if (!cpf.EhCpfValido())
                {
                    ModelState.AddModelError("Cpf", "CPF inválido");

                    return BadRequest(
                        ValidationProblemFactory.FromModelState(ModelState)
                    );
                }
                var documentos = new List<DocumentosDto>
                {
                    new DocumentosDto { File = arquivo }
                };

                if (!VerificaDocumentoExtensions.ValidarListaArquivos(documentos, ModelState))
                    return BadRequest(ValidationProblemFactory.FromModelState(ModelState));



                // Chama o serviço para criptografar e salvar o arquivo
                string nomeArquivo = await _servicoArquivos.EnviarArquivoCriptografadoAsync(arquivo);
                return Ok(new { mensagem = "Arquivo criptografado com sucesso", nomeArquivo });
            }
            catch (Exception ex)
            {
                // Retorna erro em caso de falha
                return BadRequest(new { erro = ex.Message });
            }
        }

        /// <summary>
        /// Endpoint para baixar um arquivo criptografado com AES-256, restaurando nome e tipo original.
        /// </summary>
        /// <param name="nomeArquivo">Nome do arquivo criptografado (.aes).</param>
        /// <returns>Arquivo restaurado com nome e tipo MIME original.</returns>
        [HttpGet("baixar/criptografadoAes256/{nomeArquivo}")]
        public IActionResult BaixarCriptografado(string nomeArquivo)
        {
            try
            {
                // Chama o serviço para descriptografar e retornar o conteúdo
                var conteudo = _servicoArquivos.BaixarArquivoDescriptografadoAes256(nomeArquivo, out string nomeOriginal, out string tipoConteudo);
                return File(conteudo, tipoConteudo, nomeOriginal);
            }
            catch (Exception ex)
            {
                // Retorna erro se o arquivo não for encontrado ou falhar
                return NotFound(new { erro = ex.Message });
            }
        }

        /// <summary>
        /// Endpoint para enviar um arquivo criptografado com GPG.
        /// </summary>
        /// <param name="arquivo">Arquivo enviado via formulário.</param>
        /// <returns>Mensagem de sucesso e nome do arquivo criptografado.</returns>
        [HttpPost("enviar/criptografadoGPG")]
        public async Task<IActionResult> EnviarCriptografadoGPG(IFormFile arquivo)
        {
            try
            {
                // Chama o serviço para criptografar com GPG
                string nomeArquivo = await _servicoArquivos.EnviarArquivoCriptografadoAsyncGPG(arquivo);
                return Ok(new { mensagem = "Arquivo criptografado com sucesso", nomeArquivo });
            }
            catch (Exception ex)
            {
                return BadRequest(new { erro = ex.Message });
            }
        }

        /// <summary>
        /// Endpoint para baixar um arquivo criptografado com GPG, restaurando nome e tipo original.
        /// </summary>
        /// <param name="nomeArquivo">Nome do arquivo criptografado (.gpg).</param>
        /// <returns>Arquivo restaurado com nome e tipo MIME original.</returns>
        [HttpGet("baixar/criptografadoGPG/{nomeArquivo}")]
        public IActionResult BaixarCriptografadoGPG(string nomeArquivo)
        {
            try
            {
                // Chama o serviço para descriptografar com GPG
                var conteudo = _servicoArquivos.BaixarArquivoDescriptografadoGPG(nomeArquivo, out string nomeOriginal, out string tipoConteudo);
                return File(conteudo, tipoConteudo, nomeOriginal);
            }
            catch (Exception ex)
            {
                return NotFound(new { erro = ex.Message });
            }
        }


        /// <summary>
        /// Endpoint para enviar um arquivo criptografado com PGP.
        /// </summary>
        /// <param name="arquivo">Arquivo enviado via formulário.</param>
        /// <returns>Mensagem de sucesso e nome do arquivo criptografado.</returns>
        [HttpPost("enviar/criptografadoPGP")]
        public async Task<IActionResult> EnviarCriptografadoPgp(IFormFile arquivo)
        {
            try
            {
                // Chama o serviço para criptografar com GPG
                string nomeArquivo = await _servicoArquivos.EnviarArquivoCriptografadoPGPAsync(arquivo);
                return Ok(new { mensagem = "Arquivo criptografado com sucesso", nomeArquivo });
            }
            catch (Exception ex)
            {
                return BadRequest(new { erro = ex.Message });
            }
        }

        /// <summary>
        /// Endpoint para baixar um arquivo criptografado com PGP, restaurando nome e tipo original.
        /// </summary>
        /// <param name="nomeArquivo">Nome do arquivo criptografado (.gpg).</param>
        /// <returns>Arquivo restaurado com nome e tipo MIME original.</returns>
        [HttpGet("baixar/criptografadoPGP/{nomeArquivo}")]
        public IActionResult BaixarCriptografadoPGP(string nomeArquivo)
        {
            try
            {
                // Chama o serviço para descriptografar com GPG
                var conteudo = _servicoArquivos.BaixarArquivoDescriptografadoPGP(nomeArquivo, out string nomeOriginal, out string tipoConteudo);
                return File(conteudo, tipoConteudo, nomeOriginal);
            }
            catch (Exception ex)
            {
                return NotFound(new { erro = ex.Message });
            }
        }

        /// <summary>
        /// Endpoint para enviar um arquivo simples, sem criptografia.
        /// </summary>
        /// <param name="arquivo">Arquivo enviado via formulário.</param>
        /// <returns>Mensagem de sucesso e nome do arquivo salvo.</returns>
        [HttpPost("enviar/simples")]
        public async Task<IActionResult> EnviarSimples(IFormFile arquivo)
        {
            try
            {
                // Chama o serviço para salvar o arquivo diretamente
                string nomeArquivo = await _servicoArquivos.EnviarArquivoSimplesAsync(arquivo);
                return Ok(new { mensagem = "Arquivo salvo sem criptografia", nomeArquivo });
            }
            catch (Exception ex)
            {
                return BadRequest(new { erro = ex.Message });
            }
        }

        /// <summary>
        /// Endpoint para baixar um arquivo simples, sem descriptografia.
        /// </summary>
        /// <param name="nomeArquivo">Nome do arquivo a ser baixado.</param>
        /// <returns>Arquivo original com tipo MIME detectado.</returns>
        [HttpGet("baixar/simples/{nomeArquivo}")]
        public IActionResult BaixarSimples(string nomeArquivo)
        {
            try
            {
                // Chama o serviço para retornar o conteúdo do arquivo simples
                var conteudo = _servicoArquivos.BaixarArquivoSimples(nomeArquivo, out string tipoConteudo);
                return File(conteudo, tipoConteudo, nomeArquivo);
            }
            catch (Exception ex)
            {
                return NotFound(new { erro = ex.Message });
            }
        }


        [HttpPost("upload")]
        public async Task<IActionResult> UploadPdf(IFormFile file)
        {
            try
            {
                var id = await _pdfStorageService.UploadPdfAsync(file);
                return Ok(new { Message = "Upload realizado com sucesso!", Id = id });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        // 📥 Download de PDF
        [HttpGet("download/{id}")]
        public async Task<IActionResult> DownloadPdf(Guid id)
        {
            try
            {
                var pdfBytes = await _pdfStorageService.DownloadPdfAsync(id);
                return File(pdfBytes, "application/pdf", $"documento_{id}.pdf");
            }
            catch (FileNotFoundException)
            {
                return NotFound(new { Error = "Arquivo não encontrado." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }


        //exemplo de colocar mensagem em fila e commitar apos sucesso
        [HttpPost]
        public async Task<IActionResult> Criar([FromBody] SolicitacaoDto dto)
        {
            await _solicitacaoService.CriarSolicitacaoAsync(dto);
             _messageCollector.Commit();
            return Ok();
        }
    }
}
