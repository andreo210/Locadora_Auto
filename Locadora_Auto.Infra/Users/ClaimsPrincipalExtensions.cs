using System.Security.Claims;

namespace Locadora_Auto.Infra.Users
{
    /// <summary>
    /// Extensões para facilitar o acesso a informações do usuário a partir de ClaimsPrincipal.
    /// </summary>
    public static class ClaimsPrincipalExtensions
    {
        /// <summary>
        /// Obtém o e-mail do usuário a partir do claim "email".
        /// </summary>
        public static string ObterEmailUsuario(this ClaimsPrincipal principal)
        {
            if (principal == null)
            {
                throw new ArgumentException(nameof(principal));
            }
            var claim = principal.FindFirst("email");
            return claim?.Value;
        }

        /// <summary>
        /// Obtém o CPF do usuário a partir do claim "preferred_username".
        /// </summary>
        public static string ObterCpfUsuario(this ClaimsPrincipal principal)
        {
            if (principal == null)
            {
                throw new ArgumentException(nameof(principal));
            }
            var cpf = principal?.FindFirstValue("preferred_username");
            return cpf;
        }

        /// <summary>
        /// Obtém o ID do Cadastro Único do usuário a partir do claim "IdCadastroUnico".
        /// </summary>
        public static int ObterIdCadastroUnicoUsuario(this ClaimsPrincipal principal)
        {
            if (principal == null)
            {
                throw new ArgumentException(nameof(principal));
            }
            var id = principal?.FindFirstValue("IdCadastroUnico");
            return int.Parse(id);
        }

        /// <summary>
        /// Obtém o identificador do usuário no Keycloak a partir do claim NameIdentifier.
        /// </summary>
        public static string ObterIdKeycloakUsuario(this ClaimsPrincipal principal)
        {
            if (principal == null)
            {
                throw new ArgumentException(nameof(principal));
            }
            var nameIdentifier = principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            return nameIdentifier;
        }

        /// <summary>
        /// Obtém o token de acesso do usuário a partir do claim "access_token".
        /// </summary>
        public static string ObterTokenUsuario(this ClaimsPrincipal principal)
        {
            if (principal == null)
            {
                throw new ArgumentException(nameof(principal));
            }
            var token = principal?.FindFirstValue("access_token");
            return token;
        }

        /// <summary>
        /// Obtém o nome completo do usuário a partir do claim "name".
        /// </summary>
        public static string ObterNomeUsuario(this ClaimsPrincipal principal)
        {
            if (principal == null)
            {
                throw new ArgumentException(nameof(principal));
            }
            var nome = principal?.FindFirstValue("name");
            return nome;
        }
    }


}
