using Locadora_Auto.Application.Models.Dto;

namespace Locadora_Auto.Application.Services.CategoriaVeiculosServices
{
    public interface ICategoriaVeiculoService
    {
        Task<IReadOnlyList<CategoriaVeiculoDto>> ObterTodosAsync(CancellationToken ct = default);
        Task<CategoriaVeiculoDto?> ObterPorIdAsync(int id, CancellationToken ct = default);
        Task<bool> CriarAsync(CriarCategoriaVeiculoDto dto, CancellationToken ct = default);
        Task<bool> AtualizarAsync(int id, AtualizarCategoriaVeiculoDto dto, CancellationToken ct = default);
        Task<bool> ExcluirAsync(int id, CancellationToken ct = default);
    }


}
