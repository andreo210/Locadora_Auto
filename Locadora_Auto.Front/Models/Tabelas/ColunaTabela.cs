using Microsoft.AspNetCore.Components;

namespace Locadora_Auto.Front.Models.Tabelas
{
    public class ColunaTabela<TItem>
    {
        public string Titulo { get; set; } = string.Empty;
        public string? Propriedade { get; set; }
        public Func<TItem, object>? Valor { get; set; }
        public Func<TItem, RenderFragment>? Template { get; set; }
        public bool Ordenavel { get; set; } = true;
        public string? Largura { get; set; }
        public string? Alinhamento { get; set; } = "left";
        public string? Formato { get; set; }

        public string ObterValorTexto(TItem item)
        {
            if (Valor != null)
                return Valor(item)?.ToString() ?? "";

            if (!string.IsNullOrEmpty(Propriedade))
            {
                var prop = typeof(TItem).GetProperty(Propriedade);
                if (prop != null)
                {
                    var valor = prop.GetValue(item);
                    return valor?.ToString() ?? "";
                }
            }

            return "";
        }
    }
}
