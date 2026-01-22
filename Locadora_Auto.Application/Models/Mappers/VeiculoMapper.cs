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

        public static Veiculo ToEntity(this CriarVeiculoDto dto)
        {
            return new Veiculo
            {
                Placa = dto.Placa.Trim().ToUpper(),
                Marca = dto.Marca.Trim(),
                Modelo = dto.Modelo.Trim(),
                Ano = dto.Ano,
                Chassi = dto.Chassi.Trim().ToUpper(),
                KmAtual = dto.KmInicial,
                IdCategoria = dto.IdCategoria,
                FilialAtualId = dto.IdFilialAtual,
                Ativo = true,
                Disponivel = true
            };
        }

        public static void Atualizar(this Veiculo veiculo, AtualizarVeiculoDto dto)
        {
            if (dto.Marca != null)
                veiculo.Marca = dto.Marca.Trim();

            if (dto.Modelo != null)
                veiculo.Modelo = dto.Modelo.Trim();

            if (dto.Ano.HasValue)
                veiculo.Ano = dto.Ano.Value;

            if (dto.KmAtual.HasValue)
                veiculo.KmAtual = dto.KmAtual.Value;

            if (dto.IdFilialAtual.HasValue)
                veiculo.FilialAtualId = dto.IdFilialAtual.Value;
        }
    }
}
