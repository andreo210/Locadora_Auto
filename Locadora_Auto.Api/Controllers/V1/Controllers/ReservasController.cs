using Locadora_Auto.Api.Controllers;
using Locadora_Auto.Application.Configuration.Ultils.NotificadorServices;
using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Application.Services.ReservaServices;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Locadora_Auto.Api.Controllers.V1.Controllers
{
    [ApiController]
    [Route("api/v1/reservas")]
    public class ReservaController : MainController
    {
        private readonly IReservaService _reservaService;

        public ReservaController(
            IReservaService reservaService,
            INotificadorService notificador)
            : base(notificador)
        {
            _reservaService = reservaService;
        }

        [HttpGet]
        public async Task<ActionResult> ObterTodos(
            CancellationToken ct,
            [FromQuery] ConsultaPaginadaRequest consulta,
            [FromQuery] int? status = null,
            [FromQuery] int? idFilial = null,
            [FromQuery] int? idCliente = null)
        {
            var result = await _reservaService.ObterTodosPaginadoAsync(consulta, status, idFilial, idCliente, ct);

            return CustomResponse(result);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult> ObterPorId(int id, CancellationToken ct)
        {
            var result = await _reservaService.ObterPorIdAsync(id, ct);
            if (result == null)
                return NotFound($"Reserva com ID {id} não encontrada.");

            return CustomResponse(result);
        }

        [HttpGet("cliente/{idCliente:int}")]
        public async Task<ActionResult> ObterPorCliente(int idCliente, CancellationToken ct)
        {
            var result = await _reservaService.ObterPorClienteAsync(idCliente, ct);
            return CustomResponse(result);
        }

        [HttpPost]
        public async Task<ActionResult> Criar([FromBody] CriarReservaDto dto, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return ValidationResponse(ModelState);

            var result = await _reservaService.CriarAsync(dto, ct);
            return CustomResponse(result, HttpStatusCode.Created);
        }

        [HttpPatch("{id:int}/cancelar")]
        public async Task<ActionResult> Cancelar(int id, CancellationToken ct)
        {
            var sucesso = await _reservaService.CancelarAsync(id, ct);
            if (!sucesso)
                return CustomResponse();

            return CustomResponse("reserva cancelada", HttpStatusCode.OK);
        }

        [HttpPatch("{id:int}/finalizar")]
        public async Task<ActionResult> Finalizar(int id, CancellationToken ct)
        {
            var sucesso = await _reservaService.FinalizarAsync(id, ct);
            if (!sucesso)
                return CustomResponse();

            return CustomResponse("reserva finalizada", HttpStatusCode.OK);
        }

        /// <summary>
        /// Expira em lote as reservas cuja data de início já passou. Enquanto o Hangfire está
        /// desligado esta varredura é disparada manualmente.
        /// </summary>
        [HttpPatch("expirar-vencidas")]
        public async Task<ActionResult> ExpirarVencidas(CancellationToken ct)
        {
            var total = await _reservaService.ExpirarVencidasAsync(ct);
            return CustomResponse(new { Expiradas = total });
        }
    }
}
