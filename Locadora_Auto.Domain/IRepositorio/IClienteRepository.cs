using Locadora_Auto.Domain.Entidades;

namespace Locadora_Auto.Domain.IRepositorio
{
    public interface IClienteRepository : IRepositorioGlobal<Clientes>
    {
        // Métodos específicos para Cliente podem ser adicionados aqui
        //Task<Cliente?> ObterComLocacoesAsync(int id, CancellationToken ct = default);
        //Task<IReadOnlyList<Cliente>> ObterComMaiorFaturamentoAsync(DateTime dataInicio,DateTime dataFim,int limite = 10, CancellationToken ct = default);
    }      
}
