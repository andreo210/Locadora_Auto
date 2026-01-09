using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Application.Services;
using Locadora_Auto.Application.Services.Cliente;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;

namespace Locadora_Auto.API.Controllers
{
    /// <summary>
    /// Controller para gerenciamento de clientes
    /// </summary>
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    public class ClientesController : ControllerBase
    {
        private readonly IClienteService _clienteService;
        private readonly ILogger<ClientesController> _logger;

        public ClientesController(
            IClienteService clienteService,
            ILogger<ClientesController> logger)
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
            try
            {
                _logger.LogInformation("Consultando clientes. Filtros: Nome={Nome}, Ativos={Ativos}, Pagina={Pagina}, Tamanho={Tamanho}",
                    nome, somenteAtivos, pageNumber, pageSize);

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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao consultar clientes");
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Erro interno ao processar a solicitação" });
            }
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
            try
            {
                _logger.LogInformation("Consultando cliente por ID: {Id}", id);

                var cliente = await _clienteService.ObterPorIdAsync(id, ct);

                if (cliente == null)
                {
                    _logger.LogWarning("Cliente não encontrado: ID {Id}", id);
                    return NotFound(new { Message = $"Cliente com ID {id} não encontrado" });
                }

                return Ok(cliente);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao consultar cliente por ID: {Id}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Erro interno ao processar a solicitação" });
            }
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
            try
            {
                _logger.LogInformation("Consultando cliente por CPF: {Cpf}", cpf);

                var cliente = await _clienteService.ObterPorCpfAsync(cpf, ct);

                if (cliente == null)
                {
                    _logger.LogWarning("Cliente não encontrado: CPF {Cpf}", cpf);
                    return NotFound(new { Message = $"Cliente com CPF {cpf} não encontrado" });
                }

                return Ok(cliente);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao consultar cliente por CPF: {Cpf}", cpf);
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Erro interno ao processar a solicitação" });
            }
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
        public async Task<ActionResult<ClienteDto>> Post(
            [FromBody] CriarClienteDto clienteDto,
            CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("Criando novo cliente: {Nome}", clienteDto.Nome);

                var cliente = await _clienteService.CriarClienteAsync(clienteDto, ct);

                _logger.LogInformation("Cliente criado com sucesso: ID {Id}", cliente.IdCliente);

                return CreatedAtAction(
                    nameof(GetById),
                    new { id = cliente.IdCliente },
                    cliente);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Dados inválidos ao criar cliente: {Message}", ex.Message);
                return BadRequest(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Conflito ao criar cliente: {Message}", ex.Message);
                return Conflict(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar cliente");
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Erro interno ao processar a solicitação" });
            }
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
        public async Task<IActionResult> Put(
            [FromRoute] int id,
            [FromBody] AtualizarClienteDto clienteDto,
            CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("Atualizando cliente ID: {Id}", id);

                var atualizado = await _clienteService.AtualizarClienteAsync(id, clienteDto, ct);

                if (!atualizado)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Erro ao atualizar cliente" });
                }

                _logger.LogInformation("Cliente atualizado com sucesso: ID {Id}", id);
                return Ok(new { Message = "Cliente atualizado com sucesso" });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Cliente não encontrado para atualização: ID {Id}", id);
                return NotFound(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Operação inválida ao atualizar cliente: {Message}", ex.Message);
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar cliente ID: {Id}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Erro interno ao processar a solicitação" });
            }
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
        public async Task<IActionResult> Delete(
            [FromRoute] int id,
            CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("Excluindo cliente ID: {Id}", id);

                var excluido = await _clienteService.ExcluirClienteAsync(id, ct);

                if (!excluido)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Erro ao excluir cliente" });
                }

                _logger.LogInformation("Cliente excluído com sucesso: ID {Id}", id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Cliente não encontrado para exclusão: ID {Id}", id);
                return NotFound(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Operação inválida ao excluir cliente: {Message}", ex.Message);
                return Conflict(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao excluir cliente ID: {Id}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Erro interno ao processar a solicitação" });
            }
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
        public async Task<IActionResult> Ativar(
            [FromRoute] int id,
            CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("Ativando cliente ID: {Id}", id);

                var ativado = await _clienteService.AtivarClienteAsync(id, ct);

                if (!ativado)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Erro ao ativar cliente" });
                }

                _logger.LogInformation("Cliente ativado com sucesso: ID {Id}", id);
                return Ok(new { Message = "Cliente ativado com sucesso" });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Cliente não encontrado para ativação: ID {Id}", id);
                return NotFound(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Operação inválida ao ativar cliente: {Message}", ex.Message);
                return Conflict(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao ativar cliente ID: {Id}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Erro interno ao processar a solicitação" });
            }
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
        public async Task<IActionResult> Desativar(
            [FromRoute] int id,
            CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("Desativando cliente ID: {Id}", id);

                var desativado = await _clienteService.DesativarClienteAsync(id, ct);

                if (!desativado)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Erro ao desativar cliente" });
                }

                _logger.LogInformation("Cliente desativado com sucesso: ID {Id}", id);
                return Ok(new { Message = "Cliente desativado com sucesso" });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Cliente não encontrado para desativação: ID {Id}", id);
                return NotFound(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Operação inválida ao desativar cliente: {Message}", ex.Message);
                return Conflict(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao desativar cliente ID: {Id}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Erro interno ao processar a solicitação" });
            }
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
        public async Task<ActionResult<object>> VerificarCpf(
            [FromRoute] string cpf,
            CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("Verificando disponibilidade do CPF: {Cpf}", cpf);

                var existe = await _clienteService.ExisteClienteAsync(cpf, ct);

                return Ok(new
                {
                    Disponivel = !existe,
                    Message = existe ? "CPF já cadastrado" : "CPF disponível"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar CPF: {Cpf}", cpf);
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Erro interno ao processar a solicitação" });
            }
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
            try
            {
                _logger.LogInformation("Contando clientes ativos");

                var quantidade = await _clienteService.ContarClientesAtivosAsync(ct);

                return Ok(new
                {
                    Quantidade = quantidade,
                    Message = $"Total de {quantidade} cliente(s) ativo(s)"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao contar clientes ativos");
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Erro interno ao processar a solicitação" });
            }
        }
    }
}