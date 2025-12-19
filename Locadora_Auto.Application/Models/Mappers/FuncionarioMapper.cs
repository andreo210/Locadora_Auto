using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Domain.Entidades;

namespace Locadora_Auto.Application.Models.Mappers
{
    public static class FuncionarioMapper
    {
        public static FuncionarioDto ToDto(this Funcionario entidade)
        {
            return new FuncionarioDto
            {
                IdFuncionario = entidade.IdFuncionario,
                Nome = entidade.Nome,
                Cargo = entidade.Cargo,
                Status = entidade.Status
            };
        }
    }
}
