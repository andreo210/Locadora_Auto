using Locadora_Auto.Application.Models.Dto.Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Domain.Entidades;

namespace Locadora_Auto.Application.Models.Mappers
{
    public static class FilialMapper
    {
        public static FilialDto ToDto(this Filial filial,
            int totalVeiculos = 0,
            int veiculosDisponiveis = 0,
            int totalLocacoesMes = 0)
        {
            if (filial == null) return null;

            return new FilialDto
            {
                IdFilial = filial.IdFilial,
                Nome = filial.Nome,
                Cidade = filial.Cidade,
                Ativo = filial.Ativo,
                Logradouro = filial.Endereco?.Logradouro ?? string.Empty,
                Numero = filial.Endereco?.Numero ?? string.Empty,
                Complemento = filial.Endereco?.Complemento,
                Bairro = filial.Endereco?.Bairro ?? string.Empty,
                Estado = filial.Endereco?.Estado ?? string.Empty,
                Cep = filial.Endereco?.Cep ?? string.Empty,
                TotalVeiculos = totalVeiculos,
                VeiculosDisponiveis = veiculosDisponiveis,
                TotalLocacoesMes = totalLocacoesMes

            };
        }

        public static FilialResumoDto ToResumoDto(this Filial filial,
            int totalVeiculos = 0,
            int veiculosDisponiveis = 0)
        {
            if (filial == null) return null;

            return new FilialResumoDto
            {
                IdFilial = filial.IdFilial,
                Nome = filial.Nome,
                Cidade = filial.Cidade,
                Estado = filial.Endereco?.Estado ?? string.Empty,
                Ativo = filial.Ativo,
                TotalVeiculos = totalVeiculos,
                VeiculosDisponiveis = veiculosDisponiveis
            };
        }

        public static Filial ToEntity(this CriarFilialDto dto)
        {
            return new Filial
            {
                Nome = dto.Nome.Trim(),
                Cidade = dto.Cidade.Trim(),
                Ativo = dto.Ativo,
                //Endereco = new Endereco
                //{
                //    Logradouro = dto.Logradouro.Trim(),
                //    Numero = dto.Numero.Trim(),
                //    Complemento = dto.Complemento?.Trim(),
                //    Bairro = dto.Bairro.Trim(),
                //    Cidade = dto.Cidade.Trim(),
                //    Estado = dto.Estado.Trim().ToUpper(),
                //    Cep = dto.Cep.Trim()
                //}
            };
        }

        public static void AtualizarDto(this Filial filial, AtualizarFilialDto dto)
        {
            if (!string.IsNullOrWhiteSpace(dto.Nome))
                filial.Nome = dto.Nome.Trim();

            if (!string.IsNullOrWhiteSpace(dto.Cidade))
                filial.Cidade = dto.Cidade.Trim();

            if (dto.Ativo.HasValue)
                filial.Ativo = dto.Ativo.Value;

            // Atualizar endereço se fornecido
            if (filial.Endereco != null)
            {
                if (!string.IsNullOrWhiteSpace(dto.Logradouro))
                    filial.Endereco.Logradouro = dto.Logradouro.Trim();

                if (!string.IsNullOrWhiteSpace(dto.Numero))
                    filial.Endereco.Numero = dto.Numero.Trim();

                if (!string.IsNullOrWhiteSpace(dto.Cidade))
                    filial.Endereco.Cidade = dto.Cidade.Trim();

                if (dto.Complemento != null)
                    filial.Endereco.Complemento = dto.Complemento.Trim();

                if (!string.IsNullOrWhiteSpace(dto.Bairro))
                    filial.Endereco.Bairro = dto.Bairro.Trim();

                if (!string.IsNullOrWhiteSpace(dto.Estado))
                    filial.Endereco.Estado = dto.Estado.Trim().ToUpper();

                if (!string.IsNullOrWhiteSpace(dto.Cep))
                    filial.Endereco.Cep = dto.Cep.Trim();
            }
        }
    }
}