using Locadora_Auto.Application.Models.Dto.Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Application.Services.FilialServices;
using Microsoft.AspNetCore.Mvc;

namespace Locadora_Auto.Api.V1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FiliaisController : ControllerBase
    {
        private readonly IFilialService _filialService;
        private readonly ILogger<FiliaisController> _logger;

        public FiliaisController(
            IFilialService filialService,
            ILogger<FiliaisController> logger)
        {
            _filialService = filialService ?? throw new ArgumentNullException(nameof(filialService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        ////[HttpGet]
        ////public async Task<ActionResult<IEnumerable<FilialDto>>> Get(
        ////    [FromQuery] bool? apenasAtivas = true,
        ////    [FromQuery] string? cidade = null,
        ////    CancellationToken ct = default)
        ////{
        ////    try
        ////    {
        ////        IEnumerable<FilialDto> filiais;

        ////        if (!string.IsNullOrEmpty(cidade))
        ////        {
        ////            filiais = await _filialService.ObterPorCidadeAsync(cidade, ct);
        ////        }
        ////        else if (apenasAtivas == true)
        ////        {
        ////            filiais = await _filialService.ObterAtivasAsync(ct);
        ////        }
        ////        else
        ////        {
        ////            filiais = await _filialService.ObterTodasAsync(ct);
        ////        }

        ////        return Ok(filiais);
        ////    }
        ////    catch (Exception ex)
        ////    {
        ////        _logger.LogError(ex, "Erro ao buscar filiais");
        ////        return StatusCode(500, new { Message = "Erro interno ao processar a solicitação" });
        ////    }
        ////}

        //[HttpGet("resumo")]
        //public async Task<ActionResult<IEnumerable<FilialResumoDto>>> GetResumo(CancellationToken ct = default)
        //{
        //    try
        //    {
        //        var filiais = await _filialService.ObterResumoAsync(ct);
        //        return Ok(filiais);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Erro ao buscar resumo das filiais");
        //        return StatusCode(500, new { Message = "Erro interno ao processar a solicitação" });
        //    }
        //}

        [HttpGet("{id}")]
        public async Task<ActionResult<FilialDto>> GetById(int id, CancellationToken ct = default)
        {
            
            var filial = await _filialService.ObterPorIdAsync(id, ct);
            if (filial == null)
                return NotFound(new { Message = $"Filial com ID {id} não encontrada" });

            return Ok(filial);           
        }

        [HttpGet]
        public async Task<ActionResult<FilialDto>> Get(CancellationToken ct = default)
        {           
            var filial = await _filialService.ObterTodasAsync(ct);
            return Ok(filial);            
        }

        //[HttpGet("{id}/veiculos")]
        //public async Task<ActionResult<IEnumerable<VeiculoDto>>> GetVeiculos(int id, CancellationToken ct = default)
        //{
        //    try
        //    {
        //        var veiculos = await _filialService.ObterVeiculosDaFilialAsync(id, ct);
        //        return Ok(veiculos);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Erro ao buscar veículos da filial: {Id}", id);
        //        return StatusCode(500, new { Message = "Erro interno ao processar a solicitação" });
        //    }
        //}

        //[HttpGet("{id}/estatisticas")]
        //public async Task<ActionResult<EstatisticasFilialDto>> GetEstatisticas(int id, CancellationToken ct = default)
        //{
        //    try
        //    {
        //        var estatisticas = await _filialService.ObterEstatisticasFilialAsync(id, ct);
        //        return Ok(estatisticas);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Erro ao buscar estatísticas da filial: {Id}", id);
        //        return StatusCode(500, new { Message = "Erro interno ao processar a solicitação" });
        //    }
        //}

        [HttpPost]
        public async Task<ActionResult<FilialDto>> Post([FromBody] CriarFilialDto dto, CancellationToken ct = default)
        {
            try
            {
                var filial = await _filialService.CriarFilialAsync(dto, ct);
                return Ok(filial);
                //return CreatedAtAction(nameof(GetById), new { id = filial.IdFilial }, filial);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar filial");
                return StatusCode(500, new { Message = "Erro interno ao processar a solicitação" });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(
            int id,
            [FromBody] AtualizarFilialDto dto,
            CancellationToken ct = default)
        {
            
            var atualizado = await _filialService.AtualizarFilialAsync(id, dto, ct);

            if (!atualizado)
                return StatusCode(500, new { Message = "Erro ao atualizar filial" });

            return NoContent();           
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct = default)
        {
            var excluido = await _filialService.ExcluirFilialAsync(id, ct);
            if (!excluido)
                return StatusCode(500, new { Message = "Erro ao excluir filial" });

            return NoContent();          
        }

        [HttpPatch("{id}/ativar")]
        public async Task<IActionResult> Ativar(int id, CancellationToken ct = default)
        {            
            var ativado = await _filialService.AtivarFilialAsync(id, ct);
            if (!ativado)
                return StatusCode(500, new { Message = "Erro ao ativar filial" });

            return Ok(new { Message = "Filial ativada com sucesso" });            
        }

        [HttpPatch("{id}/desativar")]
        public async Task<IActionResult> Desativar(int id, CancellationToken ct = default)
        {            
            var desativado = await _filialService.DesativarFilialAsync(id, ct);
            if (!desativado)
                return StatusCode(500, new { Message = "Erro ao desativar filial" });

            return Ok(new { Message = "Filial desativada com sucesso" });           
        }

        //[HttpPost("{id}/transferir-veiculo")]
        //public async Task<IActionResult> TransferirVeiculo(
        //    int id,
        //    [FromBody] TransferirVeiculoDto dto,
        //    CancellationToken ct = default)
        //{
        //    try
        //    {
        //        var transferido = await _filialService.TransferirVeiculoAsync(
        //            dto.VeiculoId, id, dto.FilialDestinoId, ct);

        //        if (!transferido)
        //            return StatusCode(500, new { Message = "Erro ao transferir veículo" });

        //        return Ok(new { Message = "Veículo transferido com sucesso" });
        //    }
        //    catch (KeyNotFoundException ex)
        //    {
        //        return NotFound(new { Message = ex.Message });
        //    }
        //    catch (InvalidOperationException ex)
        //    {
        //        return BadRequest(new { Message = ex.Message });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Erro ao transferir veículo da filial ID: {Id}", id);
        //        return StatusCode(500, new { Message = "Erro interno ao processar a solicitação" });
        //    }
        //}
    }
    public class TransferirVeiculoDto
    {
        public int VeiculoId { get; set; }
        public int FilialDestinoId { get; set; }
    }
}
