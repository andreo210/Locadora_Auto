using Locadora_Auto.Application.Models;

namespace Locadora_Auto.Application.Services.Notificador
{
    public interface INotificador
    {
        bool TemNotificacao();
        List<Notificacao> ObterNotificacoes();
        void Add(Notificacao notificacao);
    }
}
