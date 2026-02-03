using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Domain.IRepositorio;

namespace Locadora_Auto.Infra.Data.Repositorio
{
    public class CategoriaVeiculosRepository : RepositorioGlobal<CategoriaVeiculo>, ICategoriaVeiculosRepository
    {
        public CategoriaVeiculosRepository(LocadoraDbContext dbContext) : base(dbContext) { }
        
    }
}
