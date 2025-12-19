using System.Globalization;

namespace Locadora_Auto.Application.Configuration.UtilExtensions
{

    /// <summary>
    /// Métodos de extensão para facilitar operações comuns com valores decimais.
    /// </summary>
    public static class DecimalExtensionMethods
    {
        /// <summary>
        /// Arredonda o valor para duas casas decimais.
        /// </summary>
        public static decimal Arredondar2Casas(this decimal value)
        {
            return Math.Round(value, 2);
        }

        /// <summary>
        /// Verifica se o valor é positivo.
        /// </summary>
        public static bool EhPositivo(this decimal value)
        {
            return value > 0;
        }

        /// <summary>
        /// Verifica se o valor é negativo.
        /// </summary>
        public static bool EhNegativo(this decimal value)
        {
            return value < 0;
        }

        /// <summary>
        /// Retorna o valor absoluto.
        /// </summary>
        public static decimal ValorAbsoluto(this decimal value)
        {
            return Math.Abs(value);
        }

        /// <summary>
        /// Converte o valor para o formato de moeda brasileira (R$).
        /// </summary>
        public static string ParaMoedaBrasileira(this decimal value)
        {
            return value.ToString("C", new CultureInfo("pt-BR"));
        }

        /// <summary>
        /// Verifica se o valor está dentro de um intervalo inclusivo.
        /// </summary>
        public static bool EstaEntre(this decimal value, decimal minimo, decimal maximo)
        {
            return value >= minimo && value <= maximo;
        }

        /// <summary>
        /// Aplica um percentual sobre o valor.
        /// </summary>
        public static decimal AplicarPercentual(this decimal value, decimal percentual)
        {
            return value * (percentual / 100);
        }

        /// <summary>
        /// Adiciona um percentual ao valor original.
        /// </summary>
        public static decimal AdicionarPercentual(this decimal value, decimal percentual)
        {
            return value + value.AplicarPercentual(percentual);
        }

        /// <summary>
        /// Subtrai um percentual do valor original.
        /// </summary>
        public static decimal SubtrairPercentual(this decimal value, decimal percentual)
        {
            return value - value.AplicarPercentual(percentual);
        }
    }

}
