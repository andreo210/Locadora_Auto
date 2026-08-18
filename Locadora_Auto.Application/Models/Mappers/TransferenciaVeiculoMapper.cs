using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Domain.Entidades;

namespace Locadora_Auto.Application.Models.Mappers
{
    public static class TransferenciaVeiculoMapper
    {
        /// <param name="agora">
        /// Mesmo relógio para a lista inteira: duas linhas da mesma resposta não podem discordar
        /// sobre qual viagem está atrasada.
        /// </param>
        public static TransferenciaVeiculoDto ToDto(this TransferenciaVeiculo transferencia, DateTime agora)
        {
            if (transferencia == null) return null;

            return new TransferenciaVeiculoDto
            {
                IdTransferenciaVeiculo = transferencia.IdTransferenciaVeiculo,
                IdVeiculo = transferencia.IdVeiculo,
                IdFilialOrigem = transferencia.IdFilialOrigem,
                FilialOrigem = transferencia.FilialOrigem?.Nome,
                IdFilialDestino = transferencia.IdFilialDestino,
                FilialDestino = transferencia.FilialDestino?.Nome,
                DataEnvio = transferencia.DataEnvio,
                DataPrevistaChegada = transferencia.DataPrevistaChegada,
                DataChegada = transferencia.DataChegada,
                IdStatus = (int)transferencia.Status,
                Status = transferencia.Status.ToString(),
                IdFuncionarioResponsavel = transferencia.IdFuncionarioResponsavel,
                Responsavel = transferencia.Responsavel?.Matricula,
                Observacao = transferencia.Observacao,
                Atrasada = transferencia.Atrasada(agora)
            };
        }

        public static List<TransferenciaVeiculoDto> ToDtoList(this IEnumerable<TransferenciaVeiculo> transferencias, DateTime agora)
        {
            if (transferencias == null) return new List<TransferenciaVeiculoDto>();
            return transferencias.Select(t => t.ToDto(agora)).ToList();
        }
    }
}
