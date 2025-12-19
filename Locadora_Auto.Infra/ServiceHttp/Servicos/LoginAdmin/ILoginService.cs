using Locadora_Auto.Infra.ServiceHttp.Models.Views.Keycloak;
using System.Net;

namespace Locadora_Auto.Infra.ServiceHttp.Servicos.LoginAdmin
{
    /// <summary>
    /// Interface responsável pelos serviços de autenticação de administradores no sistema.
    /// </summary>
    public interface ILoginService
    {
        /// <summary>
        /// Realiza a autenticação de um administrador interno e retorna os dados do token, status da requisição e mensagem.
        /// </summary>
        /// <returns>Uma tupla contendo os dados do token, o status HTTP e uma mensagem explicativa.</returns>
        (TokenServiceView dados, HttpStatusCode status, string mensagem) ObterAutenticacaoAdminInterno();

        /// <summary>
        /// Realiza a autenticação de um administrador externo e retorna os dados do token, status da requisição e mensagem.
        /// </summary>
        /// <returns>Uma tupla contendo os dados do token, o status HTTP e uma mensagem explicativa.</returns>
        (TokenServiceView dados, HttpStatusCode status, string mensagem) ObterAutenticacaoAdminExterno();
    }

}
