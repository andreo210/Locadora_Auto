using System.Net;

namespace Locadora_Auto.Application.Models
{
    public class Notificacao
    {
        public string Mensagem { get; }
        public string? Campo { get; }
        public HttpStatusCode Status { get; }

        public Notificacao(
            string mensagem,
            HttpStatusCode status = HttpStatusCode.BadRequest,
            string? campo = null)
        {
            Mensagem = mensagem;
            Status = status;
            Campo = campo;
        }
    }
}
