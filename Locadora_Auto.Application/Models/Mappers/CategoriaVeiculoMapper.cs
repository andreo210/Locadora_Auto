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
                ValorKmExcedente = categoria.ValorKmExcedente,
                Fotos = categoria.Fotos != null ? categoria.Fotos.ToDtoList() : new List<FotoDto>()
            };
        }

        public static List<CategoriaVeiculoDto> ToDtoList(this IEnumerable<CategoriaVeiculo> entidades)
        {
            if (entidades == null) return new List<CategoriaVeiculoDto>();
            return entidades.Select(ToDto).ToList();
        }
    }
}
