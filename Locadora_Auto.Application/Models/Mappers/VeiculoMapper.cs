using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Domain.Entidades;

namespace Locadora_Auto.Application.Models.Mappers
{
    public static class VeiculoMapper
    {
        public static VeiculoDto ToDto(this Veiculo veiculo)
        {
            if (veiculo == null) return null;

            return new VeiculoDto
            {
                IdVeiculo = veiculo.IdVeiculo,
                Placa = veiculo.Placa,
                Marca = veiculo.Marca,
                Modelo = veiculo.Modelo,
                Ano = veiculo.Ano,
                Chassi = veiculo.Chassi,
                KmAtual = veiculo.KmAtual,
                Ativo = veiculo.Ativo,
                Disponivel = veiculo.Disponivel,
                IdCategoria = veiculo.IdCategoria,
                Categoria = veiculo.Categoria?.Nome ?? string.Empty,
                CapacidadeTanqueLitros = veiculo.CapacidadeTanqueLitros,
                MotivoDesmobilizacao = veiculo.MotivoDesmobilizacao,
                DataDesmobilizacao = veiculo.DataDesmobilizacao,
                IdFuncionarioDesmobilizacao = veiculo.IdFuncionarioDesmobilizacao,
                IdFilialAtual = veiculo.FilialAtualId,
                IdStatus = (int)veiculo.Status,
                Status = veiculo.Status.ToString(),
                Filial = veiculo.FilialAtual?.Nome ?? string.Empty
            };
        }

        public static List<VeiculoDto> ToDtoList(this IEnumerable<Veiculo> veiculos)
        {
            if (veiculos == null) return new List<VeiculoDto>();
            return veiculos.Select(ToDto).ToList();
        }
    }
}
