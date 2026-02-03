using Locadora_Auto.Application.Models.Dto.Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Domain.Entidades;

namespace Locadora_Auto.Application.Models.Mappers
{
    public static class FilialMapper
    {
        public static FilialDto ToDto(this Filial filial,
            int totalVeiculos = 0,
            int veiculosDisponiveis = 0,
            int totalLocacoesMes = 0)
        {
            if (filial == null) return null;

            return new FilialDto
            {
                IdFilial = filial.IdFilial,
                Nome = filial.Nome,
                Cidade = filial.Cidade,
                Ativo = filial.Ativo,
                Endereco = filial.Endereco?.ToDto(),
                TotalVeiculos = totalVeiculos,
                VeiculosDisponiveis = veiculosDisponiveis,
                TotalLocacoesMes = totalLocacoesMes
            };
        }

        public static FilialResumoDto ToResumoDto(this Filial filial,
            int totalVeiculos = 0,
            int veiculosDisponiveis = 0)
        {
            if (filial == null) return null;

            return new FilialResumoDto
            {
                IdFilial = filial.IdFilial,
                Nome = filial.Nome,
                Cidade = filial.Cidade,
                Endereco = filial.Endereco?.ToDto(),
                Ativo = filial.Ativo,
                TotalVeiculos = totalVeiculos,
                VeiculosDisponiveis = veiculosDisponiveis
            };
        }
    }
}