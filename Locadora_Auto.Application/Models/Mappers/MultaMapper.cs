using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Domain.Entidades;

namespace Locadora_Auto.Application.Models.Mappers
{
    public static class MultaMapper
    {
        public static MultaDto ToDto(this Multa entidade)
        {
            return new MultaDto
            {
                IdMulta = entidade.IdMulta,
                Tipo = entidade.Tipo.ToString(),
                Valor = entidade.Valor,
                Status = entidade.Status.ToString()
            };
        }
        public static List<MultaDto> ToDtoList(this IEnumerable<Multa> entidades)
        {
            if (entidades == null) return new List<MultaDto>();
            return entidades.Select(ToDto).ToList();
        }
    }
}
