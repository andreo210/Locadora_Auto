using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Domain.Entidades;

namespace Locadora_Auto.Application.Models.Mappers
{
    public static class ClienteMapper
    {

        public static ClienteDto ToDto(this Clientes entidade)
        {
            if (entidade == null) return null;

            return new ClienteDto
            {
                IdCliente = entidade.IdCliente,
                Ativo = entidade.Ativo,
                Endereco = entidade.Endereco?.ToDto(),
                NumeroHabilitacao = entidade.NumeroHabilitacao,
                TotalLocacoes = entidade.TotalLocacoes,
                ValidadeHabilitacao = entidade.ValidadeHabilitacao,
                Cpf = entidade.Usuario?.Cpf ?? string.Empty,
                Nome = entidade.Usuario?.NomeCompleto ?? string.Empty,
                Email = entidade.Usuario?.Email ?? string.Empty,
                Telefone = entidade.Usuario?.PhoneNumber ?? string.Empty,
                Reservas = entidade.Reservas.ToDtoList().Where(x => x.Ativo == true)

            };
        }

        public static List<ClienteDto> ToDtoList(this IEnumerable<Clientes> entidades)
        {
            if (entidades == null) return new List<ClienteDto>();
            return entidades.Select(ToDto).ToList();
        }
    }
}
