using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Domain.IRepositorio;

namespace Locadora_Auto.Infra.Data.Repositorio
{
    public class LogMensagemRepository : RepositorioGlobal<LogMensagem>, ILogMensagemRepository
    {
        public LogMensagemRepository(LocadoraDbContext dbContext) : base(dbContext) { }
    }
}
