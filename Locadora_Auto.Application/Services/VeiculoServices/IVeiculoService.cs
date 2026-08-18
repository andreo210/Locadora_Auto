using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Domain;

namespace Locadora_Auto.Application.Services.VeiculoServices
{
    public interface IVeiculoService
    {
        Task<VeiculoDto?> ObterPorIdAsync(int id, CancellationToken ct = default);
        Task<IReadOnlyList<VeiculoDto>> ObterTodosAsync(CancellationToken ct = default);
        Task<PaginatedResult<VeiculoDto>> ObterTodosPaginadoAsync(
            ConsultaPaginadaRequest consulta,
            int? idCategoria = null,
            int? idFilial = null,
            int? idStatus = null,
            bool? ativo = null,
            CancellationToken ct = default);
        Task<IReadOnlyList<VeiculoDto>> ObterDisponiveisAsync(int? idFilial = null, CancellationToken ct = default);

        Task<VeiculoDto?> CriarAsync(CriarVeiculoDto dto, CancellationToken ct = default);
        Task<bool> AtualizarAsync(int id, AtualizarVeiculoDto dto, CancellationToken ct = default);

        Task<bool> ExcluirAsync(int id, CancellationToken ct = default);

        Task<bool> AtivarAsync(int id, CancellationToken ct = default);
        Task<bool> DesativarAsync(int id, CancellationToken ct = default);

        /// <summary>
        /// Devolve à oferta o veículo que estava na fila do pátio depois da devolução (RN-45).
        /// </summary>
        Task<bool> LiberarDaPreparacaoAsync(int id, CancellationToken ct = default);

        /// <summary>
        /// Varre o pátio e devolve à oferta quem passou do <c>TempoPreparacaoMinutos</c> da filial
        /// sem liberação explícita (RN-45, parte automática). Chamada pelo agendador, em lote.
        /// </summary>
        Task<LiberacaoPreparacaoDto> LiberarPreparacoesVencidasAsync(CancellationToken ct = default);

        #region Bloqueio

        /// <summary>
        /// RN-52: tira o veículo da oferta com motivo, prazo e responsável.
        /// </summary>
        Task<BloqueioVeiculoDto?> BloquearAsync(int id, BloquearVeiculoDto dto, CancellationToken ct = default);

        /// <summary>
        /// Encerra o bloqueio e devolve o veículo à situação em que ele estava antes dele.
        /// </summary>
        Task<bool> LiberarBloqueioAsync(int id, int idBloqueio, CancellationToken ct = default);

        /// <summary>
        /// Bloqueios do veículo, do mais recente para o mais antigo, abertos e encerrados.
        /// </summary>
        Task<IReadOnlyList<BloqueioVeiculoDto>> ObterBloqueiosAsync(int id, CancellationToken ct = default);

        #endregion Bloqueio

        #region Transferencia entre filiais

        /// <summary>
        /// RN-49: manda o veículo para outra filial. Ele sai da oferta da origem imediatamente e só
        /// entra na do destino quando a chegada for confirmada.
        /// </summary>
        Task<TransferenciaVeiculoDto?> EnviarParaTransferenciaAsync(
            int id, EnviarTransferenciaDto dto, CancellationToken ct = default);

        /// <summary>
        /// Confirma a chegada: a filial de destino vira a atual e o veículo volta à oferta — de lá.
        /// </summary>
        Task<bool> ConfirmarChegadaTransferenciaAsync(
            int id, int idTransferencia, ChegadaTransferenciaDto dto, CancellationToken ct = default);

        /// <summary>
        /// Aborta a viagem: o veículo volta à oferta da filial de origem.
        /// </summary>
        Task<bool> CancelarTransferenciaAsync(int id, int idTransferencia, CancellationToken ct = default);

        /// <summary>Transferências do veículo, da mais recente para a mais antiga.</summary>
        Task<IReadOnlyList<TransferenciaVeiculoDto>> ObterTransferenciasAsync(int id, CancellationToken ct = default);

        #endregion Transferencia entre filiais

        #region Desmobilizacao

        /// <summary>
        /// RN-56: o ativo deixa a frota, em definitivo. Recusado com contrato aberto ou já vendido
        /// para o futuro.
        /// </summary>
        Task<bool> DesmobilizarAsync(int id, DesmobilizarVeiculoDto dto, CancellationToken ct = default);

        #endregion Desmobilizacao

        #region Trilha do ativo

        /// <summary>
        /// Por onde o veículo passou (RN-37), da transição mais recente para a mais antiga.
        /// </summary>
        /// <param name="de">Início da janela, inclusivo. Compara instante, não dia.</param>
        /// <param name="ate">Fim da janela, inclusivo. Compara instante, não dia.</param>
        /// <param name="idTipoOrigem">Filtra por <c>TipoDocumentoOrigem</c>; valor fora do enum não devolve nada.</param>
        Task<PaginatedResult<MovimentoVeiculoDto>> ObterMovimentosAsync(
            int id,
            ConsultaPaginadaRequest consulta,
            DateTime? de = null,
            DateTime? ate = null,
            int? idTipoOrigem = null,
            CancellationToken ct = default);

        #endregion Trilha do ativo

        #region Manutencao
        Task<IReadOnlyList<ManutencaoDto>> ObterManutencoesAsync(int id, CancellationToken ct = default);
        Task<bool> IniciarManutencao(int id, CriarManutencaoDto dto, CancellationToken ct = default);
        Task<bool> TerminaManutencao(int id, TerminarManutencaoDto dto, CancellationToken ct = default);
        Task<bool> CancelarManutencao(int id, int idManutencao, CancellationToken ct = default);
        Task<bool> AtualizarDescricaoManutencao(int id, AtualizarManutencaoDto dto, CancellationToken ct = default);
        #endregion Manutencao
    }

}
