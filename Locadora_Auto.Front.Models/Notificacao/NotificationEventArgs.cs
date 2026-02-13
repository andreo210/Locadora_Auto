using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Locadora_Auto.Front.Models.Notificacao
{
    public class NotificationEventArgs : EventArgs
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public NotificationType Type { get; set; }
        public Dictionary<string, string[]>? ValidationErrors { get; set; }
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
