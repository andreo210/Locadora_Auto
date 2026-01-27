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
                Tipo = entidade.Tipo,
                Valor = entidade.Valor,
                Status = entidade.Status
            };
        }

        public static Multa ToEntity(this MultaDto entidade)
        {
            return new Multa
            {
                IdMulta = entidade.IdMulta,
                Tipo = entidade.Tipo,
                Valor = entidade.Valor,
                Status = entidade.Status
            };
        }
    }
}
