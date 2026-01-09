using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Domain.IRepositorio;
using Microsoft.EntityFrameworkCore;

namespace Locadora_Auto.Infra.Data.Repositorio
{
    public class ClienteRepository : RepositorioGlobal<Clientes>, IClienteRepository
    {
        public ClienteRepository(LocadoraDbContext dbContext) : base(dbContext) { }

        public async Task<Clientes?> ObterComLocacoesAsync(int id, CancellationToken ct = default)
        {
            return await ObterPrimeiroAsync(
                c => c.IdCliente == id,
                incluir: q => q.Include(c => c.Locacoes),
                ct: ct);
        }

        //public async Task<IReadOnlyList<Cliente>> ObterComMaiorFaturamentoAsync(
        //    DateTime dataInicio,
        //    DateTime dataFim,
        //    int limite = 10,
        //    CancellationToken ct = default)
        //{
        //    // Implementação usando consulta SQL ou LINQ complexa
        //    // Este é um exemplo simplificado
        //    var query = Context.Set<Cliente>()
        //        .Where(c => c.Locacoes.Any(l =>
        //            l.DataLocacao >= dataInicio &&
        //            l.DataLocacao <= dataFim))
        //        .Select(c => new
        //        {
        //            Cliente = c,
        //            Faturamento = c.Locacoes
        //                .Where(l => l.DataLocacao >= dataInicio && l.DataLocacao <= dataFim)
        //                .Sum(l => l.ValorTotal)
        //        })
        //        .OrderByDescending(x => x.Faturamento)
        //        .Take(limite)
        //        .Select(x => x.Cliente);

        //    return await query.ToListAsync(ct);
        //}
    
    }
}
