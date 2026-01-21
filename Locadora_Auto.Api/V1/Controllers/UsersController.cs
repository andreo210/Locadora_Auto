using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Application.Services.Notificador;
using Locadora_Auto.Application.Services.OAuth.Roles;
using Locadora_Auto.Application.Services.OAuth.Token;
using Locadora_Auto.Application.Services.OAuth.Users;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Net;

namespace Locadora_Auto.Api.V1.Controllers
{

    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    public class UsersController : MainController
    {
        private readonly IUserService _userService;
        private readonly IRoleService _roleService;
        private readonly ITokenService _tokenService;
        public UsersController(IUserService userService, IRoleService roleService, ITokenService tokenService, INotificador notificador) : base(notificador)
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
                return NotFound("Usúario não encontrado.");

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
                return NotFound("Usuário não encontrado");

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
        public async Task<IActionResult> Update(string id, [FromBody] CreateUserDto dto)
        {
            var existingUser = await _userService.ObterPorIdAsync(id);
            if (existingUser == null)
                return NotFound();

            var updated = await _userService.AtualizarAsync(id);

            if (!updated)
                return ProblemResponse(HttpStatusCode.BadRequest, "Não foi possível atualizar o usuário.");

            return NoContent();
        }

        ///// <summary>
        ///// Desativa (exclui) um usuário.
        ///// </summary>
        ///// <param name="id">ID do usuário a ser desativado</param>
        ///// <returns>Status da operação</returns>
        //[HttpDelete("{id:guid}")]
        //[ProducesResponseType(StatusCodes.Status204NoContent)]
        //[ProducesResponseType(StatusCodes.Status404NotFound)]
        //[ProducesDefaultResponseType]
        //public async Task<IActionResult> Delete(string id)
        //{
        //    var existingUser = await _userService.ObterPorIdAsync(id);
        //    if (existingUser == null)
        //        return ProblemResponse(HttpStatusCode.NotFound, "usuario não encontrado");

        //    await _userService.DesativarAsync(id);
        //    return NoContent();
        //}


        [HttpPost("autenticar")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> Login(LoginDto usuarioLogin)
        {
            if (!ModelState.IsValid) return ValidationResponse(ModelState);
            

            var result = await _userService.LoginAsync(usuarioLogin);

            if (result.Succeeded)
            {
                return OkResponse(await _tokenService.GerarToken(usuarioLogin.UserName));
            }

            if (result.IsLockedOut)
            {
                return ValidationResponse("BLOQUEADO", "Usuário temporariamente bloqueado por tentativas inválidas");
            }

            return ValidationResponse("Usuário ou Senha incorretos", "Usuário ou Senha incorretos");
        }

        //[Authorize]
        [HttpPost("renovar")]
        public async Task<IActionResult> Renovar([FromBody] string refreshToken)
        {
            var (idRefreshToken, validade) = ObterIdToken(refreshToken);
            if (validade<DateTime.Now) return ValidationResponse("Token", "token expirado");

            var user = await _userService.DesativarToken(idRefreshToken);
            if (user != null)
            {
                return Ok(await _tokenService.GerarToken(user.UserName));
            }
            return ValidationResponse("Token inválido", "Token inválido");
        }



        private (string token ,DateTime validade)  ObterIdToken(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);

            // Ler claims
            var idrefreshToken = jwt.Claims.First(c => c.Type == "jti").Value;
            var expiracao = Convert.ToInt32(jwt.Claims.First(c => c.Type == "exp").Value);
            DateTime data = DateTimeOffset.FromUnixTimeSeconds(expiracao).LocalDateTime;
            return (idrefreshToken, data);
        }
    }
}
