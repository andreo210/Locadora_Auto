using Locadora_Auto.Api.Controllers;
using Locadora_Auto.Application.Configuration.Ultils.NotificadorServices;
using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Application.Services.CategoriaVeiculosServices;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Locadora_Auto.Api.V1.Controllers
{
    [ApiController]
    [Route("api/v1/categorias-veiculos")]
    public class CategoriaVeiculosController : MainController
    {
        private readonly ICategoriaVeiculoService _service;

        public CategoriaVeiculosController(
            ICategoriaVeiculoService service,
            INotificadorService notificador)
            : base(notificador)
        {
            _service = service;
        }

        // ========================= CRIAR =========================
        [HttpPost]
        public async Task<IActionResult> Criar(
            [FromBody] CriarCategoriaVeiculoDto dto,
            CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return ValidationResponse(ModelState);

            var result = await _service.CriarAsync(dto, ct);

            return CustomResponse(result, HttpStatusCode.Created);
        }

        // ========================= OBTER POR ID =========================
        [HttpGet("{id:int}")]
        public async Task<IActionResult> ObterPorId(
            int id,
            CancellationToken ct)
        {
            var result = await _service.ObterPorIdAsync(id, ct);

            return CustomResponse(result);
        }

        // ========================= OBTER TODAS =========================
        [HttpGet]
        public async Task<IActionResult> ObterTodas(
            CancellationToken ct)
        {
            var result = await _service.ObterTodosAsync(ct);

            return CustomResponse(result);
        }

        // ========================= ATUALIZAR =========================
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Atualizar(
            int id,
            [FromBody] AtualizarCategoriaVeiculoDto dto,
            CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return ValidationResponse(ModelState);

            await _service.AtualizarAsync(id, dto, ct);

            return CustomResponse();
        }

        // ========================= EXCLUIR =========================
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Excluir(
            int id,
            CancellationToken ct)
        {
            await _service.ExcluirAsync(id, ct);

            return CustomResponse(null, HttpStatusCode.NoContent);
        }
    }
}
