using System.Globalization;
using System.Reflection;
using Microsoft.AspNetCore.Components;

namespace {{RootNamespace}}.Front.Models.Tabelas
{
    /// <summary>
    /// Definição de uma coluna da <c>TabelaGenerica</c>.
    /// Mora no projeto Blazor (e não no de modelos) porque depende de <see cref="RenderFragment"/>.
    /// </summary>
    public class ColunaTabela<TItem>
    {
        private PropertyInfo? _propriedadeCache;
        private bool _propriedadeResolvida;

        /// <summary>Texto do cabeçalho.</summary>
        public string Titulo { get; set; } = string.Empty;

        /// <summary>
        /// Nome da propriedade. É o valor enviado à Api em <c>ordenarPor</c> e o
        /// caminho de leitura quando <see cref="Valor"/> não é informado.
        /// </summary>
        public string? Propriedade { get; set; }

        /// <summary>Como obter o conteúdo da célula. Prefira a este a leitura por reflexão.</summary>
        public Func<TItem, object?>? Valor { get; set; }

        /// <summary>Conteúdo em HTML (badge, link, ícone). Vence <see cref="Valor"/> quando presente.</summary>
        public Func<TItem, RenderFragment>? Template { get; set; }

        public bool Ordenavel { get; set; } = true;

        public string? Largura { get; set; }

        public string? Alinhamento { get; set; } = "left";

        /// <summary>
        /// Formato aplicado quando o valor implementa <see cref="IFormattable"/>
        /// (<c>"C2"</c>, <c>"dd/MM/yyyy"</c>, <c>"N0"</c>...).
        /// </summary>
        public string? Formato { get; set; }

        public string ObterValorTexto(TItem item)
        {
            var valor = Valor is not null ? Valor(item) : LerPorReflexao(item);

            if (valor is null)
                return string.Empty;

            if (!string.IsNullOrEmpty(Formato) && valor is IFormattable formatavel)
                return formatavel.ToString(Formato, CultureInfo.CurrentCulture);

            return valor.ToString() ?? string.Empty;
        }

        // A reflexão é resolvida uma vez por coluna: o caminho quente é o render de cada linha.
        private object? LerPorReflexao(TItem item)
        {
            if (string.IsNullOrEmpty(Propriedade))
                return null;

            if (!_propriedadeResolvida)
            {
                _propriedadeCache = typeof(TItem).GetProperty(Propriedade);
                _propriedadeResolvida = true;
            }

            return _propriedadeCache?.GetValue(item);
        }
    }
}
