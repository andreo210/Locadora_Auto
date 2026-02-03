using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Locadora_Auto.Infra.Users
{
    public class UsersAsp : IUsersAsp
    {
        private readonly IHttpContextAccessor _accessor;

        public UsersAsp(IHttpContextAccessor accessor)
        {
            _accessor = accessor;
        }

        public string? Nome => _accessor.HttpContext.User.Identity.Name;

        public string ObterIdUsuario()
        {
            return EstaAutenticado() ? _accessor.HttpContext.User.ObterIdKeycloakUsuario() : "";
        }

        public string ObterEmail()
        {
            return EstaAutenticado() ? _accessor.HttpContext.User.ObterEmailUsuario() : "";
        }

        public string ObterCpf()
        {
            return EstaAutenticado() ? _accessor.HttpContext.User.ObterCpfUsuario() : "";
        }

        public string ObterNome()
        {
            return EstaAutenticado() ? _accessor.HttpContext.User.ObterNomeUsuario() : "";
        }

        public int ObterCodigoUsuario()
        {
            return EstaAutenticado() ? _accessor.HttpContext.User.ObterIdCadastroUnicoUsuario() : 0;
        }

        public string ObterToken()
        {
            return EstaAutenticado() ? _accessor.HttpContext.GetTokenAsync("access_token").Result : "";
        }

        public bool EstaAutenticado()
        {
            return _accessor.HttpContext.User.Identity.IsAuthenticated;
        }

        public bool PossuiPapel(string papel)
        {
            return _accessor.HttpContext.User.IsInRole(papel);
        }

        public IEnumerable<Claim> ObterClaims()
        {
            return _accessor.HttpContext.User.Claims;
        }

        public HttpContext ObterContextoHttp()
        {
            return _accessor.HttpContext;
        }
    }

}
