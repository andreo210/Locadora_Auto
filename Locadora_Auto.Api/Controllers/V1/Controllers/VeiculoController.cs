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
        /// RN-52: tira o veículo da oferta com motivo, prazo e responsável. Bloqueio sem prazo é
        /// carro que some da frota e ninguém percebe — por isso os três campos são obrigatórios.
        /// </summary>
        [HttpPost("{id:int}/bloquear")]
        public async Task<ActionResult> Bloquear(int id, [FromBody] BloquearVeiculoDto dto, CancellationToken ct)
        {
            var result = await _veiculoService.BloquearAsync(id, dto, ct);
            if (result == null)
                return CustomResponse();

            return CustomResponse(result, HttpStatusCode.Created);
        }

        /// <summary>
        /// Encerra o bloqueio. O veículo volta para a situação em que estava antes dele, e não
        /// direto para a oferta: carro bloqueado no pátio volta para o pátio, carro bloqueado por
        /// não devolução volta para locado.
        /// </summary>
        [HttpPatch("{id:int}/bloqueios/{idBloqueio:int}/liberar")]
        public async Task<ActionResult> LiberarBloqueio(int id, int idBloqueio, CancellationToken ct)
        {
            var sucesso = await _veiculoService.LiberarBloqueioAsync(id, idBloqueio, ct);
            if (!sucesso)
                return CustomResponse();

            return CustomResponse(null, HttpStatusCode.NoContent);
        }

        /// <summary>
        /// Bloqueios do veículo, abertos e encerrados, do mais recente para o mais antigo.
        /// </summary>
        [HttpGet("{id:int}/bloqueios")]
        public async Task<ActionResult> ObterBloqueios(int id, CancellationToken ct)
        {
            var result = await _veiculoService.ObterBloqueiosAsync(id, ct);
            return CustomResponse(result);
        }

        /// <summary>
        /// RN-56: o ativo deixa a frota, em definitivo. Recusado com contrato aberto — ou já
        /// vendido para o futuro, que o status do veículo não revela.
        /// </summary>
        [HttpPatch("{id:int}/desmobilizar")]
        public async Task<ActionResult> Desmobilizar(
            int id, [FromBody] DesmobilizarVeiculoDto dto, CancellationToken ct)
        {
            var sucesso = await _veiculoService.DesmobilizarAsync(id, dto, ct);
            if (!sucesso)
                return CustomResponse();

            return CustomResponse(null, HttpStatusCode.NoContent);
        }

        /// <summary>
        /// RN-49: manda o veículo para outra filial. Ele sai da oferta da origem agora e só entra
        /// na do destino quando a chegada for confirmada — contá-lo nas duas durante o trecho é
        /// overbooking involuntário.
        /// </summary>
        [HttpPost("{id:int}/transferencias")]
        public async Task<ActionResult> EnviarParaTransferencia(
            int id, [FromBody] EnviarTransferenciaDto dto, CancellationToken ct)
        {
            var result = await _veiculoService.EnviarParaTransferenciaAsync(id, dto, ct);
            if (result == null)
                return CustomResponse();

            return CustomResponse(result, HttpStatusCode.Created);
        }

        /// <summary>
        /// Chegada ao destino: a filial de destino vira a atual e o veículo volta à oferta de lá.
        /// </summary>
        [HttpPatch("{id:int}/transferencias/{idTransferencia:int}/chegada")]
        public async Task<ActionResult> ConfirmarChegadaTransferencia(
            int id, int idTransferencia, [FromBody] ChegadaTransferenciaDto dto, CancellationToken ct)
        {
            var sucesso = await _veiculoService.ConfirmarChegadaTransferenciaAsync(id, idTransferencia, dto, ct);
            if (!sucesso)
                return CustomResponse();

            return CustomResponse(null, HttpStatusCode.NoContent);
        }

        /// <summary>
        /// Aborta a viagem: o veículo volta à oferta da filial de origem.
        /// </summary>
        [HttpPatch("{id:int}/transferencias/{idTransferencia:int}/cancelar")]
        public async Task<ActionResult> CancelarTransferencia(int id, int idTransferencia, CancellationToken ct)
        {
            var sucesso = await _veiculoService.CancelarTransferenciaAsync(id, idTransferencia, ct);
            if (!sucesso)
                return CustomResponse();

            return CustomResponse(null, HttpStatusCode.NoContent);
        }

        /// <summary>Transferências do veículo, da mais recente para a mais antiga.</summary>
        [HttpGet("{id:int}/transferencias")]
        public async Task<ActionResult> ObterTransferencias(int id, CancellationToken ct)
        {
            var result = await _veiculoService.ObterTransferenciasAsync(id, ct);
            return CustomResponse(result);
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
