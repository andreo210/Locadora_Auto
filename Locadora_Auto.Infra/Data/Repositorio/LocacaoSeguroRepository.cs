using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Domain.IRepositorio;

namespace Locadora_Auto.Infra.Data.Repositorio
{
    public class LocacaoSeguroRepository : RepositorioGlobal<LocacaoSeguro>, ILocacaoSeguroRepository
    {
        public LocacaoSeguroRepository(LocadoraDbContext dbContext) : base(dbContext) { }       
    
    }
}
