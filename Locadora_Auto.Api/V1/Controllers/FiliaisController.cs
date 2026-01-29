using Locadora_Auto.Application.Configuration.Ultils.NotificadorServices;
using Locadora_Auto.Application.Models.Dto.Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Application.Services.FilialServices;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Locadora_Auto.Api.V1.Controllers
{
    [ApiController]
    [Route("api/v1/filiais")]
    public class FiliaisController : MainController
    {
        private readonly IFilialService _filialService;

        public FiliaisController(
            IFilialService filialService,
            INotificadorService notificador)
            : base(notificador)
        {
            _filialService = filialService;
        }

        // ========================= CONSULTAS =========================

        [HttpGet]
        public async Task<IActionResult> ObterTodas(CancellationToken ct)
        {
            var filiais = await _filialService.ObterTodasAsync(ct);
            return CustomResponse(filiais);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> ObterPorId(int id, CancellationToken ct)
        {
            var filial = await _filialService.ObterPorIdAsync(id, ct);
            return CustomResponse(filial);
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

        // ========================= EXCLUSÃO =========================

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Excluir(int id, CancellationToken ct)
        {
            var sucesso = await _filialService.ExcluirFilialAsync(id, ct);
            return CustomResponse(sucesso, HttpStatusCode.NoContent);
        }

        // ========================= STATUS =========================

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
    }


    public class TransferirVeiculoDto
    {
        public int VeiculoId { get; set; }
        public int FilialDestinoId { get; set; }
    }
}
