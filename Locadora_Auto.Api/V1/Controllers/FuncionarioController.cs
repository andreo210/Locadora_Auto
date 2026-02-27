using Locadora_Auto.Api.Controllers;
using Locadora_Auto.Application.Configuration.Ultils.NotificadorServices;
using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Application.Services.FuncionarioServices;
using Locadora_Auto.Domain;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;
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

        public FuncionariosController(IFuncionarioService funcionarioService, INotificadorService notificador) : base(notificador)
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
            return CustomResponse(funcionario,HttpStatusCode.Created);
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
        public async Task<ActionResult<FuncionarioDto>> ObterFuncionario([FromQuery] string? cpf = null,[FromQuery] string? matricula = null,[FromQuery] string? usuarioId = null,  CancellationToken ct = default )
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


        [HttpGet("disponibilidade-matricula")]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<int>> DisponibilidadeMatricula(string? matricula, int? idExcluir, CancellationToken ct)
        {
            return Ok(await _funcionarioService.VerificarDisponibilidadeMatriculaAsync(matricula,idExcluir,ct));
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

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
        public async Task<ActionResult<PaginatedResult<FuncionarioDto>>> ObterFuncionariosPaginados(
            [FromQuery] bool? ativos = null,
            [FromQuery] string? nome = null,
            [FromQuery] string? cargo = null,
            [FromQuery] string? ordenarPor = null,
            [FromQuery] string? ordem = null,
            [FromQuery] int pagina = 1,
            [FromQuery] int itensPorPagina = 10,
            CancellationToken ct = default)
        {
            // Validação dos parâmetros de paginação
            if (pagina < 1) pagina = 1;
            if (itensPorPagina < 1) itensPorPagina = 10;
            if (itensPorPagina > 100) itensPorPagina = 100; // Limite máximo

            var resultado = await _funcionarioService.ObterPaginadoAsync(
                ativos: ativos,
                nome: nome,
                cargo: cargo,
                ordenarPor: ordenarPor,
                ordem: ordem,
                pagina: pagina,
                itensPorPagina: itensPorPagina,
                ct: ct
            );

            // Adicionar cabeçalhos de paginação na resposta
            Response.Headers.Add("X-Total-Items", resultado.Total.ToString());
            Response.Headers.Add("X-Total-Pages", resultado.TotalPaginas.ToString());
            Response.Headers.Add("X-Current-Page", resultado.Pagina.ToString());
            Response.Headers.Add("X-Page-Size", resultado.ItensPorPagina.ToString());

            return Ok(resultado);
        }

        // Manter método antigo se necessário para compatibilidade
        [HttpGet("todos")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<List<FuncionarioDto>>> ObterTodosFuncionarios(
            [FromQuery] bool? ativos = null,
            [FromQuery] string? nome = null,
            [FromQuery] string? cargo = null,
            CancellationToken ct = default)
        {
            var resultado = await _funcionarioService.ObterComFiltroAsync(
                ativos: ativos,
                nome: nome,
                cargo: cargo,
                ct: ct
            );

            return Ok(resultado);
        }

        /// <summary>
        /// Exclui um funcionario
        /// </summary>
        /// <param name="id">ID do funcionariio</param>
        /// <param name="ct">Token de cancelamento</param>
        /// <returns>Resultado da operação</returns>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken ct = default)
        {
            var excluido = await _funcionarioService.ExcluirFuncionarioAsync(id, ct);
            if (!excluido)
            {
                return ProblemResponse(HttpStatusCode.InternalServerError, "Erro ao excluir funcionario");
            }
            return NoContent();
        }


    }
}
