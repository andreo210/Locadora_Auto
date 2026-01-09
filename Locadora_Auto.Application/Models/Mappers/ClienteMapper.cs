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
                Status = true,
                Endereco = dto.Endereco.ToEntity()
            };
        }

        public static Clientes ToEntity(this AtualizarClienteDto dto)
        {
            return new Clientes
            {
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
                Status = entidade.Status,
                Endereco = entidade.Endereco?.ToDto(),
                NumeroHabilitacao = entidade.NumeroHabilitacao,
                TotalLocacoes = entidade.TotalLocacoes,
                ValidadeHabilitacao = entidade.ValidadeHabilitacao
            };
        }

        public static ClienteDto ToViewDto(this Clientes entidade)
        {
            return new ClienteDto
            {
                IdCliente = entidade.IdCliente,
                Nome = entidade.Usuario.NomeCompleto,
                Cpf = entidade.Usuario.Cpf,
                Telefone = entidade.Usuario.PhoneNumber,
                Email = entidade.Usuario.Email    ,
                Status = entidade.Status,
                Endereco = entidade.Endereco?.ToDto()
            };
        }
    }
}
