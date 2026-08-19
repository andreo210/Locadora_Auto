using System.Globalization;
using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Domain.Entidades;

namespace Locadora_Auto.Application.Models.Mappers
{
    public static class FechamentoMapper
    {
        /// <summary>
        /// Cultura fixa nos avisos, pelo mesmo motivo das bases de cálculo: a mesma mensagem não
        /// pode sair "R$ 1.700,00" numa máquina e "R$ 1700.00" na outra.
        /// </summary>
        private static readonly CultureInfo Brasil = CultureInfo.GetCultureInfo("pt-BR");

        public static LinhaFechamentoDto ToDto(this LinhaFechamento linha) => new()
        {
            IdLinhaFechamento = linha.IdLinhaFechamento,
            Tipo = linha.Tipo.ToString(),
            Natureza = linha.Natureza.ToString(),
            BaseCalculo = linha.BaseCalculo,
            Quantidade = linha.Quantidade,
            ValorUnitario = linha.ValorUnitario,
            Total = linha.Total,
            DataLancamento = linha.DataLancamento,
            EhCorrecao = linha.EhCorrecao,
            IdFuncionarioLancamento = linha.IdFuncionarioLancamento,
            Motivo = linha.Motivo
        };

        public static FechamentoLocacaoDto ToDto(this FechamentoLocacao fechamento)
        {
            if (fechamento == null) return null!;

            return new FechamentoLocacaoDto
            {
                IdFechamento = fechamento.IdFechamento,
                IdLocacao = fechamento.IdLocacao,
                DataApuracao = fechamento.DataApuracao,
                IdFuncionarioApuracao = fechamento.IdFuncionarioApuracao,
                DataSelagem = fechamento.DataSelagem,
                Selado = fechamento.Selado,
                TotalDebitos = fechamento.TotalDebitos,
                TotalCreditos = fechamento.TotalCreditos,
                Saldo = fechamento.Saldo,

                // na ordem em que foram lançadas, que é a ordem em que o extrato se lê — as
                // correções da RN-31 no fim, depois da conta original
                Linhas = fechamento.Linhas
                    .OrderBy(l => l.IdLinhaFechamento)
                    .Select(ToDto)
                    .ToList()
            };
        }

        public static ResultadoDaApuracaoDto ToDto(this ResultadoDaApuracao resultado)
        {
            if (resultado == null) return null!;

            return new ResultadoDaApuracaoDto
            {
                Fechamento = resultado.Fechamento.ToDto(),
                CaucaoConsumida = resultado.CaucaoConsumida,
                SaldoResidual = resultado.SaldoResidual,
                CreditoADevolver = resultado.CreditoADevolver,
                JaEstavaApurado = resultado.JaEstavaApurado,
                Avisos = MontarAvisos(resultado)
            };
        }

        /// <summary>
        /// Traduz para o balcão o que a apuração deixou de fora. Cada aviso aqui é dinheiro ou
        /// prazo que alguém precisa acompanhar — não são mensagens de log.
        /// </summary>
        private static List<string> MontarAvisos(ResultadoDaApuracao resultado)
        {
            var avisos = new List<string>();

            // RN-24: o cliente precisa saber que ainda há avaria a resolver, e até quando
            if (resultado.Avarias is { TemPendenciaDePosContrato: true } avarias)
                avisos.Add(string.Format(Brasil,
                    "{0} avaria(s) em análise, somando {1:C}, não entraram nesta conta. Prazo para resolver: {2:dd/MM/yyyy}.",
                    avarias.AvariasEmAnalise, avarias.ValorEmAnalise, avarias.PrazoDoPosContrato));

            // RN-14: receita perdida por cadastro incompleto. Some em silêncio se ninguém avisar
            switch (resultado.Combustivel?.Situacao)
            {
                case SituacaoDoCombustivel.TanqueNaoCadastrado:
                    avisos.Add("Combustível não cobrado: o veículo não tem capacidade de tanque cadastrada.");
                    break;
                case SituacaoDoCombustivel.PrecoNaoConfigurado:
                    avisos.Add(string.Format(Brasil,
                        "Combustível não cobrado: {0} L a repor, mas a filial de devolução está sem preço do litro configurado.",
                        resultado.Combustivel.LitrosFaltantes));
                    break;
            }

            // RN-26: a multa não sumiu, só não entrou aqui — e quem a lançou precisa saber por quê
            foreach (var multa in resultado.MultasRecusadas)
                avisos.Add(string.Format(Brasil,
                    "Multa de {0} no valor de {1:C} não entrou na conta: já está coberta por linha apurada do fechamento.",
                    multa.Tipo, multa.Valor));

            // RN-30: é o que dispara a régua de cobrança
            if (resultado.SaldoResidual > 0)
                avisos.Add(string.Format(Brasil,
                    "Saldo residual de {0:C} a cobrar do cliente.", resultado.SaldoResidual));

            if (resultado.CreditoADevolver > 0)
                avisos.Add(string.Format(Brasil,
                    "Crédito de {0:C} a devolver ao cliente.", resultado.CreditoADevolver));

            return avisos;
        }
    }
}
