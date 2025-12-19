using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Domain.Entidades;

namespace Locadora_Auto.Application.Models.Mappers
{
    public static class ReservaMapper
    {
        public static ReservaDto ToDto(this Reserva entidade)
        {
            return new ReservaDto
            {
                IdReserva = entidade.IdReserva,
                DataInicio = entidade.DataInicio,
                DataFim = entidade.DataFim,
                Status = entidade.Status,
                //Cliente = entidade.Cliente?.ToDto(),
                //Categoria = entidade.Categoria?.ToDto()
            };
        }
    }

}
