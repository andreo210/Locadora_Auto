using Locadora_Auto.Application.Models.Dto;

namespace Locadora_Auto.Application.Services.VeiculoServices
{
    public interface IIndicadoresFrotaService
    {
        /// <summary>
        /// Apura os indicadores de frota da seção 12 sobre a trilha do ativo (RN-37).
        /// </summary>
        /// <param name="de">Início da janela. Ausente, vale <paramref name="ate"/> menos 30 dias.</param>
        /// <param name="ate">Fim da janela. Ausente, vale agora; futuro é truncado em agora.</param>
        /// <param name="idFilial">Filial <b>atual</b> do veículo — ver a ressalva na implementação.</param>
        /// <param name="idCategoria">Categoria do veículo.</param>
        Task<IndicadoresFrotaDto?> ObterAsync(
            DateTime? de = null,
            DateTime? ate = null,
            int? idFilial = null,
            int? idCategoria = null,
            CancellationToken ct = default);
    }
}
