using System.Globalization;

namespace Locadora_Auto.Application.Configuration.UtilExtensions
{
    /// <summary>
    /// Métodos de extensão para facilitar operações comuns com valores do tipo double.
    /// </summary>
    public static class DoubleExtensionMethods
    {
        /// <summary>
        /// Arredonda o valor para duas casas decimais.
        /// </summary>
        public static double Arredondar2Casas(this double value)
        {
            return Math.Round(value, 2);
        }

        /// <summary>
        /// Verifica se o valor é positivo.
        /// </summary>
        public static bool EhPositivo(this double value)
        {
            return value > 0;
        }

        /// <summary>
        /// Verifica se o valor é negativo.
        /// </summary>
        public static bool EhNegativo(this double value)
        {
            return value < 0;
        }

        /// <summary>
        /// Retorna o valor absoluto.
        /// </summary>
        public static double ValorAbsoluto(this double value)
        {
            return Math.Abs(value);
        }

        /// <summary>
        /// Converte o valor para o formato de moeda brasileira (R$).
        /// </summary>
        public static string ParaMoedaBrasileira(this double value)
        {
            return value.ToString("C", new CultureInfo("pt-BR"));
        }

        /// <summary>
        /// Verifica se o valor está dentro de um intervalo inclusivo.
        /// </summary>
        public static bool EstaEntre(this double value, double minimo, double maximo)
        {
            return value >= minimo && value <= maximo;
        }

        /// <summary>
        /// Aplica um percentual sobre o valor.
        /// </summary>
        public static double AplicarPercentual(this double value, double percentual)
        {
            return value * (percentual / 100);
        }

        /// <summary>
        /// Adiciona um percentual ao valor original.
        /// </summary>
        public static double AdicionarPercentual(this double value, double percentual)
        {
            return value + value.AplicarPercentual(percentual);
        }

        /// <summary>
        /// Subtrai um percentual do valor original.
        /// </summary>
        public static double SubtrairPercentual(this double value, double percentual)
        {
            return value - value.AplicarPercentual(percentual);
        }

        /// <summary>
        /// Aplica juros compostos sobre o valor original.
        /// Fórmula: valor * (1 + taxa)^tempo
        /// </summary>
        public static double AplicarJurosCompostos(this double valor, double taxaPercentual, int tempoMeses)
        {
            var taxa = taxaPercentual / 100;
            return valor * Math.Pow(1 + taxa, tempoMeses);
        }

        /// <summary>
        /// Calcula o valor da parcela mensal em um financiamento com juros compostos.
        /// Fórmula: PMT = P * (i * (1 + i)^n) / ((1 + i)^n - 1)
        /// </summary>
        public static double CalcularParcelaFinanciamento(this double valorPrincipal, double taxaPercentualMensal, int quantidadeParcelas)
        {
            var i = taxaPercentualMensal / 100;
            var fator = Math.Pow(1 + i, quantidadeParcelas);
            return valorPrincipal * (i * fator) / (fator - 1);
        }
    }

}
