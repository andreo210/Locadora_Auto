using Locadora_Auto.Application.Configuration.Ultils.NotificadorServices;
using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Application.Services.MultaServices;
using Locadora_Auto.Application.Services.VeiculoServices;
using Microsoft.AspNetCore.Mvc;
using System.Net;

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

        // =========================
        // CONSULTAS
        // =========================

        //[HttpGet]
        //public async Task<ActionResult> ObterTodos(CancellationToken ct)
        //{
        //    var result = await _veiculoService.ObterTodosAsync(ct);
        //    return CustomResponse(result);
        //}

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


        //[HttpGet("disponiveis")]
        //public async Task<ActionResult> ObterDisponiveis(
        //    [FromQuery] int? idFilial,
        //    CancellationToken ct)
        //{
        //    var result = await _veiculoService.ObterDisponiveisAsync(idFilial, ct);
        //    return CustomResponse(result);
        //}

        // =========================
        // CRIAÇÃO
        // =========================

        //[HttpPost]
        //public async Task<ActionResult> Criar(
        //    [FromBody] CriarVeiculoDto dto,
        //    CancellationToken ct)
        //{
        //    if (!ModelState.IsValid)
        //        return ValidationResponse(ModelState);

        //    var result = await _veiculoService.CriarAsync(dto, ct);
        //    return CustomResponse(result, HttpStatusCode.Created);
        //}

        // =========================
        // ATUALIZAÇÃO
        // =========================

        //[HttpPut("{id:int}")]
        //public async Task<ActionResult> Atualizar(
        //    int id,
        //    [FromBody] AtualizarVeiculoDto dto,
        //    CancellationToken ct)
        //{
        //    if (!ModelState.IsValid)
        //        return ValidationResponse(ModelState);

        //    var sucesso = await _veiculoService.AtualizarAsync(id, dto, ct);
        //    if (!sucesso)
        //        return CustomResponse();

        //    return CustomResponse(null, HttpStatusCode.NoContent);
        //}

        // =========================
        // ATIVAÇÃO / DESATIVAÇÃO
        // =========================

        //[HttpPatch("{id:int}/ativar")]
        //public async Task<ActionResult> Ativar(int id, CancellationToken ct)
        //{
        //    var sucesso = await _veiculoService.AtivarAsync(id, ct);
        //    if (!sucesso)
        //        return CustomResponse();

        //    return CustomResponse(null, HttpStatusCode.NoContent);
        //}

        //[HttpPatch("{id:int}/desativar")]
        //public async Task<ActionResult> Desativar(int id, CancellationToken ct)
        //{
        //    var sucesso = await _veiculoService.DesativarAsync(id, ct);
        //    if (!sucesso)
        //        return CustomResponse();

        //    return CustomResponse(null, HttpStatusCode.NoContent);
        //}
    }
}
