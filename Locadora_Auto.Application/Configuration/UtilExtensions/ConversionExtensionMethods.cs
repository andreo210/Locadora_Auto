using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Locadora_Auto.Application.Configuration.UtilExtensions
{
    /// <summary>
    /// Métodos de extensão para realizar conversões seguras entre tipos.
    /// </summary>
    public static class ConversionExtensionMethods
    {
        /// <summary>
        /// Converte uma string para int de forma segura.
        /// </summary>
        public static int ParaIntSeguro(this string value, int valorPadrao = 0)
        {
            return int.TryParse(value, out var resultado) ? resultado : valorPadrao;
        }

        /// <summary>
        /// Converte uma string para decimal de forma segura.
        /// </summary>
        public static decimal ParaDecimalSeguro(this string value, decimal valorPadrao = 0m)
        {
            return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var resultado)
                ? resultado : valorPadrao;
        }

        /// <summary>
        /// Converte uma string para double de forma segura.
        /// </summary>
        public static double ParaDoubleSeguro(this string value, double valorPadrao = 0.0)
        {
            return double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var resultado)
                ? resultado : valorPadrao;
        }

        /// <summary>
        /// Converte uma string para DateTime de forma segura.
        /// </summary>
        public static DateTime ParaDateTimeSeguro(this string value, DateTime? valorPadrao = null)
        {
            return DateTime.TryParse(value, out var resultado)
                ? resultado : valorPadrao ?? DateTime.MinValue;
        }

        /// <summary>
        /// Converte uma string para bool de forma segura.
        /// </summary>
        public static bool ParaBoolSeguro(this string value, bool valorPadrao = false)
        {
            return bool.TryParse(value, out var resultado) ? resultado : valorPadrao;
        }

        /// <summary>
        /// Converte um objeto genérico para string, retornando vazio se nulo.
        /// </summary>
        public static string ParaStringSeguro(this object value)
        {
            return value?.ToString() ?? string.Empty;
        }

        /// <summary>
        /// Converte um decimal para int com arredondamento.
        /// </summary>
        public static int ParaInt(this decimal value)
        {
            return Convert.ToInt32(Math.Round(value));
        }

        /// <summary>
        /// Converte um double para decimal com precisão.
        /// </summary>
        public static decimal ParaDecimal(this double value)
        {
            return Convert.ToDecimal(value);
        }

        /// <summary>
        /// Converte um int para bool (0 = false, qualquer outro = true).
        /// </summary>
        public static bool ParaBool(this int value)
        {
            return value != 0;
        }
    }

}
