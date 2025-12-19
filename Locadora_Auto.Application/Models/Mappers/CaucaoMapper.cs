using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Domain.Entidades;

namespace Locadora_Auto.Application.Models.Mappers
{
    public static class CaucaoMapper
    {
        public static CaucaoDto ToDto(this Caucao entidade)
        {
            return new CaucaoDto
            {
                IdCaucao = entidade.IdCaucao,
                Valor = entidade.Valor,
                Status = entidade.Status
            };
        }
    }

}
