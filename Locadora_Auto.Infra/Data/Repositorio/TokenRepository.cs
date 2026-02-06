using Locadora_Auto.Domain.Entidades.Indentity;
using Locadora_Auto.Domain.IRepositorio;

namespace Locadora_Auto.Infra.Data.Repositorio
{
    public class TokenRepository : RepositorioGlobal<RefreshToken>, ITokenRepository
    {
        public TokenRepository(LocadoraDbContext dbContext) : base(dbContext) { }
    }
}
