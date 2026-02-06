using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Domain.Entidades;

namespace Locadora_Auto.Application.Models.Mappers
{
    public static class VistoriaMapper
    {
        public static VistoriaDto ToDto(this Vistoria entidade)
        {
            return new VistoriaDto
            {
                IdVistoria = entidade.IdVistoria,
                Tipo = entidade.Tipo.ToString(),
                NivelCombustivel = entidade.Combustivel.ToString(),
                Observacoes = entidade.Observacoes,
                DataVistoria = entidade.DataVistoria,
                IdFuncionario = entidade.IdFuncionario,
                IdLocacao = entidade.IdLocacao,
                KmVeiculo = entidade.KmVeiculo,
                
            };
        }

        /// <summary>
        /// Converte uma lista de Contato para uma lista de ContatoDto
        /// </summary>
        public static List<VistoriaDto> ToDtoList(this IEnumerable<Vistoria> entidades)
        {
            if (entidades == null) return new List<VistoriaDto>();
            return entidades.Select(ToDto).ToList();
        }
    }
}
