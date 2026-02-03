using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Domain.IRepositorio;

namespace Locadora_Auto.Infra.Data.Repositorio
{
    public class SeguroRepository : RepositorioGlobal<Seguro>, ISeguroRepository
    {
        public SeguroRepository(LocadoraDbContext dbContext) : base(dbContext) { }      
    }
}
