using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Domain.IRepositorio;

namespace Locadora_Auto.Infra.Data.Repositorio
{
    public class TokenRepository : RepositorioGlobal<RefreshToken>, ITokenRepository
    {
        public TokenRepository(LocadoraDbContext dbContext) : base(dbContext) { }
    }
}
