using System.Net;

namespace Locadora_Auto.Infra.Exceptions
{
    /// <summary>
    /// Exceção personalizada para representar erros em requisições HTTP,
    /// permitindo o uso de um código de status HTTP associado.
    /// </summary>
    public class CustomHttpRequestException : Exception
    {
        /// <summary>
        /// Código de status HTTP associado à exceção.
        /// </summary>
        public HttpStatusCode StatusCode { get; }

        /// <summary>
        /// Inicializa uma nova instância da exceção sem parâmetros.
        /// </summary>
        public CustomHttpRequestException() { }

        /// <summary>
        /// Inicializa uma nova instância da exceção com uma mensagem.
        /// </summary>
        /// <param name="message">Mensagem de erro.</param>
        public CustomHttpRequestException(string message)
            : base(message) { }

        /// <summary>
        /// Inicializa uma nova instância da exceção com uma mensagem e uma exceção interna.
        /// </summary>
        /// <param name="message">Mensagem de erro.</param>
        /// <param name="innerException">Exceção que causou esta exceção.</param>
        public CustomHttpRequestException(string message, Exception innerException)
            : base(message, innerException) { }

        /// <summary>
        /// Inicializa uma nova instância da exceção com um código de status HTTP.
        /// </summary>
        /// <param name="statusCode">Código de status HTTP associado ao erro.</param>
        public CustomHttpRequestException(HttpStatusCode statusCode)
            : base($"Erro HTTP: {(int)statusCode} - {statusCode}")
        {
            StatusCode = statusCode;
        }

        /// <summary>
        /// Inicializa uma nova instância da exceção com uma mensagem e um código de status HTTP.
        /// </summary>
        /// <param name="message">Mensagem de erro.</param>
        /// <param name="statusCode">Código de status HTTP associado ao erro.</param>
        public CustomHttpRequestException(string message, HttpStatusCode statusCode)
            : base(message)
        {
            StatusCode = statusCode;
        }
    }

}
