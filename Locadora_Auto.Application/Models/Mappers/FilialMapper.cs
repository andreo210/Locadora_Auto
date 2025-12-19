using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Domain.Entidades;

namespace Locadora_Auto.Application.Models.Mappers
{
    public static class FilialMapper
    {
        public static FilialDto ToDto(this Filial entidade)
        {
            return new FilialDto
            {
                IdFilial = entidade.IdFilial,
                Nome = entidade.Nome,
                Cidade = entidade.Cidade,
                Ativo = entidade.Ativo
            };
        }
    }
}
