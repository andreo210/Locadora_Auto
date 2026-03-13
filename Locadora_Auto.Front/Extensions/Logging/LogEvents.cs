namespace Locadora_Auto.Front.Extensions.Logging
{
    public static class LogEvents
    {
        // Eventos de Autenticação (1000-1999)
        public static readonly EventId LoginAttempt = new(1000, "LoginAttempt");
        public static readonly EventId LoginSuccess = new(1001, "LoginSuccess");
        public static readonly EventId LoginFailed = new(1002, "LoginFailed");
        public static readonly EventId Logout = new(1003, "Logout");

        // Eventos de CRUD (2000-2999)
        public static readonly EventId Create = new(2000, "Create");
        public static readonly EventId Read = new(2001, "Read");
        public static readonly EventId Update = new(2002, "Update");
        public static readonly EventId Delete = new(2003, "Delete");

        // Eventos de Erro (9000-9999)
        public static readonly EventId DatabaseError = new(9000, "DatabaseError");
        public static readonly EventId ApiError = new(9001, "ApiError");
        public static readonly EventId ValidationError = new(9002, "ValidationError");
        public static readonly EventId UnhandledException = new(9999, "UnhandledException");
    }
}
