using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Domain.Entidades;

namespace Locadora_Auto.Application.Models.Mappers
{
    public static class VeiculoMapper
    {
        public static Veiculo ToEntity(this VeiculoCreateDto dto)
        {
            return new Veiculo
            {
                Placa = dto.Placa,
                Chassi = dto.Chassi,
                IdCategoria = dto.IdCategoria,
                KmAtual = dto.KmAtual,
                Status = "DISPONIVEL",
                IdFilialAtual = dto.IdFilialAtual
            };
        }

        public static VeiculoDto ToDto(this Veiculo entidade)
        {
            return new VeiculoDto
            {
                IdVeiculo = entidade.IdVeiculo,
                Placa = entidade.Placa,
                Chassi = entidade.Chassi,
                KmAtual = entidade.KmAtual,
                Status = entidade.Status
            };
        }

        public static VeiculoDto ToViewDto(this Veiculo entidade)
        {
            return new VeiculoDto
            {
                IdVeiculo = entidade.IdVeiculo,
                Placa = entidade.Placa,
                Chassi = entidade.Chassi,
                KmAtual = entidade.KmAtual,
                Status = entidade.Status,
                Categoria = entidade.Categoria?.ToDto(),
                //Filial = entidade.FilialAtual?.ToDto()
            };
        }
    }
}
