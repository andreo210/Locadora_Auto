using Locadora_Auto.Application.Models.Dto;

namespace Locadora_Auto.Application.Services.LocacaoServices
{
    public interface ILocacaoService
    {
        Task<LocacaoDto?> CriarAsync(CriarLocacaoDto dto, CancellationToken ct = default);
        Task<LocacaoDto?> AtualizarAsync(int id, AtualizarLocacaoDto dto, CancellationToken ct = default);
        Task<bool> CancelarAsync(int id, CancellationToken ct = default);

        #region Devolução e fechamento

        // Doc 07 §1: DEVOLUÇÃO → FECHAMENTO → QUITAÇÃO são atos distintos, e agora são portas
        // distintas. Até aqui a Api tinha uma só, `FinalizarAsync`, que recebia o `valorFinal`
        // digitado por quem chamava — era o buraco funcional do sistema.

        /// <summary>
        /// Encerra a posse (RN-58). Não fecha o contrato nem apura nada, e <b>não recebe o
        /// hodômetro</b>: ele sai da vistoria de devolução (RN-11).
        /// </summary>
        Task<bool> RegistrarDevolucaoAsync(int id, RegistrarDevolucaoDto dto, CancellationToken ct = default);

        /// <summary>
        /// Apura a conta, sela o contrato e resolve a caução. <b>Idempotente</b> (RN-32): chamar de
        /// novo devolve a mesma apuração, sem cobrar nada outra vez.
        /// </summary>
        Task<ResultadoDaApuracaoDto?> ApurarFechamentoAsync(int id, ApurarFechamentoDto dto, CancellationToken ct = default);

        /// <summary>O extrato discriminado (RN-31) — a conta que o cliente recebe.</summary>
        Task<FechamentoLocacaoDto?> ObterFechamentoAsync(int id, CancellationToken ct = default);

        #endregion Devolução e fechamento

        /// <summary>
        /// RN-60: passa para <c>Atrasada</c> todo contrato <c>EmAndamento</c> que já passou do fim
        /// previsto. Devolve quantos mudaram. Chamada pelo agendador, em lote.
        /// </summary>
        Task<int> MarcarAtrasadasAsync(CancellationToken ct = default);

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

        #region Adicional
        Task<bool> InserirAdicionalAsync(int idLocacao, LocacaoAdicionalDto dto, CancellationToken ct = default);
        Task<bool> RemoverAdicionalAsync(int idLocacao, int idAdicional, CancellationToken ct = default);
        #endregion Adicional
    }

}
