using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Domain.IRepositorio;

namespace Locadora_Auto.Infra.Data.Repositorio
{
    public class MovimentoVeiculoRepository : RepositorioGlobal<MovimentoVeiculo>, IMovimentoVeiculoRepository
    {
        public MovimentoVeiculoRepository(LocadoraDbContext dbContext) : base(dbContext) { }
    }
}
