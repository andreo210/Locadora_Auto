using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Locadora_Auto.Infra.Users
{
    public interface IUsersAsp
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

}
