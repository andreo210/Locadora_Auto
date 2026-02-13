using Locadora_Auto.Front.Models.Notificacao;

namespace Locadora_Auto.Front.Services.Servicos.Notificacao
{
    public interface INotificationService
    {
        void ShowSuccess(string message, string title = "Sucesso");
        void ShowError(string message, string title = "Erro");
        void ShowWarning(string message, string title = "Aviso");
        void ShowInfo(string message, string title = "Informação");
        void ShowValidationErrors(Dictionary<string, string[]> errors);
        event Action<NotificationEventArgs>? OnNotification;
    }
}
