using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Domain.IRepositorio;

namespace Locadora_Auto.Infra.Data.Repositorio
{
    public class FotoRepository : RepositorioGlobal<Foto>, IFotoRepository
    {
        public FotoRepository(LocadoraDbContext dbContext) : base(dbContext) { }
    }
}
