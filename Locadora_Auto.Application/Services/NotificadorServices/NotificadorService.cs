using Locadora_Auto.Application.Models;

namespace Locadora_Auto.Application.Services.Notificador
{
    public class NotificadorService : INotificadorService
    {
        private List<Notificacao> _notificacoes;

        public NotificadorService()
        {
            _notificacoes = new List<Notificacao>();
        }

        public void Add(string notificacao)
        {
            _notificacoes.Add(new Notificacao(notificacao));
        }

        public List<Notificacao> ObterNotificacoes()
        {
            return _notificacoes;
        }

        public bool TemNotificacao()
        {
            return _notificacoes.Any();
        }
    }    
}
