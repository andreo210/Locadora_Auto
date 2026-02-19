using Locadora_Auto.Api.Controllers;
using Locadora_Auto.Application.Configuration.Ultils.NotificadorServices;
using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Application.Services.AdicionaisServices;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Locadora_Auto.Api.V1.Controllers
{
    [ApiController]
    [Route("api/v1/adicionais")]
    public class AdicionalController : MainController
    {
        private readonly IAdicionalService _seguroService;

        public AdicionalController(IAdicionalService seguroService, INotificadorService notificador): base(notificador)
        {
            _seguroService = seguroService;
        }

        [HttpGet]
        public async Task<ActionResult> ObterTodos(CancellationToken ct)
        {
            var result = await _seguroService.ObterTodosAsync(ct);
            return CustomResponse(result);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult> ObterPorId(int id, CancellationToken ct)
        {
            var result = await _seguroService.ObterPorIdAsync(id, ct);
            if (result == null)
                return NotFound($"Seguro com ID {id} não encontrado.");

            return CustomResponse(result);
        }

        [HttpGet("obter-ativos")]
        public async Task<ActionResult> ObterDisponiveis(CancellationToken ct)
        {
            var result = await _seguroService.ObterSeguroAtivoAsync(ct);
            return CustomResponse(result);
        }


        [HttpPost]
        public async Task<ActionResult> Criar([FromBody] CriarAtualizarAdicionalDto dto, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return ValidationResponse(ModelState);

            var result = await _seguroService.CriarAsync(dto, ct);
            return CustomResponse(result, HttpStatusCode.Created);
        }


        [HttpPut("{id:int}")]
        public async Task<ActionResult> Atualizar(int id,[FromBody] CriarAtualizarAdicionalDto dto, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return ValidationResponse(ModelState);

            var sucesso = await _seguroService.AtualizarAsync(id, dto, ct);
            if (!sucesso)
                return CustomResponse();

            return CustomResponse(null, HttpStatusCode.NoContent);
        }

        [HttpPatch("{id:int}/ativar")]
        public async Task<ActionResult> Ativar(int id, CancellationToken ct)
        {
            var sucesso = await _seguroService.AtivarAsync(id, ct);
            if (!sucesso)
                return CustomResponse();

            return CustomResponse("adicional ativado", HttpStatusCode.OK);
        }

        [HttpPatch("{id:int}/desativar")]
        public async Task<ActionResult> Desativar(int id, CancellationToken ct)
        {
            var sucesso = await _seguroService.DesativarAsync(id, ct);
            if (!sucesso)
                return CustomResponse();

            return CustomResponse("adicional desativado", HttpStatusCode.OK);
        }
    }
}
