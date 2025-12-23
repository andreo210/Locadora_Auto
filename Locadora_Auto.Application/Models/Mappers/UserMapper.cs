using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Domain.Entidades.Indentity;

namespace Locadora_Auto.Application.Models.Mappers
{
    public static class UserMapper
    {
        /// <summary>
        /// Converte ContatoDto para a entidade Contato
        /// </summary>
        public static User ToEntity(this UserDto dto)
        {
            if (dto == null) return null;

            return new User
            {
                Id = dto.Id,
                Cpf = dto.Cpf,
                NomeCompleto = dto.NomeCompleto,
                Email = dto.Email ?? string.Empty,
                Ativo = dto.Ativo,
                UserName = dto.Cpf
                
            };
        }


        /// <summary>
        /// Converte ContatoCommand para a entidade Contato
        /// </summary>
        public static User CreateToEntity(this CreateUserDto dto)
        {
            if (dto == null) return null;

            return new User
            {
                Cpf = dto.Cpf,
                NomeCompleto = dto.NomeCompleto,
                Email = dto.Email ?? string.Empty,
                Ativo = true,
                DataCriacao = DateTime.UtcNow,
                UserName = dto.Cpf
            };
        }

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
        /// Converte uma lista de ContatoDto para uma lista de Contato
        /// </summary>
        public static List<User> ToEntityList(this IEnumerable<UserDto> dtos)
        {
            if (dtos == null) return new List<User>();
            return dtos.Select(ToEntity).ToList();
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
