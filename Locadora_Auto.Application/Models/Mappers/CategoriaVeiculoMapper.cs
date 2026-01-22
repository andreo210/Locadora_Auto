using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Domain.Entidades;

namespace Locadora_Auto.Application.Models.Mappers
{
    public static class CategoriaVeiculoMapper
    {
        public static CategoriaVeiculoDto ToDto(this CategoriaVeiculo categoria)
        {
            if (categoria == null) return null;

            return new CategoriaVeiculoDto
            {
                Id = categoria.Id,
                Nome = categoria.Nome,
                ValorDiaria = categoria.ValorDiaria,
                LimiteKm = categoria.LimiteKm,
                ValorKmExcedente = categoria.ValorKmExcedente
            };
        }

        public static CategoriaVeiculo ToEntity(this CriarCategoriaVeiculoDto dto)
        {
            return new CategoriaVeiculo
            { 
                Nome = dto.Nome.Trim(),
                ValorDiaria = dto.ValorDiaria,
                LimiteKm = dto.LimiteKm,
                ValorKmExcedente = dto.ValorKmExcedente
            };
        }

        public static void AtualizarDto(this CategoriaVeiculo categoria, AtualizarCategoriaVeiculoDto dto)
        {
            if (!string.IsNullOrWhiteSpace(dto.Nome))
                categoria.Nome = dto.Nome.Trim();

            categoria.ValorDiaria = dto.ValorDiaria;
            categoria.LimiteKm = dto.LimiteKm;
            categoria.ValorKmExcedente = dto.ValorKmExcedente;
        }

        public static List<CategoriaVeiculoDto> ToDtoList(this IEnumerable<CategoriaVeiculo> entidades)
        {
            if (entidades == null) return new List<CategoriaVeiculoDto>();
            return entidades.Select(ToDto).ToList();
        }
    }
}
