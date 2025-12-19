namespace Locadora_Auto.Infra.ServiceHttp.Servicos.Notificacao
{
    public interface INotificacaoService
    {
        Task EnviarNotificacao(string cpf, string protocolo, string mensagem, string status);
    }
}