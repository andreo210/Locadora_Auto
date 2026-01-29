using Locadora_Auto.Api.V1.Controllers;
using Locadora_Auto.Application.Configuration.Ultils.NotificadorServices;
using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Application.Services.LocacaoServices;
using Microsoft.AspNetCore.Mvc;

namespace Locadora_Auto.Api.Controllers
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

        // ====================== FINALIZAR LOCAÇÃO ======================
        [HttpPost("{id:int}/finalizar")]
        public async Task<IActionResult> Finalizar(int id, [FromBody] FinalizarLocacaoDto dto, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return CustomResponse(ModelState);

            var sucesso = await _locacaoService.FinalizarAsync(
                id,
                dto.DataFimReal,
                dto.KmFinal,
                dto.ValorFinal,
                dto.IdFilialDevolucao,
                ct
            );

            return CustomResponse(sucesso);
        }

        // ====================== CANCELAR LOCAÇÃO ======================
        [HttpPost("{id:int}/cancelar")]
        public async Task<IActionResult> Cancelar(int id, CancellationToken ct)
        {
            var sucesso = await _locacaoService.CancelarAsync(id, ct);
            return CustomResponse(sucesso);
        }

        // ====================== ADICIONAR PAGAMENTO ======================
        [HttpPost("{id:int}/pagamentos")]
        public async Task<IActionResult> AdicionarPagamento(int id,[FromBody] AdicionarPagamentoDto dto, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return CustomResponse(ModelState);
            var sucesso = await _locacaoService.AdicionarPagamentoAsync(id, dto, ct);
            return CustomResponse(sucesso);
        }

        // ====================== ADICIONAR MULTA ======================
        [HttpPost("{id:int}/multas")]
        public async Task<IActionResult> AdicionarMulta(
            int id,
            [FromBody] MultaDto dto,
            CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return CustomResponse(ModelState);

            var sucesso = await _locacaoService.AdicionarMultaAsync(id, dto, ct);
            return CustomResponse(sucesso);
        }

        // ====================== ADICIONAR SEGURO ======================
        [HttpPost("{id:int}/seguros")]
        public async Task<IActionResult> AdicionarSeguro(
            int id,
            [FromBody] LocacaoSeguroDto dto,
            CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return CustomResponse(ModelState);

            var sucesso = await _locacaoService.AdicionarSeguroAsync(id, dto, ct);
            return CustomResponse(sucesso);
        }

        // ====================== OBTER POR ID ======================
        [HttpGet("{id:int}")]
        public async Task<IActionResult> ObterPorId(int id, CancellationToken ct)
        {
            var locacao = await _locacaoService.ObterPorIdAsync(id, ct);
            return CustomResponse(locacao);
        }

        // ====================== LISTAR TODAS ======================
        [HttpGet]
        public async Task<IActionResult> ObterTodas(CancellationToken ct)
        {
            var locacoes = await _locacaoService.ObterTodasAsync(ct);
            return CustomResponse(locacoes);
        }
    }
}
