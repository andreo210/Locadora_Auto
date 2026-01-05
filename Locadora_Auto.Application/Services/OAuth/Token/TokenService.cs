using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Application.Services.OAuth.Users;
using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Domain.Entidades.Indentity;
using Locadora_Auto.Domain.IRepositorio;
using Locadora_Auto.Infra.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

namespace Locadora_Auto.Application.Services.OAuth.Token
{
    public class TokenService : ITokenService
    {

        private readonly UserManager<User> _userManager;
        private readonly IUsersAsp _aspNetUser;
        private readonly IUserService _userService;
        private readonly RsaKeyService _rsaKeyService;
        private readonly ITokenRepository _tokenRepository;

        public TokenService(
            UserManager<User> userManager,
            ITokenRepository tokenRepository,
            IUsersAsp aspNetUser,
            RsaKeyService rsaKeyService
,
            IUserService userService)
        {
            _userManager = userManager;
            _tokenRepository = tokenRepository;
            _aspNetUser = aspNetUser;
            _rsaKeyService = rsaKeyService;
            _userService = userService;
        }

        public string GerarIdRefreshToken()
        {
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                var teste = Convert.ToBase64String(randomNumber);
                
                return teste;
            }            
        }


        public async Task<TokenDto> GerarToken(string cpf)
        {
            var user = await _userService.ObterPorCpf(cpf);
            var claims = await _userManager.GetClaimsAsync(user);

            // Gerar accesstoken
            var idAccess = Guid.NewGuid().ToString();
            var identityAccessToken= await ObterClaims(user, true, idAccess);
            var accessToken = CriarToken(identityAccessToken);

            //Gerar refeshToken
            var idRefresh = GerarIdRefreshToken();
            var identityRefreshToken = await ObterClaims(user, false,idRefresh);
            var refreshToken = CriarToken(identityRefreshToken);

            return ObterRespostaToken(accessToken, user, claims,refreshToken);
        }

             

        private string CriarToken(IEnumerable<Claim> claims)
        {
            var identityClaims = new ClaimsIdentity();
            identityClaims.AddClaims(claims);
            var tokenHandler = new JwtSecurityTokenHandler();
            var currentIssuer = $"{_aspNetUser.ObterContextoHttp().Request.Scheme}://{_aspNetUser.ObterContextoHttp().Request.Host}";
            var key = _rsaKeyService.GetSigningCredentials();
            var token = tokenHandler.CreateToken(new SecurityTokenDescriptor
            {
                Issuer = currentIssuer,
                Subject = identityClaims,
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = key
            });
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private async Task<IList<Claim>> ObterClaims(User user , bool adicionarClaimsUsuario, string id)
        {
            var claims = new List<Claim>();

            claims.Add(new Claim(JwtRegisteredClaimNames.Sub, user.Id));
            claims.Add(new Claim(JwtRegisteredClaimNames.Email, user.Email));
            claims.Add(new Claim(JwtRegisteredClaimNames.Jti, id));
            claims.Add(new Claim(JwtRegisteredClaimNames.Nbf, DateTime.Now.ToString()));
            claims.Add(new Claim(JwtRegisteredClaimNames.Iat, ToUnixEpochDate(DateTime.UtcNow).ToString(), ClaimValueTypes.Integer64));

            if (adicionarClaimsUsuario)
            {
                var userClaims = await _userManager.GetClaimsAsync(user);
                var roles = await _userManager.GetRolesAsync(user);

                claims.AddRange(userClaims);

                foreach (var role in roles)
                    claims.Add(new Claim("role", role));
            }

            return claims;
        }

        //retorna o objeto com o token
        private TokenDto ObterRespostaToken(string accessToken, User user, IEnumerable<Claim> claims, string refreshToken)
        {
            var dto = new TokenDto
            {
                AccessToken = accessToken,
                CriadoEm = DateTime.Now,
                RefreshToken = refreshToken,
                ExpiresIn = TimeSpan.FromHours(1).TotalSeconds,
                user = new UserDto()
                {
                    Id = user.Id,
                    Email = user.Email,
                    Ativo = user.Ativo,
                    Cpf = user.Cpf,
                    NomeCompleto = user.NomeCompleto
                }
            };
            var tokenModel = new RefreshToken()
            {
                Token = refreshToken,
                ExpiraEm = DateTime.Now.AddHours(3),
                CriadoEm = DateTime.Now,
                Revogado = false,
                UserId = user.Id                
            };

            var model = _tokenRepository.InserirAsync(tokenModel).Result;



            return dto;
        }

        private static long ToUnixEpochDate(DateTime date)
            => (long)Math.Round((date.ToUniversalTime() - new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero)).TotalSeconds);

        
    }
}
