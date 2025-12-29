using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Application.Models.Mappers;
using Locadora_Auto.Domain.Entidades.Indentity;
using Locadora_Auto.Domain.IRepositorio;
using Locadora_Auto.Infra.Data.Repositorio;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Locadora_Auto.Application.Services.OAuth.Users
{
    public class UserService : IUserService
    {
        private readonly UserManager<User> _userManager;
        private readonly IUserRepository _userRepository;
        private readonly SignInManager<User> _signInManager;
        private readonly ITokenRepository _tokenRepository;

        public UserService(
            UserManager<User> userManager,
            IUserRepository userRepository,
            SignInManager<User> signInManager,
            ITokenRepository tokenRepository)
        {
            _userManager = userManager;
            _userRepository = userRepository;
            _signInManager = signInManager;
            _tokenRepository = tokenRepository;
        }

        public async Task<UserDto> CriarAsync(CreateUserDto dto)
        {
            var user = dto.CreateToEntity();

            var result = await _userManager.CreateAsync(user, dto.Password);

            if (!result.Succeeded)
                throw new InvalidOperationException(
                    string.Join(", ", result.Errors.Select(e => e.Description))
                );

            return user.ToDto();
        }


        public async Task<SignInResult> LoginAsync(LoginDto dto)
        {
            return await _signInManager.PasswordSignInAsync(dto.UserName, dto.Password, false, true);            
        }

        public async Task<User> DesativarToken(string refreshToken)
        {
            var refreshTokenDB = await _tokenRepository.ObterPrimeiroAsync(x=>x.Token == refreshToken);

            if (refreshTokenDB == null)
                //return BadRequest("token inválido");

            if (refreshTokenDB.ExpiraEm < DateTime.Now)
                //return BadRequest("token expirado");

            //RefreshToken antigo - Atualizar - Desativar esse refreshToken
            refreshTokenDB.Revogado = true;
            var token = await _tokenRepository.AtualizarAsync(refreshTokenDB);

            return await _userRepository.ObterPorId(refreshTokenDB.UserId);;
        }



        public async Task DesativarAsync(string id)
        {
            await _userRepository.ExcluirAsync(id);
        }

        public async Task<IEnumerable<UserDto>> ListarAsync()
        {
            var model = await _userRepository.ObterTodos().ToListAsync();
             return model.ToDtoList();
        }

        public async Task<UserDto?> ObterPorCpfAsync(string cpf)
        {
            var UserName = cpf;
            var user = await _userRepository.ObterPrimeiroAsync(x => x.UserName == UserName);
            return user!.ToDto();
        }

        public async Task<User?> ObterPorCpf(string cpf)
        {
            var UserName = cpf;
            var user = await _userRepository.ObterPrimeiroAsync(x => x.UserName == UserName);
            return user;
        }

        public async Task<UserDto?> ObterPorIdAsync(string id)
        {
            var user = await _userRepository.ObterPorId(id);
            return user!.ToDto();
        }

        public async Task<UserDto?> ObterPorEmail(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            return user!.ToDto();
        }

        public virtual async Task<bool> AtualizarAsync(string id)
        {
            var model = await _userRepository.ObterPorId(id);
            return await _userRepository.AtualizarAsync(model);
        }
    }
    
}
