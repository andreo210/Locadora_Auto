using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Domain.Entidades;

namespace Locadora_Auto.Application.Models.Mappers
{
    public static class CategoriaVeiculoMapper
    {
        public static CategoriaVeiculoDto ToDto(this CategoriaVeiculo entidade)
        {
            return new CategoriaVeiculoDto
            {
                IdCategoria = entidade.IdCategoria,
                Nome = entidade.Nome,
                ValorDiaria = entidade.ValorDiaria,
                LimiteKm = entidade.LimiteKm,
                ValorKmExcedente = entidade.ValorKmExcedente
            };
        }
    }
}
