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
                ValorDiaria = entidade.ValorDiaria
            };
        }
    }
}
