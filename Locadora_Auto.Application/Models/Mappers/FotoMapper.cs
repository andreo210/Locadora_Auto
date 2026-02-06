using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Application.Models.Dto.Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Domain.Entidades;

namespace Locadora_Auto.Application.Models.Mappers
{
    public static class FotosMapper
    {
        public static FotoDto ToDto(this Foto foto)
        {
            if (foto == null) return null;

            return new FotoDto
            {
                Extensao = foto.Extensao,
                Diretorio = foto.Diretorio,
                IdFoto = foto.IdFoto,
                NomeArquivo = foto.NomeArquivo,
                Raiz = foto.Raiz,
                DataUpload = foto.DataUpload,
                QuantidadeBytes = foto.QuantidadeBytes,
                Entidade = foto.Tipo.ToString(),
                IdEntidade = foto.IdEntidade
            };
        }

        public static List<FotoDto> ToDtoList(this IEnumerable<Foto> fotos)
        {
            if (fotos == null) return new List<FotoDto>();
            return fotos.Select(ToDto).ToList();
        }
    }
}