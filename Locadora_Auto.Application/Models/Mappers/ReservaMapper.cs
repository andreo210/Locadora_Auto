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
                Ativo = entidade.Ativo,
                IdCategoriaVeiculo =entidade.IdCategoria,
                IdFilial = entidade.IdFilial
                //Cliente = entidade.Cliente?.ToDto(),
                //Categoria = entidade.Categoria?.ToDto()
            };
        }

        public static List<ReservaDto> ToDtoList(this IEnumerable<Reserva> entidades)
        {
            if (entidades == null) return new List<ReservaDto>();
            return entidades.Select(ToDto).ToList();
        }
    }

}
