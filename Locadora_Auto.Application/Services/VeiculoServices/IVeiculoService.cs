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
