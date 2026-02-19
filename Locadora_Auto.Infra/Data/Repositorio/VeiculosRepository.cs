using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Domain.IRepositorio;

namespace Locadora_Auto.Infra.Data.Repositorio
{
    public class VeiculosRepository : RepositorioGlobal<Veiculo>, IVeiculosRepository
    {
        public VeiculosRepository(LocadoraDbContext dbContext) : base(dbContext) { }        
    }
}
