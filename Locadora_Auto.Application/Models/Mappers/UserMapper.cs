using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Domain.Entidades.Indentity;

namespace Locadora_Auto.Application.Models.Mappers
{
    public static class UserMapper
    {        

        /// <summary>
        /// Converte Contato para ContatoDto
        /// </summary>
        public static UserDto ToDto(this User entidade)
        {
            if (entidade == null) return null;

            return new UserDto
            {
                Id = entidade.Id,
                NomeCompleto = entidade.NomeCompleto,
                Email = entidade.Email ?? string.Empty,
                Ativo = entidade.Ativo,
                Cpf = entidade.UserName
            };
        }


        /// <summary>
        /// Converte uma lista de Contato para uma lista de ContatoDto
        /// </summary>
        public static List<UserDto> ToDtoList(this IEnumerable<User> entidades)
        {
            if (entidades == null) return new List<UserDto>();
            return entidades.Select(ToDto).ToList();
        }
    }
}
