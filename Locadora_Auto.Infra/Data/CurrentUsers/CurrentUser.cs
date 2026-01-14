using Locadora_Auto.Infra.Users;

namespace Locadora_Auto.Infra.Data.CurrentUsers
{
        public class CurrentUser : ICurrentUser
    {
        private readonly IUsersAsp _usersAsp;

        public CurrentUser(IUsersAsp usersAsp)
        {
            _usersAsp = usersAsp;
        }

        public string? UserId =>
            _usersAsp.EstaAutenticado()
                ? _usersAsp.ObterIdUsuario()
                : null;

        public bool IsAuthenticated =>
            _usersAsp.EstaAutenticado();
    }

}
