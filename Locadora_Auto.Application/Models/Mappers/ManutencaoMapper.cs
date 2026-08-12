using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Domain.Entidades;

namespace Locadora_Auto.Application.Models.Mappers
{
    public static class ManutencaoMapper
    {
        /// <param name="idVeiculo">
        /// O veículo dono da manutenção vem por FK sombra ("id_veiculo"), então precisa ser informado por quem chama.
        /// </param>
        public static ManutencaoDto ToDto(this Manutencao entidade, int idVeiculo)
        {
            if (entidade == null) return null;

            return new ManutencaoDto
            {
                IdManutencao = entidade.IdManutencao,
                IdVeiculo = idVeiculo,
                IdTipoManutencao = (int)entidade.Tipo,
                Tipo = entidade.Tipo.ToString(),
                Descricao = entidade.Descricao,
                Custo = entidade.Custo,
                DataInicio = entidade.DataInicio,
                DataFim = entidade.DataFim,
                IdStatus = (int)entidade.Status,
                Status = entidade.Status.ToString()
            };
        }
    }

}
