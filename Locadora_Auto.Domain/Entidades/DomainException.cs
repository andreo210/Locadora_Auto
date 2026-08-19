
namespace Locadora_Auto.Domain.Entidades
{
    /// <summary>
    /// Recusa de regra de domínio.
    ///
    /// Era <c>internal</c>, e a consequência aparecia na borda: a Application não conseguia
    /// distingui-la de um defeito, então ou repetia cada guarda do domínio antes de chamá-lo, ou
    /// deixava a recusa escapar como <b>500</b>. Repetir funcionava enquanto o domínio tinha uma
    /// guarda por método; a apuração do fechamento tem dezenas, e duplicá-las seria garantir que um
    /// dia divergissem.
    ///
    /// Pública, o serviço a captura e transforma em notificação — que é o caminho do
    /// <c>CustomResponse</c> para 4xx. Mapeá-la também no <c>ExceptionProblemFactory</c>, para o
    /// caso de escapar, continua sendo o backlog `C5`.
    /// </summary>
    [Serializable]
    public class DomainException : Exception
    {
        public DomainException()
        {
        }

        public DomainException(string? message) : base(message)
        {
        }

        public DomainException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}