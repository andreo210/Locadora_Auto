namespace {{RootNamespace}}.Front.Models.Notificacao
{
    public class NotificationEventArgs : EventArgs
    {
        public string Title { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public NotificationType Type { get; set; }

        /// <summary>Preenchido só em <see cref="NotificationType.Validation"/>: campo → mensagens.</summary>
        public Dictionary<string, string[]>? ValidationErrors { get; set; }

        /// <summary>Milissegundos até sumir sozinha. A de validação ignora isso e espera o clique.</summary>
        public int Duration { get; set; } = 5000;
    }

    public enum NotificationType
    {
        Success,
        Error,
        Warning,
        Info,
        Validation
    }
}
