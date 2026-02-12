 using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Domain.Entidades;

namespace Locadora_Auto.Application.Models.Mappers
{
    public static class AdicionalMapper
    {
        public static AdicionalDto ToDto(this Adicional entidade)
        {
            return new AdicionalDto
            {
                IdAdicional = entidade.IdAdicional,
                Nome = entidade.Nome,
                ValorDiaria = entidade.ValorDiaria,
                Ativo = entidade.Ativo
            };
        }

        public static List<AdicionalDto> ToDtoList(this IEnumerable<Adicional> entidades)
        {
            if (entidades == null) return new List<AdicionalDto>();
            return entidades.Select(ToDto).ToList();
        }
    }
}
