using Locadora_Auto.Application.Extensions;
using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Application.Services.OAuth.Roles;
using Locadora_Auto.Application.Services.OAuth.Token;
using Locadora_Auto.Application.Services.OAuth.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Locadora_Auto.Api.V1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IRoleService _roleService;
        private readonly ITokenService _tokenService;
        public UsersController(IUserService userService, IRoleService roleService, ITokenService tokenService)
        {
            _userService = userService;
            _roleService = roleService;
            _tokenService = tokenService;
        }


        [HttpPost("{userId:guid}/roles/{role}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> AtribuirRole(string userId, string role)
        {
            await _roleService.AtribuirRoleAsync(userId, role);
            return NoContent();
        }

        [HttpGet("{userId:guid}/roles")]
        public async Task<IActionResult> ObterRoles(string userId)
        {
            var roles = await _roleService.ObterRolesAsync(userId);
            return Ok(roles);
        }

        [HttpDelete("{userId:guid}/roles/{role}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> RemoverRole(string userId, string role)
        {
            await _roleService.RemoverRoleAsync(userId, role);
            return NoContent();
        }


        /// <summary>
        /// Lista todos os usuários.
        /// </summary>
        /// <returns>Lista de usuários</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetAll()
        {
            var users = await _userService.ListarAsync();
            return Ok(users);
        }

        /// <summary>
        /// Obtém um usuário pelo ID.
        /// </summary>
        /// <param name="id">ID do usuário</param>
        /// <returns>Usuário correspondente</returns>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
        public async Task<ActionResult<UserDto>> GetById(string id)
        {
            var user = await _userService.ObterPorIdAsync(id);
            if (user == null)
                return NotFound(ProblemFactory.Create(HttpStatusCode.NotFound, "Usúario não encontrado."));

            return Ok(user);
        }

        /// <summary>
        /// Obtém um usuário pelo CPF.
        /// </summary>
        /// <param name="cpf">CPF do usuário</param>
        /// <returns>Usuário correspondente</returns>
        [HttpGet("cpf/{cpf}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
        public async Task<ActionResult<UserDto>> GetByCpf(string cpf)
        {
            var user = await _userService.ObterPorCpfAsync(cpf);
            if (user == null)
                return NotFound(ProblemFactory.Create(HttpStatusCode.NotFound, "Usúario não encontrado."));

            return Ok(user);
        }

        /// <summary>
        /// Cria um novo usuário.
        /// </summary>
        /// <param name="dto">Dados do usuário a ser criado</param>
        /// <returns>Usuário criado</returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesDefaultResponseType]
        public async Task<ActionResult<UserDto>> Create([FromBody] CreateUserDto usuarioRegistro)
        {

            var usuario = await _userService.ObterPorEmail(usuarioRegistro.Email);

            if (usuario != null)
            {
                throw new Exception("Usuário já cadastrado");
            }
            if (usuarioRegistro.Password != usuarioRegistro.RepeatPassword)
            {
                throw new Exception("Senha diferente");
            }
            var user =await _userService.CriarAsync(usuarioRegistro);
            return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
        }

        /// <summary>
        /// Atualiza um usuário existente.
        /// </summary>
        /// <param name="id">ID do usuário a ser atualizado</param>
        /// <param name="dto">Dados atualizados do usuário</param>
        /// <returns>Resultado da atualização</returns>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
        public async Task<ActionResult> Update(string id, [FromBody] CreateUserDto dto)
        {
            var existingUser = await _userService.ObterPorIdAsync(id);
            if (existingUser == null)
                return NotFound();

            var updated = await _userService.AtualizarAsync(id);

            if (!updated)
                return BadRequest(ProblemFactory.Create(HttpStatusCode.BadRequest, "Não foi possível atualizar o usuário."));

            return NoContent();
        }

        /// <summary>
        /// Desativa (exclui) um usuário.
        /// </summary>
        /// <param name="id">ID do usuário a ser desativado</param>
        /// <returns>Status da operação</returns>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
        public async Task<ActionResult> Delete(string id)
        {
            var existingUser = await _userService.ObterPorIdAsync(id);
            if (existingUser == null)
                return NotFound();

            await _userService.DesativarAsync(id);
            return NoContent();
        }


        [HttpPost("autenticar")]
        public async Task<ActionResult> Login(LoginDto usuarioLogin)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _userService.LoginAsync(usuarioLogin);

            if (result.Succeeded)
            {
                return Ok(await _tokenService.GerarToken(usuarioLogin.UserName));
            }

            if (result.IsLockedOut)
            {
                return BadRequest("Usuário temporariamente bloqueado por tentativas inválidas");
            }

            return BadRequest("Usuário ou Senha incorretos");
        }

        //[Authorize]
        [HttpPost("renovar")]
        public async Task<IActionResult> Renovar([FromBody] string refreshToken)
        {
            
            var user = await _userService.DesativarToken(refreshToken);
            if (user != null)
            {
                return Ok(await _tokenService.GerarToken(user.UserName));
            }
            return BadRequest("Token inválido");
        }
    }
}
