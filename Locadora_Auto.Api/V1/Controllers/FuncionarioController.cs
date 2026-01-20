using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Application.Services.FuncionarioServices;
using Locadora_Auto.Application.Services.Notificador;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Locadora_Auto.Api.V1.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
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
        [ProducesResponseType(typeof(FuncionarioDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<FuncionarioDto>> Post([FromBody] CriarFuncionarioDto dto, CancellationToken ct)
        {
            var funcionario = await _funcionarioService.CriarFuncionarioAsync(dto, ct);
            return CustomResponse(HttpStatusCode.Created);
        }

        /// <summary>
        /// Atualiza um funcionario existente
        /// </summary>
        /// <param name="id">ID do funcionario</param>
        /// <param name="funcionarioDto">Dados atualizados do funcionario</param>
        /// <param name="ct">Token de cancelamento</param>
        /// <returns>Resultado da operação</returns>
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Put([FromRoute] int id, [FromBody] AtualizarFuncionarioDto funcionarioDto, CancellationToken ct = default)
        {
            var atualizado = await _funcionarioService.AtualizarFuncionarioAsync(id, funcionarioDto, ct);
            return CustomResponse(new { Message = "Funcionario atualizado com sucesso" });
        }

        //[HttpPost("{id}/atribuir-locacao/{locacaoId}")]
        //public async Task<IActionResult> AtribuirLocacao(
        //    [FromRoute] int id, [FromRoute] int locacaoId, CancellationToken ct)
        //{
        //    var sucesso = await _funcionarioService.AtribuirLocacaoAsync(id, locacaoId, ct);
        //    return sucesso ? Ok() : BadRequest();
        //}

        [HttpGet]
        public async Task<ActionResult<EstatisticasFuncionariosDto>> ObterTodos(CancellationToken ct)
        {
            var funcionarios = await _funcionarioService.ObterTodosAsync(ct);
            return Ok(funcionarios);
        }


        /// <summary>
        /// Obtém um funcionario pelo CPF
        /// </summary>
        /// <param name="cpf">CPF do funcionario (com ou sem formatação)</param>
        /// <param name="ct">Token de cancelamento</param>
        /// <returns>Dados do funcionario</returns>
        [HttpGet("obter-funcionario")]
        [ProducesResponseType(typeof(FuncionarioDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<FuncionarioDto>> ObterFuncionario(
            [FromQuery] string? cpf = null,
            [FromQuery] string? matricula = null,
            [FromQuery] string? usuarioId = null,
            CancellationToken ct = default
            )
        {

            if (!string.IsNullOrWhiteSpace(cpf))
            {
                var model = await _funcionarioService.ObterPorFuncionarioCpfAsync(cpf, ct);
                if (model != null) return Ok(model);
                return NotFound();
            }
            else if (!string.IsNullOrWhiteSpace(matricula))
            {
                var model = await _funcionarioService.ObterPorMatriculaAsync(matricula, ct);
                if (model != null) return Ok(model);
                return NotFound();
            }        
            else if (!string.IsNullOrWhiteSpace(usuarioId))
            {
                var model = await _funcionarioService.ObterPorUsuarioIdAsync(usuarioId, ct);
                if (model != null) return Ok(model);
                return NotFound();
            }
            return NotFound();
        }

        [HttpGet("existe")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<bool>> VerificaExiste(
            [FromQuery] string? cpf = null,
            [FromQuery] string? matricula = null,
            CancellationToken ct = default)
        {

            if (!string.IsNullOrWhiteSpace(cpf))
            {
                return await _funcionarioService.ExisteFuncionarioPorCpfAsync(cpf, ct);
            }
            else if (!string.IsNullOrWhiteSpace(matricula))
            {
                return await _funcionarioService.ExisteFuncionarioAsync(matricula, ct);
            }
            
            return Ok(false);
        }

        [HttpGet("contar-ativos")]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<int>> ContarAtivos()
        {
            return Ok(await _funcionarioService.ContarFuncionariosAtivosAsync());
        }

        /// <summary>
        /// Ativa um funcionario
        /// </summary>
        /// <param name="id">ID do funcionario</param>
        /// <param name="ct">Token de cancelamento</param>
        /// <returns>Resultado da operação</returns>
        [HttpPatch("{id:int}/ativar")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Ativar([FromRoute] int id, CancellationToken ct = default)
        {
            var ativado = await _funcionarioService.AtivarFuncionarioAsync(id, ct);
            if (!ativado)
            {
                return ProblemResponse(HttpStatusCode.InternalServerError, "Erro ao ativar funcionario");
            }
            return Ok(new { Message = "Funcionario ativado com sucesso" });
        }

        /// <summary>
        /// Desativa um funcionario
        /// </summary>
        /// <param name="id">ID do funcionario</param>
        /// <param name="ct">Token de cancelamento</param>
        /// <returns>Resultado da operação</returns>
        [HttpPatch("{id:int}/desativar")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Desativar([FromRoute] int id, CancellationToken ct = default)
        {
            var desativado = await _funcionarioService.DesativarFuncionarioAsync(id, ct);

            if (!desativado)
            {
                return ProblemResponse(HttpStatusCode.InternalServerError, "Erro ao desativar funcionario");
            }
            return Ok(new { Message = "Funcionario desativado com sucesso" });
        }
    }
}
