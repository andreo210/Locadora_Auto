using Locadora_Auto.Front.Models.Notificacao;

namespace Locadora_Auto.Front.Services.Servicos.Notificacao
{
    public class NotificationService : INotificationService
    {
        public event Action<NotificationEventArgs>? OnNotification;

        public void ShowSuccess(string message, string title = "Sucesso")
        {
            OnNotification?.Invoke(new NotificationEventArgs
            {
                Title = title,
                Message = message,
                Type = NotificationType.Success
            });
        }

        public void ShowError(string message, string title = "Erro")
        {
            OnNotification?.Invoke(new NotificationEventArgs
            {
                Title = title,
                Message = message,
                Type = NotificationType.Error
            });
        }

        public void ShowWarning(string message, string title = "Aviso")
        {
            OnNotification?.Invoke(new NotificationEventArgs
            {
                Title = title,
                Message = message,
                Type = NotificationType.Warning
            });
        }

        public void ShowInfo(string message, string title = "Informação")
        {
            OnNotification?.Invoke(new NotificationEventArgs
            {
                Title = title,
                Message = message,
                Type = NotificationType.Info
            });
        }

        public void ShowValidationErrors(Dictionary<string, string[]> errors)
        {
            OnNotification?.Invoke(new NotificationEventArgs
            {
                Title = "Erros de Validação",
                Type = NotificationType.Validation,
                ValidationErrors = errors,
                Duration = 8000 // Mais tempo para validar vários campos
            });
        }
    }
}
