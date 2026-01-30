using Locadora_Auto.Application.Models.Dto;

namespace Locadora_Auto.Application.Services.LocacaoServices
{
    public interface ILocacaoService
    {
        Task<LocacaoDto?> CriarAsync(CriarLocacaoDto dto, CancellationToken ct = default);
        Task<LocacaoDto?> AtualizarAsync(int id, AtualizarLocacaoDto dto, CancellationToken ct = default);
        Task<bool> FinalizarAsync(int id, DateTime dataFimReal, int kmFinal, decimal valorFinal, int filialDevolucao, CancellationToken ct = default);
        Task<bool> CancelarAsync(int id, CancellationToken ct = default);
        Task<bool> AdicionarPagamentoAsync(int id,AdicionarPagamentoDto pagamento, CancellationToken ct = default);
        Task<bool> CompensarMultaAsync(int idLocacao, int idMulta,CancellationToken ct = default);
        Task<bool> AdicionarMultaAsync(int idLocacao, CriarMultaDto dto, CancellationToken ct = default);
        Task<bool> AdicionarSeguroAsync(int idLocacao, LocacaoSeguroDto dto, CancellationToken ct = default);
        Task<LocacaoDto?> ObterPorIdAsync(int id, CancellationToken ct = default);
        Task<IEnumerable<LocacaoDto>> ObterTodasAsync(CancellationToken ct = default);

        Task<bool> AdicionarCalcaoAsync(int idLocacao, decimal valor, CancellationToken ct = default);
    }

}
