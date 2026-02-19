using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Domain.IRepositorio;

namespace Locadora_Auto.Infra.Data.Repositorio
{
    public class AdicionalRepository : RepositorioGlobal<Adicional>, IAdicionalRepository
    {
        public AdicionalRepository(LocadoraDbContext dbContext) : base(dbContext) { }        
    }
}
