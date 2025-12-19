using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Domain.Entidades;

namespace Locadora_Auto.Application.Models.Mappers
{
    public static class ClienteMapper
    {
        public static Cliente ToEntity(this ClienteCreateDto dto)
        {
            return new Cliente
            {
                Nome = dto.Nome,
                Cpf = dto.Cpf,
                Telefone = dto.Telefone,
                Email = dto.Email,
                Status = "ATIVO"
            };
        }

        public static ClienteDto ToDto(this Cliente entidade)
        {
            if (entidade == null) return null;

            return new ClienteDto
            {
                IdCliente = entidade.IdCliente,
                Nome = entidade.Nome,
                Cpf = entidade.Cpf,
                Telefone = entidade.Telefone,
                Email = entidade.Email,
                Status = entidade.Status
            };
        }

        public static ClienteDto ToViewDto(this Cliente entidade)
        {
            return new ClienteDto
            {
                IdCliente = entidade.IdCliente,
                Nome = entidade.Nome,
                Cpf = entidade.Cpf,
                Telefone = entidade.Telefone,
                Email = entidade.Email,
                Status = entidade.Status,
                Endereco = entidade.Endereco?.ToDto()
            };
        }
    }
}
