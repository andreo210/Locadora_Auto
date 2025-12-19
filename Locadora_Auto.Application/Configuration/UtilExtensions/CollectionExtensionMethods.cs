using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Locadora_Auto.Application.Configuration.UtilExtensions
{
    /// <summary>
    /// Métodos de extensão para facilitar manipulações inteligentes de coleções.
    /// </summary>
    public static class CollectionExtensionMethods
    {
        /// <summary>
        /// Filtra uma coleção removendo nulos e duplicados.
        /// </summary>
        public static IEnumerable<T> FiltrarLimpos<T>(this IEnumerable<T> source)
        {
            return source?.Where(x => x != null).Distinct() ?? Enumerable.Empty<T>();
        }

        /// <summary>
        /// Filtra elementos que contêm uma palavra-chave (case-insensitive).
        /// </summary>
        public static IEnumerable<string> FiltrarPorTexto(this IEnumerable<string> source, string termo)
        {
            if (string.IsNullOrWhiteSpace(termo)) return source;
            return source?.Where(x => x != null && x.Contains(termo, StringComparison.OrdinalIgnoreCase)) ?? Enumerable.Empty<string>();
        }

        /// <summary>
        /// Agrupa elementos por uma chave e retorna um dicionário.
        /// </summary>
        public static Dictionary<TKey, List<T>> AgruparPor<T, TKey>(this IEnumerable<T> source, Func<T, TKey> chaveSelector)
        {
            return source?
                .Where(x => x != null)
                .GroupBy(chaveSelector)
                .ToDictionary(g => g.Key, g => g.ToList()) ?? new Dictionary<TKey, List<T>>();
        }

        /// <summary>
        /// Ordena uma coleção por uma propriedade ascendente ou descendente.
        /// </summary>
        public static IEnumerable<T> OrdenarPor<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector, bool descendente = false)
        {
            if (source == null) return Enumerable.Empty<T>();
            return descendente
                ? source.OrderByDescending(keySelector)
                : source.OrderBy(keySelector);
        }

        /// <summary>
        /// Retorna os N primeiros elementos distintos da coleção.
        /// </summary>
        public static IEnumerable<T> Top<T>(this IEnumerable<T> source, int quantidade)
        {
            return source?.Where(x => x != null).Distinct().Take(quantidade) ?? Enumerable.Empty<T>();
        }

        /// <summary>
        /// Agrupa e conta ocorrências por chave.
        /// </summary>
        public static Dictionary<TKey, int> ContarPor<T, TKey>(this IEnumerable<T> source, Func<T, TKey> chaveSelector)
        {
            return source?
                .Where(x => x != null)
                .GroupBy(chaveSelector)
                .ToDictionary(g => g.Key, g => g.Count()) ?? new Dictionary<TKey, int>();
        }

        /// <summary>
        /// Filtra elementos com base em uma propriedade de data dentro de um intervalo.
        /// </summary>
        public static IEnumerable<T> FiltrarPorIntervalo<T>(
            this IEnumerable<T> source,
            Func<T, DateTime> seletorData,
            DateTime? inicio,
            DateTime? fim)
        {
            return source?.Where(x =>
                (inicio == null || seletorData(x) >= inicio) &&
                (fim == null || seletorData(x) <= fim)) ?? Enumerable.Empty<T>();
        }
        /// <summary>
        /// Aplica paginação à coleção com metadados.
        /// </summary>
        public static PaginacaoResultado<T> Paginar<T>(
            this IEnumerable<T> source,
            int pagina,
            int tamanhoPagina)
        {
            var totalItens = source?.Count() ?? 0;
            var totalPaginas = (int)Math.Ceiling((double)totalItens / tamanhoPagina);
            var itens = source?.Skip((pagina - 1) * tamanhoPagina).Take(tamanhoPagina) ?? Enumerable.Empty<T>();

            return new PaginacaoResultado<T>
            {
                Itens = itens,
                PaginaAtual = pagina,
                TotalPaginas = totalPaginas,
                TotalItens = totalItens
            };
        }

        /// <summary>
        /// Ordena dinamicamente por nome de propriedade (ascendente ou descendente).
        /// </summary>
        public static IEnumerable<T> OrdenarPorPropriedade<T>(
            this IEnumerable<T> source,
            string nomePropriedade,
            bool descendente = false)
        {
            if (string.IsNullOrWhiteSpace(nomePropriedade) || source == null)
                return source ?? Enumerable.Empty<T>();

            var param = Expression.Parameter(typeof(T), "x");
            var body = Expression.PropertyOrField(param, nomePropriedade);
            var lambda = Expression.Lambda<Func<T, object>>(Expression.Convert(body, typeof(object)), param);

            return descendente ? source.OrderByDescending(lambda.Compile()) : source.OrderBy(lambda.Compile());
        }
    }
    public class PaginacaoResultado<T>
    {
        public IEnumerable<T> Itens { get; set; } = Enumerable.Empty<T>();
        public int PaginaAtual { get; set; }
        public int TotalPaginas { get; set; }
        public int TotalItens { get; set; }
    }

}
