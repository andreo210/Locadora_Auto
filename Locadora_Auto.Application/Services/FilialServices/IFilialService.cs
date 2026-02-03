using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Application.Models.Dto.Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Domain.Entidades;
using System.Linq.Expressions;

namespace Locadora_Auto.Application.Services.FilialServices
{
    public interface IFilialService
    {
        //#region Operações de Consulta
        Task<FilialDto?> ObterPorIdAsync(int id, CancellationToken ct = default);
        //Task<FilialDto?> ObterPorIdComVeiculosAsync(int id, CancellationToken ct = default);
        Task<IReadOnlyList<FilialDto>> ObterTodasAsync(CancellationToken ct = default);
        //Task<IReadOnlyList<FilialDto>> ObterAtivasAsync(CancellationToken ct = default);
        //Task<IReadOnlyList<FilialDto>> ObterPorCidadeAsync(string cidade, CancellationToken ct = default);
        //Task<IReadOnlyList<FilialDto>> ObterPorEstadoAsync(string estado, CancellationToken ct = default);
        //Task<IReadOnlyList<FilialResumoDto>> ObterResumoAsync(CancellationToken ct = default);

        //Task<bool> ExisteFilialAsync(int id, CancellationToken ct = default);
        //Task<int> ContarAtivasAsync(CancellationToken ct = default);
        //Task<int> ContarVeiculosNaFilialAsync(int filialId, CancellationToken ct = default);
        //Task<int> ContarVeiculosDisponiveisNaFilialAsync(int filialId, CancellationToken ct = default);

        //// Método genérico para consultas complexas
        //Task<IReadOnlyList<FilialDto>> ObterComFiltroAsync(
        //    Expression<Func<Filial, bool>>? filtro = null,
        //    Func<IQueryable<Filial>, IOrderedQueryable<Filial>>? ordenarPor = null,
        //    CancellationToken ct = default);
        //#endregion

        //#region Operações de CRUD
        Task<FilialDto> CriarFilialAsync(CriarFilialDto filialDto, CancellationToken ct = default);
        Task<bool> AtualizarFilialAsync(int id, AtualizarFilialDto filialDto, CancellationToken ct = default);
        Task<bool> ExcluirFilialAsync(int id, CancellationToken ct = default);
        Task<bool> AtivarFilialAsync(int id, CancellationToken ct = default);
        Task<bool> DesativarFilialAsync(int id, CancellationToken ct = default);
        //#endregion

        //#region Operações Específicas
        //Task<bool> TransferirVeiculoAsync(int veiculoId, int filialOrigemId, int filialDestinoId, CancellationToken ct = default);
        //Task<IReadOnlyList<VeiculoDto>> ObterVeiculosDaFilialAsync(int filialId, CancellationToken ct = default);
        //Task<EstatisticasFilialDto> ObterEstatisticasFilialAsync(int filialId, CancellationToken ct = default);

        //Task<bool> ValidarFilialParaLocacaoAsync(int filialId, CancellationToken ct = default);
        //Task<bool> FilialPossuiVeiculosAsync(int filialId, CancellationToken ct = default);
        //Task<bool> FilialPossuiLocacoesAtivasAsync(int filialId, CancellationToken ct = default);
        //#endregion

        //#region Validações
        Task<bool> ValidarCriacaoFilialAsync(CriarFilialDto filialDto, CancellationToken ct = default);
        Task<bool> ValidarAtualizacaoFilialAsync(int id, AtualizarFilialDto filialDto, CancellationToken ct = default);
        //#endregion
    }

    public class EstatisticasFilialDto
    {
        public int TotalVeiculos { get; set; }
        public int VeiculosDisponiveis { get; set; }
        public int VeiculosEmManutencao { get; set; }
        public int VeiculosAlugados { get; set; }
        public int TotalLocacoesMes { get; set; }
        public decimal FaturamentoMes { get; set; }
        public int TotalFuncionarios { get; set; }
        public decimal TaxaOcupacao { get; set; } // Percentual
        public decimal MediaAvaliacao { get; set; }
    }
}