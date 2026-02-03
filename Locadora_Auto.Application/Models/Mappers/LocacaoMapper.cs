using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Application.Models.Mappers;
using Locadora_Auto.Domain.Entidades;

namespace Locadora_Auto.Application.Mappers
{
    public static class LocacaoMapper
    {
        // Locacao → LocacaoDto
        public static LocacaoDto ToDto(this Locacao locacao)
        {
            if (locacao == null) return null!;

            return new LocacaoDto
            {
                IdLocacao = locacao.IdLocacao,
                //IdCliente = locacao.ClienteId,
                //IdVeiculo = locacao.IdVeiculo,
                IdFuncionario = locacao.IdFuncionario,
                IdFilialRetirada = locacao.IdFilialRetirada,
                IdFilialDevolucao = locacao.IdFilialDevolucao,
                DataInicio = locacao.DataInicio,
                DataFimPrevista = locacao.DataFimPrevista,
                DataFimReal = locacao.DataFimReal,
                KmInicial = locacao.KmInicial,
                KmFinal = locacao.KmFinal,
                ValorPrevisto = locacao.ValorPrevisto,
                ValorFinal = locacao.ValorFinal,
                Status = locacao.Status.ToString(),
                Multas = locacao.Multas.ToDtoList(),
                Pagamentos = locacao.Pagamentos.ToDtoList(),
                ClienteDto = locacao.Cliente.ToDto(),
                FuncionarioDto = locacao.Funcionario.ToDto()
            };
        }

        public static List<LocacaoDto> ToDtoList(this IEnumerable<Locacao> entidades)
        {
            if (entidades == null) return new List<LocacaoDto>();
            return entidades.Select(ToDto).ToList();
        }
    }
}
