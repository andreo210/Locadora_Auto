using {{RootNamespace}}.Front.Models.Notificacao;

namespace {{RootNamespace}}.Front.Services.Utils.Notificacao
{
    public interface INotificationService
    {
        void ShowSuccess(string message, string title = "Sucesso");
        void ShowError(string message, string title = "Erro");
        void ShowWarning(string message, string title = "Aviso");
        void ShowInfo(string message, string title = "Informação");

        /// <summary>Erros vindos do <c>ProblemDetails</c> da Api: campo → mensagens.</summary>
        void ShowValidationErrors(Dictionary<string, string[]> errors);

        event Action<NotificationEventArgs>? OnNotification;
    }

    /// <summary>
    /// Guarda apenas o evento; quem desenha é o <c>NotificationDisplay</c>, montado
    /// uma vez no layout. Registrado como Scoped — em Blazor Server isso é o circuito,
    /// então o serviço acompanha a aba do usuário do login ao fechamento.
    /// </summary>
    public class NotificationService : INotificationService
    {
        public event Action<NotificationEventArgs>? OnNotification;

        public void ShowSuccess(string message, string title = "Sucesso") =>
            Notificar(title, message, NotificationType.Success);

        public void ShowError(string message, string title = "Erro") =>
            Notificar(title, message, NotificationType.Error);

        public void ShowWarning(string message, string title = "Aviso") =>
            Notificar(title, message, NotificationType.Warning);

        public void ShowInfo(string message, string title = "Informação") =>
            Notificar(title, message, NotificationType.Info);

        public void ShowValidationErrors(Dictionary<string, string[]> errors)
        {
            OnNotification?.Invoke(new NotificationEventArgs
            {
                Title = "Erros de Validação",
                Type = NotificationType.Validation,
                ValidationErrors = errors,
                Duration = 8000   // vários campos de uma vez: precisa de mais tempo de leitura
            });
        }

        private void Notificar(string titulo, string mensagem, NotificationType tipo)
        {
            OnNotification?.Invoke(new NotificationEventArgs
            {
                Title = titulo,
                Message = mensagem,
                Type = tipo
            });
        }
    }
}
