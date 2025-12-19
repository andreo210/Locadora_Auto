using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Domain.Entidades;

namespace Locadora_Auto.Application.Models.Mappers
{
    public static class LocacaoMapper
    {
        public static Locacao ToEntity(this LocacaoCreateDto dto)
        {
            return new Locacao
            {
                IdCliente = dto.IdCliente,
                IdVeiculo = dto.IdVeiculo,
                IdFuncionario = dto.IdFuncionario,
                IdFilialRetirada = dto.IdFilialRetirada,
                DataInicio = dto.DataInicio,
                DataFimPrevista = dto.DataFimPrevista,
                //KmInicial = dto.KmInicial,
                //ValorPrevisto = dto.ValorPrevisto,
                Status = "ABERTA"
            };
        }

        public static LocacaoDto ToDto(this Locacao entidade)
        {
            return new LocacaoDto
            {
                IdLocacao = entidade.IdLocacao,
                DataInicio = entidade.DataInicio,
                DataFimPrevista = entidade.DataFimPrevista,
                DataFimReal = entidade.DataFimReal,
               // ValorPrevisto = entidade.ValorPrevisto,
                //ValorFinal = entidade.ValorFinal,
                Status = entidade.Status
            };
        }

        public static LocacaoDto ToViewDto(this Locacao entidade)
        {
            return new LocacaoDto
            {
                IdLocacao = entidade.IdLocacao,
                Cliente = entidade.Cliente?.ToDto()!,
                Veiculo = entidade.Veiculo?.ToDto()!,
                //Funcionario = entidade.Funcionario?.ToDto(),
               // FilialRetirada = entidade.FilialRetirada?.ToDto(),
                //FilialDevolucao = entidade.FilialDevolucao?.ToDto(),
                DataInicio = entidade.DataInicio,
                DataFimPrevista = entidade.DataFimPrevista,
                DataFimReal = entidade.DataFimReal,
                Status = entidade.Status,
                //ValorFinal = entidade.ValorFinal,
                //Pagamentos = entidade.Pagamentos?.Select(p => p.ToDto()).ToList(),
                //Multas = entidade.Multas?.Select(m => m.ToDto()).ToList(),
                //Vistorias = entidade.Vistorias?.Select(v => v.ToDto()).ToList()
            };
        }

        public static LocacaoAdicionalDto ToDto(this LocacaoAdicional entidade)
        {
            return new LocacaoAdicionalDto
            {
                Quantidade = entidade.Quantidade,
                //Adicional = entidade.Adicional?.ToDto()
            };
        }
    }
}
