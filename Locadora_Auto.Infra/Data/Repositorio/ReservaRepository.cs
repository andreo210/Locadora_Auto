using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Domain.IRepositorio;

namespace Locadora_Auto.Infra.Data.Repositorio
{
    public class ReservaRepository : RepositorioGlobal<Reserva>, IReservaRepository
    {
        public ReservaRepository(LocadoraDbContext dbContext) : base(dbContext) { }
        
    }
}
