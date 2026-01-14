namespace Locadora_Auto.Infra.Data.CurrentUsers
{
    public interface ICurrentUser
    {
        string? UserId { get; }
        bool IsAuthenticated { get; }
    }
}
