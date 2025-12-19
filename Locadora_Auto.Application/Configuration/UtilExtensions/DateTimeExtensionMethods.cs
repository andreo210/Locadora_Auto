namespace Locadora_Auto.Application.Configuration.UtilExtensions
{
    /// <summary>
    /// Métodos de extensão para facilitar operações comuns com datas.
    /// </summary>
    public static class DateTimeExtensionMethods
    {
        /// <summary>
        /// Verifica se a data é um dia útil (segunda a sexta, excluindo finais de semana e feriados).
        /// </summary>
        public static bool EhDiaUtil(this DateTime data)
        {
            var feriados = FeriadosNacionais.ObterFeriados(data.Year);
            return data.DayOfWeek != DayOfWeek.Saturday &&
                   data.DayOfWeek != DayOfWeek.Sunday &&
                   !feriados.Contains(data.Date);
        }

        /// <summary>
        /// Adiciona um número de dias úteis à data, ignorando finais de semana e feriados.
        /// </summary>
        public static DateTime AdicionarDiasUteis(this DateTime data, int diasUteis)
        {
            var resultado = data;
            var adicionados = 0;

            while (adicionados < diasUteis)
            {
                resultado = resultado.AddDays(1);
                if (resultado.EhDiaUtil())
                {
                    adicionados++;
                }
            }

            return resultado;
        }

        /// <summary>
        /// Retorna o número de dias úteis entre duas datas, excluindo finais de semana e feriados.
        /// </summary>
        public static int DiasUteisAte(this DateTime dataInicial, DateTime dataFinal)
        {
            int diasUteis = 0;
            var data = dataInicial.Date;

            while (data <= dataFinal.Date)
            {
                if (data.EhDiaUtil()) diasUteis++;
                data = data.AddDays(1);
            }

            return diasUteis;
        }
        /// <summary>
        /// Calcula a idade com base na data de nascimento.
        /// </summary>
        public static int CalcularIdade(this DateTime dataNascimento)
        {
            var hoje = DateTime.Today;
            var idade = hoje.Year - dataNascimento.Year;

            if (dataNascimento.Date > hoje.AddYears(-idade)) idade--;

            return idade;
        }

       


        /// <summary>
        /// Verifica se a data está vencida em relação à data atual.
        /// </summary>
        public static bool EstaVencida(this DateTime data)
        {
            return data.Date < DateTime.Today;
        }

        /// <summary>
        /// Retorna o número de dias corridos entre duas datas.
        /// </summary>
        public static int DiasCorridosAte(this DateTime dataInicial, DateTime dataFinal)
        {
            return (dataFinal.Date - dataInicial.Date).Days;
        }

        

        /// <summary>
        /// Formata a data no padrão brasileiro (dd/MM/yyyy).
        /// </summary>
        public static string ParaFormatoBrasileiro(this DateTime data)
        {
            return data.ToString("dd/MM/yyyy");
        }

        /// <summary>
        /// Formata a data e hora no padrão brasileiro (dd/MM/yyyy HH:mm).
        /// </summary>
        public static string ParaFormatoBrasileiroComHora(this DateTime data)
        {
            return data.ToString("dd/MM/yyyy HH:mm");
        }
    }

    /// <summary>
    /// Utilitário para obter feriados nacionais brasileiros.
    /// </summary>
    public static class FeriadosNacionais
    {
        /// <summary>
        /// Retorna a lista de feriados nacionais brasileiros para o ano especificado.
        /// Inclui feriados fixos e móveis (como Carnaval e Páscoa).
        /// </summary>
        public static List<DateTime> ObterFeriados(int ano)
        {
            var feriados = new List<DateTime>
        {
            new DateTime(ano, 1, 1),   // Confraternização Universal
            new DateTime(ano, 4, 21),  // Tiradentes
            new DateTime(ano, 5, 1),   // Dia do Trabalho
            new DateTime(ano, 9, 7),   // Independência do Brasil
            new DateTime(ano, 10, 12), // Nossa Senhora Aparecida
            new DateTime(ano, 11, 2),  // Finados
            new DateTime(ano, 11, 15), // Proclamação da República
            new DateTime(ano, 12, 25), // Natal
        };

            // Feriados móveis
            var pascoa = CalcularPascoa(ano);
            feriados.Add(pascoa.AddDays(-48)); // Carnaval
            feriados.Add(pascoa.AddDays(-2));  // Sexta-feira Santa
            feriados.Add(pascoa);              // Páscoa
            feriados.Add(pascoa.AddDays(60));  // Corpus Christi

            return feriados;
        }

        /// <summary>
        /// Calcula a data da Páscoa para o ano especificado usando o algoritmo de Gauss.
        /// </summary>
        private static DateTime CalcularPascoa(int ano)
        {
            int a = ano % 19;
            int b = ano / 100;
            int c = ano % 100;
            int d = b / 4;
            int e = b % 4;
            int f = (b + 8) / 25;
            int g = (b - f + 1) / 3;
            int h = (19 * a + b - d - g + 15) % 30;
            int i = c / 4;
            int k = c % 4;
            int l = (32 + 2 * e + 2 * i - h - k) % 7;
            int m = (a + 11 * h + 22 * l) / 451;
            int mes = (h + l - 7 * m + 114) / 31;
            int dia = (h + l - 7 * m + 114) % 31 + 1;

            return new DateTime(ano, mes, dia);
        }
    }

}
