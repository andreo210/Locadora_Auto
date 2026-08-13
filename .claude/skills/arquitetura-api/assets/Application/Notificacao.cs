using System.Net;

namespace {{RootNamespace}}.Application.Models
{
    /// <summary>
    /// Falha de regra de negócio. O <see cref="Campo"/> vira a chave em 'errors' na resposta,
    /// o que permite a tela destacar o input errado; sem campo, as mensagens caem em "geral".
    /// </summary>
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
