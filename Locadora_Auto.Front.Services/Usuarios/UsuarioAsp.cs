using Locadora_Auto.Front.Services.Extensions;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Locadora_Auto.Front.Services.Usuarios
{
    public interface IUsuarioAsp
    {
        /// <summary>
        /// Retorna o nome do usuário autenticado.
        /// </summary>
        string? Nome { get; }

        /// <summary>
        /// Retorna o ID do usuário no Keycloak.
        /// </summary>
        string ObterIdKeycloak();

        /// <summary>
        /// Retorna o e-mail do usuário.
        /// </summary>
        string ObterEmail();

        /// <summary>
        /// Retorna o CPF do usuário.
        /// </summary>
        string ObterCpf();

        /// <summary>
        /// Retorna o nome completo do usuário.
        /// </summary>
        string ObterNome();

        /// <summary>
        /// Retorna o código do Cadastro Único do usuário.
        /// </summary>
        int ObterCodigoCadastroUnico();

        /// <summary>
        /// Retorna o token de acesso do usuário.
        /// </summary>
        string ObterToken();

        /// <summary>
        /// Verifica se o usuário está autenticado.
        /// </summary>
        bool EstaAutenticado();

        /// <summary>
        /// Verifica se o usuário possui a role especificada.
        /// </summary>
        bool PossuiPapel(string papel);

        /// <summary>
        /// Retorna todos os claims do usuário.
        /// </summary>
        IEnumerable<Claim> ObterClaims();

        /// <summary>
        /// Retorna o contexto HTTP atual.
        /// </summary>
        HttpContext ObterContextoHttp();
    }


    public class UsuarioAsp : IUsuarioAsp
    {
        private readonly IHttpContextAccessor _accessor;

        public UsuarioAsp(IHttpContextAccessor accessor)
        {
            _accessor = accessor;
        }

        public string? Nome => _accessor.HttpContext.User.Identity.Name;

        public string ObterIdKeycloak()
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

        public int ObterCodigoCadastroUnico()
        {
            return EstaAutenticado() ? _accessor.HttpContext.User.ObterIdCadastroUnicoUsuario() : 0;
        }

        public string ObterToken()
        {
            return EstaAutenticado() ? _accessor.HttpContext.User.ObterTokenUsuario() : "";
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
