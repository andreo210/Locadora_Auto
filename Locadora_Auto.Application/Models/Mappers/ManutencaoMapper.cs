using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Domain.Entidades;

namespace Locadora_Auto.Application.Models.Mappers
{
    public static class ManutencaoMapper
    {
        public static ManutencaoDto ToDto(this Manutencao entidade)
        {
            return new ManutencaoDto
            {
                IdManutencao = entidade.IdManutencao,
                Tipo = entidade.Tipo,
                Descricao = entidade.Descricao,
                Custo = entidade.Custo,
                //DataInicio = entidade.DataInicio,
                //DataFim = entidade.DataFim,
                Status = entidade.Status
            };
        }
    }

}
