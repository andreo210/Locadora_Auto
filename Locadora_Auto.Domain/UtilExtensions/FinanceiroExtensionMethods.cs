using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Locadora_Auto.Domain.UtilExtensions
{

    /// <summary>
    /// Métodos de extensão para facilitar cálculos financeiros com valores decimais.
    /// </summary>
    public static class FinanceiroExtensionMethods
    {
        /// <summary>
        /// Aplica juros simples sobre o valor original.
        /// Fórmula: valor + (valor * taxa * tempo)
        /// </summary>
        public static decimal AplicarJurosSimples(this decimal valor, decimal taxaPercentual, int tempoMeses)
        {
            var taxa = taxaPercentual / 100;
            return valor + valor * taxa * tempoMeses;
        }

        /// <summary>
        /// Aplica juros compostos sobre o valor original.
        /// Fórmula: valor * (1 + taxa)^tempo
        /// </summary>
        public static decimal AplicarJurosCompostos(this decimal valor, decimal taxaPercentual, int tempoMeses)
        {
            var taxa = taxaPercentual / 100;
            return valor * (decimal)Math.Pow((double)(1 + taxa), tempoMeses);
        }

        /// <summary>
        /// Calcula o valor de desconto aplicado sobre o valor original.
        /// </summary>
        public static decimal AplicarDesconto(this decimal valor, decimal percentualDesconto)
        {
            var desconto = valor * (percentualDesconto / 100);
            return valor - desconto;
        }

        /// <summary>
        /// Calcula o valor final com acréscimo percentual.
        /// </summary>
        public static decimal AplicarAcrescimo(this decimal valor, decimal percentualAcrescimo)
        {
            var acrescimo = valor * (percentualAcrescimo / 100);
            return valor + acrescimo;
        }

        /// <summary>
        /// Calcula o valor da parcela mensal em um financiamento com juros compostos.
        /// Fórmula: PMT = P * (i * (1 + i)^n) / ((1 + i)^n - 1)
        /// </summary>
        public static decimal CalcularParcelaFinanciamento(this decimal valorPrincipal, decimal taxaPercentualMensal, int quantidadeParcelas)
        {
            var i = taxaPercentualMensal / 100;
            var fator = (decimal)Math.Pow((double)(1 + i), quantidadeParcelas);
            return valorPrincipal * i * fator / (fator - 1);
        }
    }

}
