using System.Net;

namespace Locadora_Auto.Front.Services.Exceptions
{
    public class ValidationErrorException : CustomHttpRequestException
    {
        public Dictionary<string, string[]>? Errors { get; }

        public ValidationErrorException(
            string message,
            HttpStatusCode statusCode,
            Dictionary<string, string[]>? errors = null)
            : base(message, statusCode)
        {
            Errors = errors;
        }

        // Método para obter mensagens de erro formatadas
        public string GetFormattedErrors()
        {
            if (Errors == null || !Errors.Any())
                return Message;

            return string.Join("\n", Errors.SelectMany(e => e.Value));
        }
    }

}
