using System.Text.RegularExpressions;

namespace Locadora_Auto.Domain.UtilExtensions
{
    /// <summary>
    /// Métodos de extensão para validar documentos brasileiros: CPF, CNPJ e RG.
    /// </summary>
    public static class DocumentoExtensionMethods
    {
        /// <summary>
        /// Valida se a string é um CPF válido.
        /// </summary>
        public static bool EhCpfValido(this string cpf)
        {
            if (string.IsNullOrWhiteSpace(cpf)) return false;

            if (cpf.Length != 11)
                return false;
            if (cpf.Equals("00000000000") || cpf.Equals("11111111111")
               || cpf.Equals("22222222222") || cpf.Equals("33333333333")
               || cpf.Equals("44444444444") || cpf.Equals("55555555555")
               || cpf.Equals("66666666666") || cpf.Equals("77777777777")
               || cpf.Equals("99999999999") || cpf.Equals("88888888888"))
                return false;

            cpf = Regex.Replace(cpf, @"[^\d]", "");

            if (cpf.Length != 11 || Regex.IsMatch(cpf, @"^(.)\1{10}$")) return false;

            int[] multiplicador1 = { 10, 9, 8, 7, 6, 5, 4, 3, 2 };
            int[] multiplicador2 = { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 };

            string tempCpf = cpf.Substring(0, 9);
            int soma = 0;

            for (int i = 0; i < 9; i++)
                soma += int.Parse(tempCpf[i].ToString()) * multiplicador1[i];

            int resto = soma % 11;
            int digito1 = resto < 2 ? 0 : 11 - resto;

            tempCpf += digito1;
            soma = 0;

            for (int i = 0; i < 10; i++)
                soma += int.Parse(tempCpf[i].ToString()) * multiplicador2[i];

            resto = soma % 11;
            int digito2 = resto < 2 ? 0 : 11 - resto;

            return cpf.EndsWith($"{digito1}{digito2}");
        }

        /// <summary>
        /// Valida se a string é um CNPJ válido.
        /// </summary>
        public static bool EhCnpjValido(this string cnpj)
        {
            if (string.IsNullOrWhiteSpace(cnpj)) return false;

            cnpj = Regex.Replace(cnpj, @"[^\d]", "");

            if (cnpj.Length != 14 || Regex.IsMatch(cnpj, @"^(.)\1{13}$")) return false;

            int[] multiplicador1 = { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
            int[] multiplicador2 = { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

            string tempCnpj = cnpj.Substring(0, 12);
            int soma = 0;

            for (int i = 0; i < 12; i++)
                soma += int.Parse(tempCnpj[i].ToString()) * multiplicador1[i];

            int resto = soma % 11;
            int digito1 = resto < 2 ? 0 : 11 - resto;

            tempCnpj += digito1;
            soma = 0;

            for (int i = 0; i < 13; i++)
                soma += int.Parse(tempCnpj[i].ToString()) * multiplicador2[i];

            resto = soma % 11;
            int digito2 = resto < 2 ? 0 : 11 - resto;

            return cnpj.EndsWith($"{digito1}{digito2}");
        }

        /// <summary>
        /// Valida se a string é um RG válido (formato básico, sem dígito verificador).
        /// </summary>
        public static bool EhRgValido(this string rg)
        {
            if (string.IsNullOrWhiteSpace(rg)) return false;

            rg = Regex.Replace(rg, @"[^\d]", "");

            // RGs geralmente têm entre 7 e 9 dígitos
            return rg.Length >= 7 && rg.Length <= 9;
        }

        /// <summary>
        /// Valida se a string é um número de PIS/PASEP válido.
        /// </summary>
        /// <param name="pis">Número do PIS em formato livre ou com pontuação.</param>
        /// <returns>True se o PIS for válido; caso contrário, false.</returns>
        public static bool EhPisValido(this string pis)
        {
            if (string.IsNullOrWhiteSpace(pis)) return false;

            pis = Regex.Replace(pis, @"[^\d]", "").PadLeft(11, '0');

            if (pis.Length != 11 || Regex.IsMatch(pis, @"^(.)\1{10}$")) return false;

            int[] multiplicadores = { 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
            int soma = 0;

            for (int i = 0; i < 10; i++)
            {
                soma += int.Parse(pis[i].ToString()) * multiplicadores[i];
            }

            int resto = soma % 11;
            int digitoVerificador = resto < 2 ? 0 : 11 - resto;

            return pis.EndsWith(digitoVerificador.ToString());
        }
    }

}
