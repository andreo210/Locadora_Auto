using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Domain.Entidades;

namespace Locadora_Auto.Application.Models.Mappers
{
    public static class FuncionarioMapper
    {
        public static FuncionarioDto ToDto(this Funcionario entidade)
        {
            if (entidade == null) return null;

            return new FuncionarioDto
            {
                IdFuncionario = entidade.IdFuncionario,
                UsuarioId = entidade.Usuario?.Id ?? string.Empty,
                Status = entidade.Ativo,
                Cpf = entidade.Usuario?.Cpf ?? string.Empty,
                Nome = entidade.Usuario?.NomeCompleto ?? string.Empty,
                Email = entidade.Usuario?.Email ?? string.Empty,
                Telefone = entidade.Usuario?.PhoneNumber ?? string.Empty
            };
        }

        
        /// <summary>
        /// Converte uma lista de Funcionario para uma lista de FuncionarioDto
        /// </summary>
        public static List<FuncionarioDto> ToDtoList(this IEnumerable<Funcionario> entidades)
        {
            if (entidades == null) return new List<FuncionarioDto>();
            return entidades.Select(ToDto).ToList();
        }


    }
}
