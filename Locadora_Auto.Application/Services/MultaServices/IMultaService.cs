using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Domain.Entidades;

namespace Locadora_Auto.Application.Services.MultaServices
{
    public interface IMultaService
    {
        Task<IEnumerable<MultaDto>> ObterMultasPorLocacaoAsync(int idLocacao, CancellationToken ct = default);
        Task<IEnumerable<MultaDto>> ObterMultasStatusAsync(int status = 0, CancellationToken ct = default);
        Task<IEnumerable<MultaDto>> ObterMultasPorTipoAsync(int tipo, CancellationToken ct = default);
    }
}
