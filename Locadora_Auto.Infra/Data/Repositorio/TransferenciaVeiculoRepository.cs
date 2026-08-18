using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Domain.IRepositorio;

namespace Locadora_Auto.Infra.Data.Repositorio
{
    public class TransferenciaVeiculoRepository : RepositorioGlobal<TransferenciaVeiculo>, ITransferenciaVeiculoRepository
    {
        public TransferenciaVeiculoRepository(LocadoraDbContext dbContext) : base(dbContext) { }
    }
}
