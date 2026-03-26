using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Domain;
using Microsoft.AspNetCore.Http;

namespace Locadora_Auto.Application.Services.CategoriaVeiculosServices
{
    public interface ICategoriaVeiculoService
    {
        Task<PaginatedResult<CategoriaVeiculoDto>> ObterTodosPaginadoAsync(int pagina, int itemPorPagina, CancellationToken ct = default);
        Task<CategoriaVeiculoDto?> ObterPorIdAsync(int id, CancellationToken ct = default);
        Task<CategoriaVeiculoDto> CriarAsync(CriarCategoriaVeiculoDto dto, CancellationToken ct = default);
        Task<bool> AtualizarAsync(int id, AtualizarCategoriaVeiculoDto dto, CancellationToken ct = default);
        Task<bool> ExcluirAsync(int id, CancellationToken ct = default);
        Task<bool> RegistarFotoCategoriaAsync(int id, List<IFormFile> fotos, CancellationToken ct = default);
    }


}
