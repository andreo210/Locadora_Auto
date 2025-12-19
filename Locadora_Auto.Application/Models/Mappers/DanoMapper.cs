using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Domain.Entidades;

namespace Locadora_Auto.Application.Models.Mappers
{
    public static class DanoMapper
    {
        public static DanoDto ToDto(this Dano entidade)
        {
            return new DanoDto
            {
                IdDano = entidade.IdDano,
                Descricao = entidade.Descricao,
                ValorEstimado = entidade.ValorEstimado,
               // CobertoSeguro = entidade.CobertoSeguro
            };
        }
    }
}
