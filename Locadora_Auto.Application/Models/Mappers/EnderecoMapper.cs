using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Domain.Entidades;

namespace Locadora_Auto.Application.Models.Mappers
{
    public static class EnderecoMapper
    {
        public static EnderecoDto ToDto(this Endereco entidade)
        {
            if (entidade == null) return null;

            return new EnderecoDto
            {
                Logradouro = entidade.Logradouro,
                Numero = entidade.Numero,
                Bairro = entidade.Bairro,
                Cidade = entidade.Cidade,
                Estado = entidade.Estado,
                Cep = entidade.Cep,
                DataCriacao = entidade.DataCriacao,
                Complemento = entidade.Complemento               
            };
        }

        /// <summary>
        /// Converte ContatoDto para a entidade Contato
        /// </summary>
        public static Endereco ToEntity(this CriarEnderecoDto dto)
        {
            if (dto == null) return null;

            return new Endereco
            {
                Logradouro = dto.Logradouro,
                Numero = dto.Numero,
                Bairro = dto.Bairro,
                Cidade = dto.Cidade,
                Estado = dto.Estado,
                Cep = dto.Cep,
                Complemento = dto.Complemento ?? string.Empty                
            };            
        }

        /// <summary>
        /// Converte ContatoDto para a entidade Contato
        /// </summary>
        public static Endereco ToEntity(this EnderecoDto dto)
        {
            if (dto == null) return null;

            return new Endereco
            {
                Logradouro = dto.Logradouro,
                Numero = dto.Numero,
                Bairro = dto.Bairro,
                Cidade = dto.Cidade,
                Estado = dto.Estado,
                Cep = dto.Cep,
                Complemento = dto.Complemento ?? string.Empty
            };
        }

        /// <summary>
        /// Converte uma lista de Contato para uma lista de ContatoDto
        /// </summary>
        public static List<EnderecoDto> ToDtoList(this IEnumerable<Endereco> entidades)
        {
            if (entidades == null) return new List<EnderecoDto>();
            return entidades.Select(ToDto).ToList();
        }
    }
}
