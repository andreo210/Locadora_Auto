using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Domain.Entidades;

namespace Locadora_Auto.Application.Models.Mappers
{
    public static class PagamentoMapper
    {
        public static PagamentoDto ToDto(this Pagamento entidade)
        {
            return new PagamentoDto
            {
                IdPagamento = entidade.IdPagamento,
                Valor = entidade.Valor,
                DataPagamento = entidade.DataPagamento,
                Status = entidade.Status.ToString(),
                FormaPagamento = entidade.FormaPagamento.ToString()
            };
        }

        public static List<PagamentoDto> ToDtoList(this IEnumerable<Pagamento> entidades)
        {
            if (entidades == null) return new List<PagamentoDto>();
            return entidades.Select(ToDto).ToList();
        }
    }
}
