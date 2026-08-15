using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Domain.Entidades;

namespace Locadora_Auto.Application.Models.Mappers
{
    public static class MovimentoVeiculoMapper
    {
        public static MovimentoVeiculoDto ToDto(this MovimentoVeiculo movimento)
        {
            if (movimento == null) return null;

            return new MovimentoVeiculoDto
            {
                IdMovimentoVeiculo = movimento.IdMovimentoVeiculo,
                IdVeiculo = movimento.IdVeiculo,
                IdStatusOrigem = (int?)movimento.StatusOrigem,
                StatusOrigem = movimento.StatusOrigem?.ToString(),
                IdStatusDestino = (int)movimento.StatusDestino,
                StatusDestino = movimento.StatusDestino.ToString(),
                IdTipoOrigem = (int)movimento.TipoOrigem,
                TipoOrigem = movimento.TipoOrigem.ToString(),
                IdLocacaoOrigem = movimento.IdLocacaoOrigem,
                IdManutencaoOrigem = movimento.IdManutencaoOrigem,
                DataMovimento = movimento.DataMovimento,
                Autor = movimento.IdUsuarioCriacao
            };
        }

        public static List<MovimentoVeiculoDto> ToDtoList(this IEnumerable<MovimentoVeiculo> movimentos)
        {
            if (movimentos == null) return new List<MovimentoVeiculoDto>();
            return movimentos.Select(ToDto).ToList();
        }
    }
}
