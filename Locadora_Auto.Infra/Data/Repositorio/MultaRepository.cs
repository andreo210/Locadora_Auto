using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Domain.IRepositorio;

namespace Locadora_Auto.Infra.Data.Repositorio
{
    public class MultaRepository : RepositorioGlobal<Multa>, IMultaRepository
    {
        public MultaRepository(LocadoraDbContext dbContext) : base(dbContext) { }       
    
    }
}
