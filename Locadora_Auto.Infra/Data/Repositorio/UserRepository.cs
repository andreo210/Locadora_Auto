using Locadora_Auto.Domain.Entidades.Indentity;
using Locadora_Auto.Domain.IRepositorio;

namespace Locadora_Auto.Infra.Data.Repositorio
{
    public class UserRepository : RepositorioGlobal<User>, IUserRepository
    {
        public UserRepository(LocadoraDbContext dbContext) : base(dbContext) { }
    }
}
