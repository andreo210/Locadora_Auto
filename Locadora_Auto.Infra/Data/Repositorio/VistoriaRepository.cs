using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Domain.IRepositorio;

namespace Locadora_Auto.Infra.Data.Repositorio
{
    public class VistoriaRepository : RepositorioGlobal<Vistoria>, IVistoriaRepository
    {
        public VistoriaRepository(LocadoraDbContext dbContext) : base(dbContext) { }
        
    }
}
