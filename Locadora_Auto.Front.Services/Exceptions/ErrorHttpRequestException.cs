using System.Net;

namespace Locadora_Auto.Front.Services.Exceptions
{
    public class ErrorHttpRequestException : Exception
    {
        public HttpStatusCode StatusCode;

        public ErrorHttpRequestException() { }

        public ErrorHttpRequestException(string message, Exception innerException)
            : base(message, innerException) { }

        public ErrorHttpRequestException(HttpStatusCode statusCode)
        {
            StatusCode = statusCode;
        }

        public ErrorHttpRequestException(string message, HttpStatusCode statusCode) : base(message)
        {
            StatusCode = statusCode;
        }
    }
}
