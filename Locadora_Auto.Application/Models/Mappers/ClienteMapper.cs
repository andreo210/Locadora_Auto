using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Domain.Entidades;

namespace Locadora_Auto.Application.Models.Mappers
{
    public static class ClienteMapper
    {
        public static Clientes ToEntity(this CriarClienteDto dto)
        {
            return new Clientes
            {
                Nome = dto.Nome,
                Cpf = dto.Cpf,
                Telefone = dto.Telefone,
                Email = dto.Email,
                Status = true,
                Endereco = dto.Endereco.ToEntity()
            };
        }

        public static Clientes ToEntity(this AtualizarClienteDto dto)
        {
            return new Clientes
            {
                Nome = dto.Nome,
                Telefone = dto.Telefone,
                Email = dto.Email,
                Status = dto.Status,
                Endereco = dto.Endereco.ToEntity()
            };
        }

        public static ClienteDto ToDto(this Clientes entidade)
        {
            if (entidade == null) return null;
            //if (entidade.Endereco == null) return null;
            return new ClienteDto
            {
                IdCliente = entidade.IdCliente,
                Nome = entidade.Nome,
                Cpf = entidade.Cpf,
                Telefone = entidade.Telefone,
                Email = entidade.Email,
                Status = entidade.Status,
                Endereco = entidade.Endereco?.ToDto(),
            };
        }

        public static ClienteDto ToViewDto(this Clientes entidade)
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
