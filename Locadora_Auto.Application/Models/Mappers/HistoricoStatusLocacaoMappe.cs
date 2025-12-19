using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Domain.Entidades;

namespace Locadora_Auto.Application.Models.Mappers
{
    public static class HistoricoStatusLocacaoMapper
    {
        public static HistoricoStatusLocacaoDto ToDto(this HistoricoStatusLocacao entidade)
        {
            return new HistoricoStatusLocacaoDto
            {
                Status = entidade.Status,
                DataStatus = entidade.DataStatus,
                //Funcionario = entidade.Funcionario?.ToDto()
            };
        }
    }

}
