using Locadora_Auto.Application.Models.Dto;

namespace Locadora_Auto.Application.Services.LocacaoServices
{
    public interface ILocacaoService
    {
        Task<LocacaoDto?> CriarAsync(CriarLocacaoDto dto, CancellationToken ct = default);
        Task<LocacaoDto?> AtualizarAsync(int id, AtualizarLocacaoDto dto, CancellationToken ct = default);
        Task<bool> FinalizarAsync(int id, DateTime dataFimReal, int kmFinal, decimal valorFinal, int filialDevolucao, CancellationToken ct = default);
        Task<bool> CancelarAsync(int id, CancellationToken ct = default);

        #region Pagamento
        Task<bool> AdicionarPagamentoAsync(int id,AdicionarPagamentoDto pagamento, CancellationToken ct = default);
        Task<bool> ConfirmarPagamentoAsync(int id, int idPagamento, CancellationToken ct = default);
        Task<bool> CancelarPagamentoAsync(int id, int idPagamento, string motivo, CancellationToken ct = default);
        Task<bool> MarcarComoFalhaAsync(int id, int idPagamento, CancellationToken ct = default);
        #endregion Pagamento

        #region Multa
        Task<bool> CompensarMultaAsync(int idLocacao, int idMulta,CancellationToken ct = default);
        Task<bool> AdicionarMultaAsync(int idLocacao, CriarMultaDto dto, CancellationToken ct = default);
        Task<bool> PagarMultaAsync(int idLocacao, int idMulta, CancellationToken ct = default);
        Task<bool> CancelarMultaAsync(int idLocacao, int idMulta, CancellationToken ct = default);
        #endregion Multa

        Task<LocacaoDto?> ObterPorIdAsync(int id, CancellationToken ct = default);
        Task<IEnumerable<LocacaoDto>> ObterTodasAsync(CancellationToken ct = default);

        #region Caucao
        Task<bool> AdicionarCalcaoAsync(int idLocacao, decimal valor, CancellationToken ct = default);
        Task<bool> DevolverCalcaoAsync(int idLocacao, int idCaucao, CancellationToken ct = default);
        Task<bool> BloquearCalcaoAsync(int idLocacao, int idCaucao, CancellationToken ct = default);
        Task<bool> DeduzirCalcaoAsync(int idLocacao, int idCaucao, decimal valor, CancellationToken ct = default);
        #endregion Caucao

        #region Seguro
        Task<bool> AdicionarSeguroAsync(int idLocacao, int idSeguro, CancellationToken ct = default);
        Task<bool> CancelarSeguroAsync(int idLocacao, int idLocacaoSeguro, CancellationToken ct = default);
        #endregion Seguro

        #region Vistoria
        Task<bool> RegistrarVistoriaAsync(int idLocacao, CriarVistoriaDto dto, CancellationToken ct = default);
        Task<bool> RegistrarFotoVistoriaAsync(int id, EnviarFotoVistoriaDto dto, CancellationToken ct = default);
        Task<bool> RegistrarDanoVistoriaAsync(int id, CriarDanoDto dto, CancellationToken ct = default);
        Task<bool> RemoverDanoVistoriaAsync(int id, RemoverDanoDto dto, CancellationToken ct = default);
        #endregion Vistoria
    }

}
