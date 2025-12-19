using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Domain.Entidades;

namespace Locadora_Auto.Application.Models.Mappers
{
    public static class RefreshTokenMapper
    {
        public static RefreshTokenDto ToDto(this RefreshToken entidade)
        {
            return new RefreshTokenDto
            {
                Token = entidade.Token,
                ExpiraEm = entidade.ExpiraEm,
                Revogado = entidade.Revogado,
                CriadoEm = entidade.CriadoEm
            };
        }
    }

}
