using k8s.Models;
using Locadora_Auto.Api.Controllers;
using Locadora_Auto.Application.Configuration.Ultils.NotificadorServices;
using Locadora_Auto.Application.Models.Dto.Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Application.Services.FilialServices;
using Locadora_Auto.Application.Services.ImageService;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Locadora_Auto.Api.Controllers.V1.Controllers
{
    [ApiController]
    [Route("api/v1/filiais")]
    public class FiliaisController : MainController
    {
        private readonly IFilialService _filialService;
        private readonly IImageService _imageService;

        public FiliaisController(
            IFilialService filialService,
            INotificadorService notificador,
            IImageService imageService)
            : base(notificador)
        {
            _filialService = filialService;
            _imageService = imageService;
        }

        // ========================= CONSULTAS =========================



        [HttpGet]
        public async Task<IActionResult> ObterTodas(CancellationToken ct = default, [FromQuery] int pagina = 1, [FromQuery] int itensPorPagina = 10, [FromQuery] string? nome = null)
        {
            var result = await _filialService.ObterTodosPaginadoAsync(pagina, itensPorPagina, nome, ct);
            return CustomResponse(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> ObterPorId(int id, CancellationToken ct)
        {
            var filial = await _filialService.ObterPorIdAsync(id, ct);
            return CustomResponse(filial);
        }

        [HttpGet("{id:int}/fotos/{idFoto:int}")]
        public async Task<IActionResult> GetFoto(CancellationToken ct, int idFoto, int id, int? width = null, int? height = null)
        {
            var filial = await _filialService.ObterPorIdAsync(id, ct);
            if (filial == null)
                return NotFound();

            var foto = filial.Fotos?.FirstOrDefault(f => f.IdFoto == idFoto);
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

        // ========================= CRIAÇÃO =========================

        [HttpPost]
        public async Task<IActionResult> Criar(
            [FromBody] CriarFilialDto dto,
            CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return ValidationResponse(ModelState);

            var filial = await _filialService.CriarFilialAsync(dto, ct);
            return CustomResponse(filial, HttpStatusCode.Created);
        }

        // ========================= ATUALIZAÇÃO =========================

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Atualizar(
            int id,
            [FromBody] AtualizarFilialDto dto,
            CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return ValidationResponse(ModelState);

            var sucesso = await _filialService.AtualizarFilialAsync(id, dto, ct);
            return CustomResponse(sucesso, HttpStatusCode.NoContent);
        }



        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Excluir(int id, CancellationToken ct)
        {
            var sucesso = await _filialService.ExcluirFilialAsync(id, ct);
            return CustomResponse(sucesso, HttpStatusCode.NoContent);
        }


        [HttpPatch("{id:int}/ativar")]
        public async Task<IActionResult> Ativar(int id, CancellationToken ct)
        {
            var sucesso = await _filialService.AtivarFilialAsync(id, ct);
            return CustomResponse(sucesso);
        }

        [HttpPatch("{id:int}/desativar")]
        public async Task<IActionResult> Desativar(int id, CancellationToken ct)
        {
            var sucesso = await _filialService.DesativarFilialAsync(id, ct);
            return CustomResponse(sucesso);
        }

        [HttpPost("{id:int}/registrar-foto")]
        public async Task<IActionResult> RegistraFoto([FromForm] List<IFormFile>fotos, int id,CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return ValidationResponse(ModelState);

            var filial = await _filialService.RegistarFotoFilialAsync(id,fotos, ct);
            return CustomResponse(filial, HttpStatusCode.Created);
        }

        [HttpDelete("{id:int}/excluir-foto/{idFoto:int}")]
        public async Task<IActionResult> ExcluirFoto(int id, int idFoto, CancellationToken ct)
        {
            var sucesso = await _filialService.ExluirFotoFilialAsync(id, idFoto, ct);
            return CustomResponse(sucesso);
        }
    }


    public class TransferirVeiculoDto
    {
        public int VeiculoId { get; set; }
        public int FilialDestinoId { get; set; }
    }
}
