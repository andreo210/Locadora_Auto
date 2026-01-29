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
                KmAtual = veiculo.KmAtual,
                Ativo = veiculo.Ativo,
                Disponivel = veiculo.Disponivel,

                IdCategoria = veiculo.IdCategoria,
                Categoria = veiculo.Categoria?.Nome ?? string.Empty,

                IdFilialAtual = veiculo.FilialAtualId,
                Filial = veiculo.FilialAtual?.Nome ?? string.Empty
            };
        }              
    }
}
