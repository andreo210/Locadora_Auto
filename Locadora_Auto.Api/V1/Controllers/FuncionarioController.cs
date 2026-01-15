using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Application.Services.FuncionarioServices;
using Locadora_Auto.Application.Services.Notificador;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Locadora_Auto.Api.V1.Controllers
{
    [ApiController]
    [Route("api/funcionarios")]
    //[Authorize(Policy = "Gerente")] // Apenas gerentes podem gerenciar funcionários
    public class FuncionariosController : MainController
    {
        private readonly IFuncionarioService _funcionarioService;

        public FuncionariosController(IFuncionarioService funcionarioService, INotificador notificador) : base(notificador)
        {
            _funcionarioService = funcionarioService;
        }

       

        /// <summary>
        /// Cria um novo cliente
        /// </summary>
        /// <param name="clienteDto">Dados do cliente a ser criado</param>
        /// <param name="ct">Token de cancelamento</param>
        /// <returns>Cliente criado</returns>
        [HttpPost]
        [ProducesResponseType(typeof(ClienteDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<FuncionarioDto>> Post([FromBody] CriarFuncionarioDto dto, CancellationToken ct)
        {
            var funcionario = await _funcionarioService.CriarFuncionarioAsync(dto, ct);
            return CustomResponse(HttpStatusCode.Created);
        }

        //[HttpPost("{id}/atribuir-locacao/{locacaoId}")]
        //public async Task<IActionResult> AtribuirLocacao(
        //    [FromRoute] int id, [FromRoute] int locacaoId, CancellationToken ct)
        //{
        //    var sucesso = await _funcionarioService.AtribuirLocacaoAsync(id, locacaoId, ct);
        //    return sucesso ? Ok() : BadRequest();
        //}

        //[HttpGet]
        //public async Task<ActionResult<EstatisticasFuncionariosDto>> GetEstatisticas(CancellationToken ct)
        //{
        //    var funcionarios = await _funcionarioService.ObterTodosAsync(ct);
        //    return Ok(funcionarios);
        //}
    }
}
