using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Domain.IRepositorio;

namespace Locadora_Auto.Infra.Data.Repositorio
{
    public class BloqueioVeiculoRepository : RepositorioGlobal<BloqueioVeiculo>, IBloqueioVeiculoRepository
    {
        public BloqueioVeiculoRepository(LocadoraDbContext dbContext) : base(dbContext) { }
    }
}
