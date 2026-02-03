using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Domain.IRepositorio;

namespace Locadora_Auto.Infra.Data.Repositorio
{
    public class LocacaoRepository : RepositorioGlobal<Locacao>, ILocacaoRepository
    {
        public LocacaoRepository(LocadoraDbContext dbContext) : base(dbContext) { }       
    
    }
}
