using System.Net;

namespace Locadora_Auto.Front.Services.Exceptions
{
    public class CustomHttpRequestException : Exception
    {
        public HttpStatusCode StatusCode { get; }
        public object? ErrorDetails { get; }

        public CustomHttpRequestException(string message, HttpStatusCode statusCode, object? errorDetails = null)
            : base(message)
        {
            StatusCode = statusCode;
            ErrorDetails = errorDetails;
        }
    }
}
