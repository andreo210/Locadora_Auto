using Locadora_Auto.Api.V1.Controllers;
using Locadora_Auto.Application.Configuration.Ultils.NotificadorServices;
using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Application.Services.ClienteServices;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Locadora_Auto.API.Controllers
{
    /// <summary>
    /// Controller para gerenciamento de clientes
    /// </summary>
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    public class ClientesController : MainController
    {
        private readonly IClienteService _clienteService;
        private readonly ILogger<ClientesController> _logger;

        public ClientesController(IClienteService clienteService, INotificadorService notificador, ILogger<ClientesController> logger) : base(notificador)
        {
            _clienteService = clienteService ?? throw new ArgumentNullException(nameof(clienteService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Obtém todos os clientes
        /// </summary>
        /// <param name="nome">Filtrar por nome (opcional)</param>
        /// <param name="somenteAtivos">Filtrar apenas clientes ativos (opcional, default: false)</param>
        /// <param name="pageNumber">Número da página (opcional, default: 1)</param>
        /// <param name="pageSize">Tamanho da página (opcional, default: 10)</param>
        /// <param name="ct">Token de cancelamento</param>
        /// <returns>Lista de clientes</returns>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ClienteDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<ClienteDto>>> Get(
            [FromQuery] string? nome = null,
            [FromQuery] bool somenteAtivos = false,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken ct = default)
        {           
 
            IReadOnlyList<ClienteDto> clientes;

            if (!string.IsNullOrWhiteSpace(nome))
            {
                clientes = await _clienteService.ObterPorNomeAsync(nome, ct);
            }
            else if (somenteAtivos)
            {
                clientes = await _clienteService.ObterAtivosAsync(ct);
            }
            else if (pageNumber > 0 && pageSize > 0)
            {
                clientes = await _clienteService.ObterPaginadoAsync(pageNumber, pageSize, ct);
            }
            else
            {
                clientes = await _clienteService.ObterTodosAsync(ct);
            }

            return Ok(clientes);           
        }

        /// <summary>
        /// Obtém um cliente específico pelo ID
        /// </summary>
        /// <param name="id">ID do cliente</param>
        /// <param name="ct">Token de cancelamento</param>
        /// <returns>Dados do cliente</returns>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ClienteDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ClienteDto>> GetById(
            [FromRoute] int id,
            CancellationToken ct = default)
        {          

            var cliente = await _clienteService.ObterPorIdAsync(id, ct);

            if (cliente == null)
            {
                return NotFound($"Cliente com ID {id} não encontrado" );
            }

            return Ok(cliente);            
        }

        /// <summary>
        /// Obtém um cliente pelo CPF
        /// </summary>
        /// <param name="cpf">CPF do cliente (com ou sem formatação)</param>
        /// <param name="ct">Token de cancelamento</param>
        /// <returns>Dados do cliente</returns>
        [HttpGet("cpf/{cpf}")]
        [ProducesResponseType(typeof(ClienteDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ClienteDto>> GetByCpf(
            [FromRoute] string cpf,
            CancellationToken ct = default)
        {
            var cliente = await _clienteService.ObterPorCpfAsync(cpf, ct);

            if (cliente == null)
            {
                return NotFound( $"Cliente com CPF {cpf} não encontrado");
            }
            return Ok(cliente);
           
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
        public async Task<ActionResult<ClienteDto>> Post([FromBody] CriarClienteDto clienteDto, CancellationToken ct = default)
        {
            var cliente = await _clienteService.CriarClienteAsync(clienteDto, ct);
            return CustomResponse(cliente,HttpStatusCode.Created);  
        }

        /// <summary>
        /// Atualiza um cliente existente
        /// </summary>
        /// <param name="id">ID do cliente</param>
        /// <param name="clienteDto">Dados atualizados do cliente</param>
        /// <param name="ct">Token de cancelamento</param>
        /// <returns>Resultado da operação</returns>
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Put([FromRoute] int id, [FromBody] AtualizarClienteDto clienteDto, CancellationToken ct = default)
        {
            var atualizado = await _clienteService.AtualizarClienteAsync(id, clienteDto, ct);            
            return CustomResponse(new { Message = "Cliente atualizado com sucesso" });           
        }

        /// <summary>
        /// Exclui um cliente
        /// </summary>
        /// <param name="id">ID do cliente</param>
        /// <param name="ct">Token de cancelamento</param>
        /// <returns>Resultado da operação</returns>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken ct = default)
        {
            var excluido = await _clienteService.ExcluirClienteAsync(id, ct);
            if (!excluido)
            {
                return ProblemResponse(HttpStatusCode.InternalServerError, "Erro ao excluir cliente");
            }
            return NoContent();           
        }

        /// <summary>
        /// Ativa um cliente
        /// </summary>
        /// <param name="id">ID do cliente</param>
        /// <param name="ct">Token de cancelamento</param>
        /// <returns>Resultado da operação</returns>
        [HttpPatch("{id:int}/ativar")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Ativar( [FromRoute] int id, CancellationToken ct = default)
        {  
            var ativado = await _clienteService.AtivarClienteAsync(id, ct);
            if (!ativado)
            {
                return ProblemResponse(HttpStatusCode.InternalServerError, "Erro ao ativar cliente");
            }
            return Ok(new { Message = "Cliente ativado com sucesso" });            
        }

        /// <summary>
        /// Desativa um cliente
        /// </summary>
        /// <param name="id">ID do cliente</param>
        /// <param name="ct">Token de cancelamento</param>
        /// <returns>Resultado da operação</returns>
        [HttpPatch("{id:int}/desativar")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Desativar([FromRoute] int id,  CancellationToken ct = default)
        {
            var desativado = await _clienteService.DesativarClienteAsync(id, ct);

            if (!desativado)
            {
                return ProblemResponse(HttpStatusCode.InternalServerError, "Erro ao desativar cliente");
            }
            return Ok(new { Message = "Cliente desativado com sucesso" });           
        }

        ///// <summary>
        ///// Verifica se um cliente está apto para locação
        ///// </summary>
        ///// <param name="id">ID do cliente</param>
        ///// <param name="ct">Token de cancelamento</param>
        ///// <returns>Resultado da validação</returns>
        //[HttpGet("{id:int}/validar-locacao")]
        //[ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        //[ProducesResponseType(StatusCodes.Status404NotFound)]
        //[ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //public async Task<ActionResult<object>> ValidarParaLocacao(
        //    [FromRoute] int id,
        //    CancellationToken ct = default)
        //{
        //    try
        //    {
        //        _logger.LogInformation("Validando cliente para locação: ID {Id}", id);

        //        var valido = await _clienteService.ValidarClienteParaLocacaoAsync(id, ct);

        //        if (!valido)
        //        {
        //            return Ok(new
        //            {
        //                Valido = false,
        //                Message = "Cliente não está apto para locação. Verifique: habilitação válida, status ativo, e pendências financeiras."
        //            });
        //        }

        //        return Ok(new
        //        {
        //            Valido = true,
        //            Message = "Cliente apto para locação"
        //        });
        //    }
        //    catch (KeyNotFoundException ex)
        //    {
        //        _logger.LogWarning(ex, "Cliente não encontrado para validação: ID {Id}", id);
        //        return NotFound(new { Message = ex.Message });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Erro ao validar cliente para locação ID: {Id}", id);
        //        return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Erro interno ao processar a solicitação" });
        //    }
        //}

        /// <summary>
        /// Verifica se um CPF já está cadastrado
        /// </summary>
        /// <param name="cpf">CPF a verificar</param>
        /// <param name="ct">Token de cancelamento</param>
        /// <returns>Disponibilidade do CPF</returns>
        [HttpGet("verificar-cpf/{cpf}")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<object>> VerificarCpf([FromRoute] string cpf, CancellationToken ct = default)
        {            
            var existe = await _clienteService.ExisteClienteAsync(cpf, ct);
            return Ok(new
            {
                Disponivel = !existe,
                Message = existe ? "CPF já cadastrado" : "CPF disponível"
            });            
        }

        /// <summary>
        /// Conta o número de clientes ativos
        /// </summary>
        /// <param name="ct">Token de cancelamento</param>
        /// <returns>Quantidade de clientes ativos</returns>
        [HttpGet("contar-ativos")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<object>> ContarAtivos(CancellationToken ct = default)
        {
            var quantidade = await _clienteService.ContarClientesAtivosAsync(ct);

            return Ok(new
            {
                Quantidade = quantidade,
                Message = $"Total de {quantidade} cliente(s) ativo(s)"
            });           
        }
    }
}