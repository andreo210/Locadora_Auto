using Locadora_Auto.Api.Controllers;
using Locadora_Auto.Application.Configuration.Ultils.NotificadorServices;
using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Application.Services.VeiculoServices;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Locadora_Auto.Api.V1.Controllers
{
    [ApiController]
    [Route("api/v1/veiculos")]
    public class VeiculoController : MainController
    {
        private readonly IVeiculoService _veiculoService;

        public VeiculoController(IVeiculoService veiculoService,  INotificadorService notificador): base(notificador)
        {
            _veiculoService = veiculoService;
        }


        [HttpGet]
        public async Task<ActionResult> ObterTodos(CancellationToken ct)
        {
            var result = await _veiculoService.ObterTodosAsync(ct);
            return CustomResponse(result);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult> ObterPorId(int id, CancellationToken ct)
        {
            var result = await _veiculoService.ObterPorIdAsync(id, ct);
            if (result == null)
                return NotFound($"Veículo com ID {id} não encontrado.");

            return CustomResponse(result);
        }

        [HttpGet("disponiveis")]
        public async Task<ActionResult> ObterDisponiveis(
            [FromQuery] int? idFilial,
            CancellationToken ct)
        {
            var result = await _veiculoService.ObterDisponiveisAsync(idFilial, ct);
            return CustomResponse(result);
        }

        [HttpPost]
        public async Task<ActionResult> Criar(
            [FromBody] CriarVeiculoDto dto,
            CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return ValidationResponse(ModelState);

            var result = await _veiculoService.CriarAsync(dto, ct);
            return CustomResponse(result, HttpStatusCode.Created);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult> Atualizar(
            int id,
            [FromBody] AtualizarVeiculoDto dto,
            CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return ValidationResponse(ModelState);

            var sucesso = await _veiculoService.AtualizarAsync(id, dto, ct);
            if (!sucesso)
                return CustomResponse();

            return CustomResponse(null, HttpStatusCode.NoContent);
        }

        [HttpPatch("{id:int}/ativar")]
        public async Task<ActionResult> Ativar(int id, CancellationToken ct)
        {
            var sucesso = await _veiculoService.AtivarAsync(id, ct);
            if (!sucesso)
                return CustomResponse();

            return CustomResponse(null, HttpStatusCode.NoContent);
        }

        [HttpPatch("{id:int}/desativar")]
        public async Task<ActionResult> Desativar(int id, CancellationToken ct)
        {
            var sucesso = await _veiculoService.DesativarAsync(id, ct);
            if (!sucesso)
                return CustomResponse();

            return CustomResponse(null, HttpStatusCode.NoContent);
        }

        [HttpPost("{id:int}/manutencao/iniciar-manutencao")]
        public async Task<ActionResult> IniciarManutencao(int id, [FromBody] CriarManutencaoDto dto, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return ValidationResponse(ModelState);

            var result = await _veiculoService.IniciarManutencao(id,dto, ct);
            return CustomResponse(result, HttpStatusCode.Created);
        }

        [HttpPost("{id:int}/manutencao/cancelar-manutencao/{idManutencao:int}")]
        public async Task<ActionResult> CancelarManutencao(int id, int idManutencao, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return ValidationResponse(ModelState);

            var result = await _veiculoService.CancelarManutencao(id, idManutencao, ct);
            return CustomResponse(result, HttpStatusCode.Created);
        }

        [HttpPost("{id:int}/manutencao/terminar-manutencao")]
        public async Task<ActionResult> TerminarManutencao(int id, [FromBody] TerminarManutencaoDto dto, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return ValidationResponse(ModelState);

            var result = await _veiculoService.TerminaManutencao(id, dto, ct);
            return CustomResponse(result, HttpStatusCode.Created);
        }

        [HttpPost("{id:int}/manutencao/atualizar-manutencao")]
        public async Task<ActionResult> AtualizarManutencao(int id, [FromBody] AtualizarManutencaoDto dto, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return ValidationResponse(ModelState);

            var result = await _veiculoService.AtualizarDescricaoManutencao(id, dto, ct);
            return CustomResponse(result, HttpStatusCode.Created);
        }
    }
}
