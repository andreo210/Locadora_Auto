using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Domain.Entidades;

namespace Locadora_Auto.Application.Models.Mappers
{
    public static class VistoriaMapper
    {
        public static VistoriaDto ToDto(this Vistoria entidade)
        {
            return new VistoriaDto
            {
                IdVistoria = entidade.IdVistoria,
                Tipo = entidade.Tipo,
                Observacoes = entidade.Observacoes,
                DataVistoria = entidade.DataVistoria,
               // Danos = entidade.Danos?.Select(d => d.ToDto()).ToList()
            };
        }
    }
}
