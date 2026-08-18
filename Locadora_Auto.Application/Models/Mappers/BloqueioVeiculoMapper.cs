using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Domain.Entidades;

namespace Locadora_Auto.Application.Models.Mappers
{
    public static class BloqueioVeiculoMapper
    {
        /// <param name="agora">
        /// Instante contra o qual o vencimento é medido. Entra como parâmetro, e não como um
        /// <c>UtcNow</c> aqui dentro, para uma lista inteira ser julgada pelo mesmo relógio — sem
        /// isso duas linhas da mesma página poderiam discordar sobre o mesmo prazo.
        /// </param>
        public static BloqueioVeiculoDto ToDto(this BloqueioVeiculo bloqueio, DateTime agora)
        {
            if (bloqueio == null) return null;

            return new BloqueioVeiculoDto
            {
                IdBloqueioVeiculo = bloqueio.IdBloqueioVeiculo,
                IdVeiculo = bloqueio.IdVeiculo,
                IdMotivo = (int)bloqueio.Motivo,
                Motivo = bloqueio.Motivo.ToString(),
                Observacao = bloqueio.Observacao,
                DataBloqueio = bloqueio.DataBloqueio,
                DataPrevistaLiberacao = bloqueio.DataPrevistaLiberacao,
                DataLiberacao = bloqueio.DataLiberacao,
                IdStatusAnterior = (int)bloqueio.StatusAnterior,
                StatusAnterior = bloqueio.StatusAnterior.ToString(),
                IdFuncionarioResponsavel = bloqueio.IdFuncionarioResponsavel,

                // a navegação só vem quando o chamador pediu o Include; sem ela o id continua
                // respondendo quem é o responsável
                Responsavel = bloqueio.Responsavel?.Matricula,

                EmAberto = bloqueio.EmAberto,
                Vencido = bloqueio.Vencido(agora)
            };
        }

        public static List<BloqueioVeiculoDto> ToDtoList(this IEnumerable<BloqueioVeiculo> bloqueios, DateTime agora)
        {
            if (bloqueios == null) return new List<BloqueioVeiculoDto>();
            return bloqueios.Select(b => b.ToDto(agora)).ToList();
        }
    }
}
