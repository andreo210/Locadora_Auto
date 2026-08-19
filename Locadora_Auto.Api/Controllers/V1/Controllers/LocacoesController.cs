using Locadora_Auto.Application.Configuration.Ultils.NotificadorServices;
using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Application.Services.LocacaoServices;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.Net;

namespace Locadora_Auto.Api.Controllers.V1.Controllers
{
    [ApiController]
    [Route("api/locacoes")]
    public class LocacoesController : MainController
    {
        private readonly ILocacaoService _locacaoService;

        public LocacoesController(ILocacaoService locacaoService, INotificadorService notificador) : base(notificador)
        {
            _locacaoService= locacaoService;
        }

        #region Leitura
        [HttpGet("{id:int}")]
        public async Task<IActionResult> ObterPorId(int id, CancellationToken ct)
        {
            var locacao = await _locacaoService.ObterPorIdAsync(id, ct);
            return CustomResponse(locacao);
        }


        [HttpGet]
        public async Task<IActionResult> ObterTodas(CancellationToken ct)
        {
            var locacoes = await _locacaoService.ObterTodasAsync(ct);
            return CustomResponse(locacoes);
        }

        #endregion Leitura

        // ====================== CRIAR LOCAÇÃO ======================
        [HttpPost]
        public async Task<IActionResult> Criar([FromBody] CriarLocacaoDto dto, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return CustomResponse(ModelState);

            var resultado = await _locacaoService.CriarAsync(dto, ct);
            return CustomResponse(resultado);
        }

        // ====================== ATUALIZAR LOCAÇÃO ======================
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Atualizar(
            int id,
            [FromBody] AtualizarLocacaoDto dto,
            CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return CustomResponse(ModelState);

            var resultado = await _locacaoService.AtualizarAsync(id, dto, ct);
            return CustomResponse(resultado);
        }

        // ====================== DEVOLUÇÃO E FECHAMENTO ======================
        //
        // Doc 07 §1: DEVOLUÇÃO → FECHAMENTO → QUITAÇÃO são atos distintos, e agora são portas
        // distintas. Antes havia uma só, `POST {id}/finalizar`, que recebia o `valorFinal` digitado
        // por quem chamava — nenhum cálculo, nenhum extrato.

        /// <summary>Encerra a posse: o carro volta ao pátio e o contrato vai a `Devolvida`.</summary>
        [HttpPost("{id:int}/devolucao")]
        public async Task<IActionResult> RegistrarDevolucao(int id, [FromBody] RegistrarDevolucaoDto dto, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return CustomResponse(ModelState);

            var sucesso = await _locacaoService.RegistrarDevolucaoAsync(id, dto, ct);
            return CustomResponse(sucesso);
        }

        /// <summary>
        /// Apura a conta, fecha o contrato e resolve a caução. Idempotente (RN-32): repetir a
        /// chamada devolve a mesma apuração, sem cobrar nada de novo.
        /// </summary>
        [HttpPost("{id:int}/fechamento")]
        public async Task<IActionResult> ApurarFechamento(int id, [FromBody] ApurarFechamentoDto dto, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return CustomResponse(ModelState);

            var resultado = await _locacaoService.ApurarFechamentoAsync(id, dto, ct);
            return CustomResponse(resultado);
        }

        /// <summary>O extrato discriminado (RN-31) — a conta que o cliente recebe.</summary>
        [HttpGet("{id:int}/fechamento")]
        public async Task<IActionResult> ObterFechamento(int id, CancellationToken ct)
        {
            var fechamento = await _locacaoService.ObterFechamentoAsync(id, ct);
            return CustomResponse(fechamento);
        }

        // ====================== CANCELAR LOCAÇÃO ======================
        [HttpPost("{id:int}/cancelar")]
        public async Task<IActionResult> Cancelar(int id, CancellationToken ct)
        {
            var sucesso = await _locacaoService.CancelarAsync(id, ct);
            return CustomResponse(sucesso);
        }


        #region Multa
        [HttpPost("{id:int}/multas")]
        public async Task<IActionResult> AdicionarMulta(
            int id,
            [FromBody] CriarMultaDto dto,
            CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return CustomResponse(ModelState);

            var sucesso = await _locacaoService.AdicionarMultaAsync(id, dto, ct);
            return CustomResponse(sucesso);
        }

        [HttpPost("{id:int}/multas/{idMulta:int}/compensar")]
        public async Task<IActionResult> CompensarMulta(int id,int idMulta,CancellationToken ct)  
        {
            if (!ModelState.IsValid)
                return CustomResponse(ModelState);

            var sucesso = await _locacaoService.CompensarMultaAsync(id,idMulta, ct);
            return CustomResponse(sucesso);
        }

        [HttpPost("{id:int}/multas/{idMulta:int}/cancelar")]
        public async Task<IActionResult> CancelarMulta(int id, int idMulta, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return CustomResponse(ModelState);

            var sucesso = await _locacaoService.CancelarMultaAsync(id, idMulta, ct);
            return CustomResponse(sucesso);
        }

        [HttpPost("{id:int}/multas/{idMulta:int}/pagar")]
        public async Task<IActionResult> PagarMulta(int id, int idMulta, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return CustomResponse(ModelState);

            var sucesso = await _locacaoService.PagarMultaAsync(id, idMulta, ct);
            return CustomResponse(sucesso);
        }
        #endregion Multa

        #region Pagamento
        [HttpPost("{id:int}/pagamento")]
        public async Task<IActionResult> AdicionarPagamento(int id,[FromBody] AdicionarPagamentoDto dto, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return CustomResponse(ModelState);

            var sucesso = await _locacaoService.AdicionarPagamentoAsync(id, dto, ct);
            return CustomResponse(sucesso);
        }

