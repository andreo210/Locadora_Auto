using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Locadora_Auto.Application.Configuration.UtilExtensions
{
    using System;

    /// <summary>
    /// Métodos de extensão para facilitar operações comuns com valores inteiros.
    /// </summary>
    public static class IntExtensionMethods
    {
        /// <summary>
        /// Verifica se o número é par.
        /// </summary>
        public static bool EhPar(this int value)
        {
            return value % 2 == 0;
        }

        /// <summary>
        /// Verifica se o número é ímpar.
        /// </summary>
        public static bool EhImpar(this int value)
        {
            return value % 2 != 0;
        }

        /// <summary>
        /// Verifica se o número está dentro de um intervalo inclusivo.
        /// </summary>
        public static bool EstaEntre(this int value, int minimo, int maximo)
        {
            return value >= minimo && value <= maximo;
        }

        /// <summary>
        /// Converte o número para moeda brasileira (R$).
        /// </summary>
        public static string ParaMoedaBrasileira(this int value)
        {
            return value.ToString("C", new System.Globalization.CultureInfo("pt-BR"));
        }

        /// <summary>
        /// Retorna o valor absoluto do número.
        /// </summary>
        public static int ValorAbsoluto(this int value)
        {
            return Math.Abs(value);
        }

        /// <summary>
        /// Verifica se o número é positivo.
        /// </summary>
        public static bool EhPositivo(this int value)
        {
            return value > 0;
        }

        /// <summary>
        /// Verifica se o número é negativo.
        /// </summary>
        public static bool EhNegativo(this int value)
        {
            return value < 0;
        }
    }

}
