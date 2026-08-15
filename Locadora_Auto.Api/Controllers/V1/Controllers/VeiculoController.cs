using Locadora_Auto.Api.Controllers;
using Locadora_Auto.Application.Configuration.Ultils.NotificadorServices;
using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Application.Services.VeiculoServices;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Locadora_Auto.Api.Controllers.V1.Controllers
{
    [ApiController]
    [Route("api/v1/veiculos")]
    public class VeiculoController : MainController
    {
        private readonly IVeiculoService _veiculoService;
        private readonly IIndicadoresFrotaService _indicadoresService;

        public VeiculoController(
            IVeiculoService veiculoService,
            IIndicadoresFrotaService indicadoresService,
            INotificadorService notificador): base(notificador)
        {
            _veiculoService = veiculoService;
            _indicadoresService = indicadoresService;
        }


        [HttpGet]
        public async Task<ActionResult> ObterTodos(
            CancellationToken ct,
            [FromQuery] ConsultaPaginadaRequest consulta,
            [FromQuery] int? idCategoria = null,
            [FromQuery] int? idFilial = null,
            [FromQuery] int? idStatus = null,
            [FromQuery] bool? ativo = null)
        {
            var result = await _veiculoService.ObterTodosPaginadoAsync(
                consulta, idCategoria, idFilial, idStatus, ativo, ct);

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

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Excluir(int id, CancellationToken ct)
        {
            var sucesso = await _veiculoService.ExcluirAsync(id, ct);
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

        /// <summary>
        /// O pátio declara o carro pronto depois da devolução e ele volta à oferta (RN-45).
        /// </summary>
        [HttpPatch("{id:int}/liberar-preparacao")]
        public async Task<ActionResult> LiberarDaPreparacao(int id, CancellationToken ct)
        {
            var sucesso = await _veiculoService.LiberarDaPreparacaoAsync(id, ct);
            if (!sucesso)
                return CustomResponse();

            return CustomResponse(null, HttpStatusCode.NoContent);
        }

        /// <summary>
        /// Indicadores de frota da seção 12, apurados sobre a trilha (RN-37): utilização real e
        /// tempo médio de preparação. Sem período informado, vale a janela dos últimos 30 dias.
        /// </summary>
        [HttpGet("indicadores")]
        public async Task<ActionResult> ObterIndicadores(
            CancellationToken ct,
            [FromQuery] DateTime? de = null,
            [FromQuery] DateTime? ate = null,
            [FromQuery] int? idFilial = null,
            [FromQuery] int? idCategoria = null)
        {
            var result = await _indicadoresService.ObterAsync(de, ate, idFilial, idCategoria, ct);
            return CustomResponse(result);
        }

        /// <summary>
        /// Trilha de movimentação do ativo (RN-37): por onde o carro passou, com que documento,
        /// quando e por quem. Da transição mais recente para a mais antiga.
        /// </summary>
        [HttpGet("{id:int}/movimentos")]
        public async Task<ActionResult> ObterMovimentos(
            int id,
            CancellationToken ct,
            [FromQuery] ConsultaPaginadaRequest consulta,
            [FromQuery] DateTime? de = null,
            [FromQuery] DateTime? ate = null,
            [FromQuery] int? idTipoOrigem = null)
        {
            var result = await _veiculoService.ObterMovimentosAsync(id, consulta, de, ate, idTipoOrigem, ct);
            return CustomResponse(result);
        }

        [HttpGet("{id:int}/manutencao")]
        public async Task<ActionResult> ObterManutencoes(int id, CancellationToken ct)
        {
            var result = await _veiculoService.ObterManutencoesAsync(id, ct);
            return CustomResponse(result);
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
