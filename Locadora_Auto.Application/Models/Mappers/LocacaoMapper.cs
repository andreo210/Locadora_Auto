using Locadora_Auto.Application.Models.Dto;
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
                IdCliente = locacao.ClienteId,
                IdVeiculo = locacao.IdVeiculo,
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
                Status = locacao.Status.ToString()
            };
        }

        // CriarLocacaoDto → Locacao (via factory)
        public static Locacao ToEntity(
            this CriarLocacaoDto dto,
            Clientes cliente,
            Veiculo veiculo,
            Funcionario funcionario)
        {
            return Locacao.Criar(
                cliente,
                veiculo,
                funcionario,
                dto.IdFilialRetirada,
                dto.DataInicio,
                dto.DataFimPrevista,
                dto.KmInicial,
                dto.ValorPrevisto
            );
        }

        // Atualizar entidade com AtualizarLocacaoDto
        public static void UpdateFromDto(this Locacao locacao, AtualizarLocacaoDto dto)
        {
            if (dto.IdFuncionario > 0)
                locacao.GetType().GetProperty("IdFuncionario")!.SetValue(locacao, dto.IdFuncionario);

            if (dto.IdFilialDevolucao.HasValue)
                locacao.GetType().GetProperty("IdFilialDevolucao")!.SetValue(locacao, dto.IdFilialDevolucao);

            if (dto.DataFimReal.HasValue)
                locacao.GetType().GetProperty("DataFimReal")!.SetValue(locacao, dto.DataFimReal);

            if (dto.KmFinal.HasValue)
                locacao.GetType().GetProperty("KmFinal")!.SetValue(locacao, dto.KmFinal);

            if (dto.ValorFinal.HasValue)
                locacao.GetType().GetProperty("ValorFinal")!.SetValue(locacao, dto.ValorFinal);

            if (!string.IsNullOrWhiteSpace(dto.Status) &&
                Enum.TryParse(typeof(StatusLocacao), dto.Status, out var status))
            {
                locacao.GetType().GetProperty("Status")!.SetValue(locacao, status);
            }
        }
    }
}
