using {{RootNamespace}}.Application.Models;

namespace {{RootNamespace}}.Application.Configuration.Ultils.NotificadorServices
{
    /// <summary>
    /// Canal de erros de regra de negócio entre serviço e controller.
    ///
    /// Registre como SCOPED: é a instância compartilhada dentro da requisição que faz o
    /// controller enxergar o que o serviço reportou. Singleton vaza mensagens entre usuários;
    /// transient entrega ao controller uma instância vazia e a resposta sai 200 com corpo nulo.
    /// </summary>
    public interface INotificadorService
    {
        bool TemNotificacao();
        List<Notificacao> ObterNotificacoes();
        void Add(string notificacao);
    }

    public class NotificadorService : INotificadorService
    {
        private readonly List<Notificacao> _notificacoes = new();

        /// <summary>
        /// Acumula em vez de interromper: o serviço reporta todos os problemas de uma vez e o
        /// usuário corrige tudo numa tentativa só — o oposto do que a primeira exceção lançada
        /// permitiria.
        /// </summary>
        public void Add(string notificacao) => _notificacoes.Add(new Notificacao(notificacao));

        public List<Notificacao> ObterNotificacoes() => _notificacoes;

        public bool TemNotificacao() => _notificacoes.Any();
    }
}
