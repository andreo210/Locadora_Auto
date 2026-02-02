using Locadora_Auto.Application.Configuration.Ultils.NotificadorServices;
using Locadora_Auto.Application.Services.MultaServices;
using Microsoft.AspNetCore.Mvc;

namespace Locadora_Auto.Api.V1.Controllers
{
    [ApiController]
    [Route("api/v1/multas")]
    public class MultaController : MainController
    {
        private readonly IMultaService _multaService;

        public MultaController(
            IMultaService multaServicee,
            INotificadorService notificador)
            : base(notificador)
        {
            _multaService = multaServicee;
        }
        
        [HttpGet("{idLocacao:int}")]
        public async Task<ActionResult> ObterPorLocacao(int idLocacao, CancellationToken ct)
        {
            var result = await _multaService.ObterMultasPorLocacaoAsync(idLocacao,ct);
            return CustomResponse(result);
        }

        [HttpGet("tipo-multa/{idTipo:int}")]
        public async Task<ActionResult> ObterPorTipo(int idTipo, CancellationToken ct)
        {
            var result = await _multaService.ObterMultasPorTipoAsync(idTipo, ct);
            return CustomResponse(result);
        }

        [HttpGet("status-multa/{idTipo:int}")]
        public async Task<ActionResult> ObterPorAtatus(int idTipo, CancellationToken ct)
        {
            var result = await _multaService.ObterMultasStatusAsync(idTipo, ct);
            return CustomResponse(result);
        }
    }
}
