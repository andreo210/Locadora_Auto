using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Domain.IRepositorio;

namespace Locadora_Auto.Infra.Data.Repositorio
{
    public class FilialRepository : RepositorioGlobal<Filial>, IFilialRepository
    {
        public FilialRepository(LocadoraDbContext dbContext) : base(dbContext) { }
    }
}
