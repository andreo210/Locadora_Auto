using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Domain.IRepositorio;

namespace Locadora_Auto.Infra.Data.Repositorio
{
    public class FuncionarioRepository : RepositorioGlobal<Funcionario>, IFuncionarioRepository
    {
        public FuncionarioRepository(LocadoraDbContext dbContext) : base(dbContext) { }
    }
}
