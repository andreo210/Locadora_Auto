using Locadora_Auto.Infra.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                ? _usersAsp.ObterIdKeycloak()
                : null;

        public bool IsAuthenticated =>
            _usersAsp.EstaAutenticado();
    }

}
