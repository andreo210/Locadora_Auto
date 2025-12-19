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
                Cep = entidade.Cep
            };
        }
    }
}
