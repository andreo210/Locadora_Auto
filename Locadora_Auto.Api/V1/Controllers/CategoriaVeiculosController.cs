using Locadora_Auto.Api.Controllers;
using Locadora_Auto.Application.Configuration.Ultils.NotificadorServices;
using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Application.Services.CategoriaVeiculosServices;
using Locadora_Auto.Application.Services.ImageService;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Locadora_Auto.Api.V1.Controllers
{
    [ApiController]
    [Route("api/v1/categorias-veiculos")]
    public class CategoriaVeiculosController : MainController
    {
        private readonly ICategoriaVeiculoService _service;
        private readonly IImageService _imageService;

        public CategoriaVeiculosController(ICategoriaVeiculoService service, INotificadorService notificador, IImageService imageService) : base(notificador)
        {
            _service = service;
            _imageService = imageService;
        }

        // ========================= CRIAR =========================
        [HttpPost]
        public async Task<IActionResult> Criar([FromBody] CriarCategoriaVeiculoDto dto, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return ValidationResponse(ModelState);

            var result = await _service.CriarAsync(dto, ct);

            return CustomResponse(result, HttpStatusCode.Created);
        }

        // ========================= OBTER POR ID =========================
        [HttpGet("{id:int}")]
        public async Task<IActionResult> ObterPorId(int id, CancellationToken ct)
        {
            var result = await _service.ObterPorIdAsync(id, ct);

            return CustomResponse(result);
        }

        [HttpGet("{id:int}/fotos/{idFoto:int}")]
        public async Task<IActionResult> GetFoto(CancellationToken ct, int idFoto, int id, int? width = null, int? height = null)
        {
            var categoria = await _service.ObterPorIdAsync(id, ct);
            if (categoria == null)
                return NotFound();

            var foto = categoria.Fotos.FirstOrDefault(f => f.IdFoto == idFoto);
            if (foto == null)
                return NotFound();

            var caminho = Path.Combine(foto.Diretorio, foto.NomeArquivo);
            if (!System.IO.File.Exists(caminho))
                return NotFound();

            // Redimensiona a imagem
            var bytes = await _imageService.RedimensionarAsync(caminho, width, height);

            var contentType = foto.Extensao?.ToLower() switch
            {
                "jpg" or "jpeg" => "image/jpeg",
                "png" => "image/png",
                "gif" => "image/gif",
                "webp" => "image/webp",
                _ => "application/octet-stream"
            };

            return File(bytes, contentType);
        }

        // ========================= OBTER TODAS =========================
        [HttpGet]
        public async Task<IActionResult> ObterTodas(CancellationToken ct = default, [FromQuery] int pagina = 1, [FromQuery] int itensPorPagina = 10)
        {
            var result = await _service.ObterTodosPaginadoAsync(pagina, itensPorPagina, ct);

            return CustomResponse(result);
        }

        // ========================= ATUALIZAR =========================
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Atualizar(int id, [FromBody] AtualizarCategoriaVeiculoDto dto, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return ValidationResponse(ModelState);

            await _service.AtualizarAsync(id, dto, ct);

            return CustomResponse();
        }

        // ========================= EXCLUIR =========================
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Excluir(int id, CancellationToken ct)
        {
            await _service.ExcluirAsync(id, ct);

            return CustomResponse(null, HttpStatusCode.NoContent);
        }


        [HttpPost("{id:int}/registrar-foto")]
        public async Task<IActionResult> RegistraFoto([FromForm] List<IFormFile> fotos, int id, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return ValidationResponse(ModelState);

            var filial = await _service.RegistarFotoCategoriaAsync(id, fotos, ct);
            return CustomResponse(filial, HttpStatusCode.Created);
        }

        [HttpDelete("{id:int}/excluir-foto/{idFoto:int}")]
        public async Task<IActionResult> ExcluirFoto(int id, int idFoto, CancellationToken ct)
        {

            var filial = await _service.ExluirFotoCategoriaAsync(id, idFoto, ct);
            return CustomResponse(filial, HttpStatusCode.Created);
        }
    }
}
