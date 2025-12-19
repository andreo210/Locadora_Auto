using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Domain.Entidades;

namespace Locadora_Auto.Application.Models.Mappers
{
    public static class SeguroMapper
    {
        public static SeguroDto ToDto(this Seguro entidade)
        {
            return new SeguroDto
            {
                IdSeguro = entidade.IdSeguro,
                Nome = entidade.Nome,
                ValorDiaria = entidade.ValorDiaria,
                //CobreDanos = entidade.CobreDanos
            };
        }
    }
}
