using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Domain.Entidades;

namespace Locadora_Auto.Application.Models.Mappers
{
    public static class ReservaMapper
    {
        /// <summary>
        /// Os nomes de cliente, filial e categoria só vêm preenchidos quando a consulta usou os Includes
        /// correspondentes; sem eles o Dto sai apenas com os ids.
        /// </summary>
        public static ReservaDto ToDto(this Reserva entidade)
        {
            return new ReservaDto
            {
                IdReserva = entidade.IdReserva,
                DataInicio = entidade.DataInicio,
                DataFim = entidade.DataFim,
                Ativo = entidade.Ativo,
                IdCliente = entidade.IdCliente,
                IdCategoriaVeiculo = entidade.IdCategoria,
                IdFilial = entidade.IdFilial,
                IdStatus = (int)entidade.Status,
                Status = entidade.Status.ToString(),
                NomeCliente = entidade.Cliente?.Usuario?.NomeCompleto,
                NomeFilial = entidade.Filial?.Nome,
                NomeCategoriaVeiculo = entidade.CategoriaVeiculo?.Nome
            };
        }

        public static List<ReservaDto> ToDtoList(this IEnumerable<Reserva> entidades)
        {
            if (entidades == null) return new List<ReservaDto>();
            return entidades.Select(ToDto).ToList();
        }
    }

}
