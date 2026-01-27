using Locadora_Auto.Application.Models;

namespace Locadora_Auto.Application.Services.Notificador
{
    public interface INotificadorService
    {
        bool TemNotificacao();
        List<Notificacao> ObterNotificacoes();
        void Add(string notificacao);
    }
}