        [HttpPost("{id:int}/pagamento/{idPagamento:int}/comfirmar")]
        public async Task<IActionResult> ComfirmarPagamento(int id, int idPagamento, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return CustomResponse(ModelState);

            var sucesso = await _locacaoService.ConfirmarPagamentoAsync(id, idPagamento, ct);
            return CustomResponse(sucesso);
        }

        [HttpPost("{id:int}/pagamento/{idPagamento:int}/marcar-falha")]
        public async Task<IActionResult> MarcarComoFalha(int id, int idPagamento, string motivo, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return CustomResponse(ModelState);

            var sucesso = await _locacaoService.MarcarComoFalhaAsync(id, idPagamento, ct);
            return CustomResponse(sucesso);
        }

        [HttpPost("{id:int}/pagamento/{idPagamento:int}/cancelar")]
        public async Task<IActionResult> CancelarPagamento(int id, int idPagamento, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return CustomResponse(ModelState);

            var sucesso = await _locacaoService.PagarMultaAsync(id, idPagamento, ct);
            return CustomResponse(sucesso);
        }
        #endregion Multa

        #region Caucao
        [HttpPost("{id:int}/caucao/{valor:decimal}")]
        public async Task<IActionResult> AdicionarCaucao(int id, decimal valor, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return CustomResponse(ModelState);

            var sucesso = await _locacaoService.AdicionarCalcaoAsync(id, valor, ct);
            return CustomResponse(sucesso);
        }

        

        [HttpPost("{id:int}/caucao/{idCaucao:int}/devolver")]
        public async Task<IActionResult> DevolverCaucao(int id, int idCaucao, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return CustomResponse(ModelState);

            var sucesso = await _locacaoService.DevolverCalcaoAsync(id, idCaucao, ct);
            return CustomResponse(sucesso);
        }

        [HttpPost("{id:int}/caucao/{idCaucao:int}/bloquear")]
        public async Task<IActionResult> BloquarCaucao(int id, int idCaucao, string motivo, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return CustomResponse(ModelState);

            var sucesso = await _locacaoService.BloquearCalcaoAsync(id, idCaucao, ct);
            return CustomResponse(sucesso);
        }

        [HttpPost("{id:int}/caucao/{idCaucao:int}/deduzir")]
        public async Task<IActionResult> DeduzirCaucao(int id, int idCaucao,decimal valor, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return CustomResponse(ModelState);

            var sucesso = await _locacaoService.DeduzirCalcaoAsync(id, idCaucao,valor,ct);
            return CustomResponse(sucesso);
        }

        #endregion Caucao

        #region Seguro
        // ====================== ADICIONAR SEGURO ======================
        [HttpPost("{id:int}/seguros/{idSeguro:int}/adicionar")]
        public async Task<IActionResult> AdicionarSeguro(int id, int idSeguro, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return CustomResponse(ModelState);

            var sucesso = await _locacaoService.AdicionarSeguroAsync(id, idSeguro, ct);
            return CustomResponse(sucesso);
        }

        [HttpPost("{id:int}/seguros/{idLocacaoSeguro:int}/cancelar")]
        public async Task<IActionResult> CancelarSeguro(int id, int idLocacaoSeguro, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return CustomResponse(ModelState);

            var sucesso = await _locacaoService.CancelarSeguroAsync(id, idLocacaoSeguro, ct);
            return CustomResponse(sucesso);
        }
        #endregion Seguro

        #region Vistoria
        [HttpPost("{id:int}/vistoria")]
        public async Task<IActionResult> RegistrarVistoria(int id, [FromBody] CriarVistoriaDto dto, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return CustomResponse(ModelState);
            var sucesso = await _locacaoService.RegistrarVistoriaAsync(id, dto, ct);
            return CustomResponse(sucesso);
        }

        [HttpPost("{id:int}/vistoria/enviar-fotos")]
        public async Task<IActionResult> RegistrarFotoVistoria(int id, [FromForm] EnviarFotoVistoriaDto dto, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return ValidationResponse(ModelState);

            var fotos = await _locacaoService.RegistrarFotoVistoriaAsync(id, dto, ct);
            return CustomResponse(fotos, HttpStatusCode.Created);
        }

        [HttpPost("{id:int}/vistoria/registrar-dano")]
        public async Task<IActionResult> RegistrarDanoVistoria(int id, [FromBody] CriarDanoDto dto, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return ValidationResponse(ModelState);

            var fotos = await _locacaoService.RegistrarDanoVistoriaAsync(id, dto, ct);
            return CustomResponse(fotos, HttpStatusCode.Created);
        }

        [HttpPost("{id:int}/vistoria/remover-dano")]
        public async Task<IActionResult> RemoverDanoVistoria(int id, [FromBody] RemoverDanoDto dto, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return ValidationResponse(ModelState);

            var fotos = await _locacaoService.RemoverDanoVistoriaAsync(id, dto, ct);
            return CustomResponse(fotos, HttpStatusCode.Created);
        }

        #endregion Vistoria

        #region Adicional
        [HttpPost("{id:int}/adicional")]
        public async Task<IActionResult> InserirAdicional(int id, [FromBody] LocacaoAdicionalDto dto, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return CustomResponse(ModelState);
            var sucesso = await _locacaoService.InserirAdicionalAsync(id, dto, ct);
            return CustomResponse(sucesso);
        }

        [HttpPost("{id:int}/adicional/{idAdicional}/remover")]
        public async Task<IActionResult> RemoverAdicional(int id,  int idAdicional, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return CustomResponse(ModelState);
            var sucesso = await _locacaoService.RemoverAdicionalAsync(id, idAdicional, ct);
            return CustomResponse(sucesso);
        }
        #endregion Adicional

    }
}
