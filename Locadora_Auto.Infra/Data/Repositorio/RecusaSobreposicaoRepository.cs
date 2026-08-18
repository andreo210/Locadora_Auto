using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Domain.IRepositorio;

namespace Locadora_Auto.Infra.Data.Repositorio
{
    public class RecusaSobreposicaoRepository : RepositorioGlobal<RecusaSobreposicao>, IRecusaSobreposicaoRepository
    {
        public RecusaSobreposicaoRepository(LocadoraDbContext dbContext) : base(dbContext) { }
    }
}
