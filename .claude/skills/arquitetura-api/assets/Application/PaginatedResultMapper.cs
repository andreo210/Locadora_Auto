using {{RootNamespace}}.Domain;

namespace {{RootNamespace}}.Application.Models.Mappers
{
    public static class PaginatedResultMapper
    {
        /// <summary>
        /// Troca as entidades da página pelos DTOs, preservando os metadados de paginação.
        ///
        /// Existe para não repetir, em cada listagem, o bloco que copiava Total/Pagina/
        /// TotalPaginas/ItensPorPagina campo a campo — onde esquecer uma linha passava batido no
        /// compilador e aparecia como paginação quebrada na tela.
        ///
        /// O mapeador tem exatamente a forma dos <c>ToDtoList</c> já existentes:
        /// <code>pagina.ParaDto(ReservaMapper.ToDtoList)</code>
        /// </summary>
        public static PaginatedResult<TDto> ParaDto<TEntidade, TDto>(
            this PaginatedResult<TEntidade> pagina,
            Func<IEnumerable<TEntidade>, List<TDto>> mapeador)
        {
            return new PaginatedResult<TDto>
            {
                Items = mapeador(pagina.Items),
                Total = pagina.Total,
                Pagina = pagina.Pagina,
                TotalPaginas = pagina.TotalPaginas,
                ItensPorPagina = pagina.ItensPorPagina
            };
        }
    }
}
