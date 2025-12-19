using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Domain.Entidades;

namespace Locadora_Auto.Application.Models.Mappers
{
    public static class FormaPagamentoMapper
    {
        public static FormaPagamentoDto ToDto(this FormaPagamento entidade)
        {
            return new FormaPagamentoDto
            {
                IdFormaPagamento = entidade.IdFormaPagamento,
                Descricao = entidade.Descricao
            };
        }
    }
}
