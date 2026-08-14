using System.Globalization;
using System.Text;

namespace {{RootNamespace}}.Front.Services.Servicos
{
    /// <summary>
    /// Monta a query string de uma consulta paginada. Existe para que cada serviço
    /// não repita a mesma sequência de <c>if (x.HasValue) parametros.Add(...)</c> —
    /// é nessa repetição que o <c>EscapeDataString</c> acaba esquecido em um dos casos.
    /// </summary>
    /// <example>
    /// var query = new QueryPaginada(pagina, itensPorPagina, termo, ordenarPor, direcao)
    ///     .Com("idCategoria", idCategoria)
    ///     .Com("ativo", ativo);
    ///
    /// return await _api.GetAsync&lt;PaginatedResponse&lt;VeiculoResponse&gt;&gt;($"{RotaBase}{query}");
    /// </example>
    public sealed class QueryPaginada
    {
        private readonly List<string> _parametros = new();

        public QueryPaginada(
            int pagina = 1,
            int itensPorPagina = 10,
            string? termo = null,
            string? ordenarPor = null,
            string? direcao = null)
        {
            Com("pagina", pagina);
            Com("itensPorPagina", itensPorPagina);
            Com("termo", termo);

            // `direcao` sozinha não significa nada para a Api: só acompanha uma ordenação.
            if (!string.IsNullOrWhiteSpace(ordenarPor))
            {
                Com("ordenarPor", ordenarPor);
                Com("direcao", string.IsNullOrWhiteSpace(direcao) ? "asc" : direcao);
            }
        }

        /// <summary>Acrescenta um filtro. Valor nulo ou vazio é ignorado.</summary>
        public QueryPaginada Com(string nome, object? valor)
        {
            if (valor is null)
                return this;

            var texto = valor switch
            {
                bool b => b ? "true" : "false",
                DateTime data => data.ToString("o", CultureInfo.InvariantCulture),
                IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
                _ => valor.ToString()
            };

            if (string.IsNullOrWhiteSpace(texto))
                return this;

            _parametros.Add($"{Uri.EscapeDataString(nome)}={Uri.EscapeDataString(texto)}");
            return this;
        }

        /// <summary>Devolve <c>"?a=1&amp;b=2"</c> — ou vazio quando não há parâmetro.</summary>
        public override string ToString()
        {
            if (_parametros.Count == 0)
                return string.Empty;

            var sb = new StringBuilder("?");
            sb.AppendJoin('&', _parametros);
            return sb.ToString();
        }
    }
}
