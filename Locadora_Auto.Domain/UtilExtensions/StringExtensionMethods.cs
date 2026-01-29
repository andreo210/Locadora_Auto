namespace Locadora_Auto.Domain.UtilExtensions
{
    using System.Globalization;
    using System.Text;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Métodos de extensão para facilitar operações comuns com strings.
    /// </summary>
    public static class StringExtensionMethods
    {
        /// <summary>
        /// Codifica a string em Base64 usando UTF-8.
        /// </summary>
        public static string EncodeBase64(this string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;

            var valueBytes = Encoding.UTF8.GetBytes(value);
            return Convert.ToBase64String(valueBytes);
        }

        /// <summary>
        /// Decodifica uma string codificada em Base64 usando UTF-8.
        /// </summary>
        public static string DecodeBase64(this string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;

            try
            {
                var valueBytes = Convert.FromBase64String(value);
                return Encoding.UTF8.GetString(valueBytes);
            }
            catch (FormatException)
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Remove todos os espaços em branco da string.
        /// </summary>
        public static string RemoverEspacos(this string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : Regex.Replace(value, @"\s+", "");
        }

        /// <summary>
        /// Verifica se a string é um e-mail válido.
        /// </summary>
        public static bool EhEmailValido(this string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;

            return Regex.IsMatch(value,
                @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Converte a primeira letra da string para maiúscula.
        /// </summary>
        public static string CapitalizarPrimeiraLetra(this string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;

            return char.ToUpper(value[0]) + value.Substring(1);
        }

        /// <summary>
        /// Verifica se a string contém apenas números.
        /// </summary>
        public static bool EhNumerica(this string value)
        {
            return !string.IsNullOrWhiteSpace(value) && Regex.IsMatch(value, @"^\d+$");
        }

        /// <summary>
        /// Remove todos os caracteres especiais da string, mantendo apenas letras, números e espaços.
        /// </summary>
        public static string RemoverCaracteresEspeciais(this string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;

            // Remove tudo que não for letra, número ou espaço
            return Regex.Replace(value, @"[^a-zA-Z0-9\s]", "");
        }

        /// <summary>
        /// Remove acentos e diacríticos da string, convertendo para caracteres básicos.
        /// </summary>
        public static string RemoverAcentos(this string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;

            var normalized = value.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();

            foreach (var c in normalized)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }

            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        /// <summary>
        /// Formata a string como CPF (000.000.000-00).
        /// </summary>
        /// <param name="cpf">CPF em formato livre.</param>
        /// <returns>CPF formatado ou original se inválido.</returns>
        public static string FormatarCpf(this string cpf)
        {
            cpf = Regex.Replace(cpf ?? "", @"[^\d]", "");
            return cpf.Length == 11
                ? Convert.ToUInt64(cpf).ToString(@"000\.000\.000\-00")
                : cpf;
        }

        /// <summary>
        /// Formata a string como CNPJ (00.000.000/0000-00).
        /// </summary>
        /// <param name="cnpj">CNPJ em formato livre.</param>
        /// <returns>CNPJ formatado ou original se inválido.</returns>
        public static string FormatarCnpj(this string cnpj)
        {
            cnpj = Regex.Replace(cnpj ?? "", @"[^\d]", "");
            return cnpj.Length == 14
                ? Convert.ToUInt64(cnpj).ToString(@"00\.000\.000\/0000\-00")
                : cnpj;
        }

        /// <summary>
        /// Formata a string como RG (ex: 12.345.678).
        /// </summary>
        /// <param name="rg">RG em formato livre.</param>
        /// <returns>RG formatado ou original se inválido.</returns>
        public static string FormatarRg(this string rg)
        {
            rg = Regex.Replace(rg ?? "", @"[^\d]", "");
            return rg.Length >= 7 && rg.Length <= 9
                ? Regex.Replace(rg, @"(\d{1,2})(\d{3})(\d{3})", "$1.$2.$3")
                : rg;
        }

        /// <summary>
        /// Mascara o CPF, ocultando os primeiros dígitos (ex: ***.456.789-09).
        /// </summary>
        /// <param name="cpf">CPF em formato livre.</param>
        /// <returns>CPF mascarado ou original se inválido.</returns>
        public static string MascararCpf(this string cpf)
        {
            cpf = Regex.Replace(cpf ?? "", @"[^\d]", "");
            return cpf.Length == 11
                ? $"***.{cpf.Substring(3, 3)}.{cpf.Substring(6, 3)}-{cpf.Substring(9, 2)}"
                : cpf;
        }

        /// <summary>
        /// Mascara o CNPJ, ocultando os primeiros dígitos (ex: **.***.***.0001-81).
        /// </summary>
        /// <param name="cnpj">CNPJ em formato livre.</param>
        /// <returns>CNPJ mascarado ou original se inválido.</returns>
        public static string MascararCnpj(this string cnpj)
        {
            cnpj = Regex.Replace(cnpj ?? "", @"[^\d]", "");
            return cnpj.Length == 14
                ? $"**.***.***.{cnpj.Substring(8, 4)}-{cnpj.Substring(12, 2)}"
                : cnpj;
        }

        /// <summary>
        /// Mascara o RG, ocultando os primeiros dígitos (ex: ***.456.678).
        /// </summary>
        /// <param name="rg">RG em formato livre.</param>
        /// <returns>RG mascarado ou original se inválido.</returns>
        public static string MascararRg(this string rg)
        {
            rg = Regex.Replace(rg ?? "", @"[^\d]", "");
            return rg.Length >= 7
                ? $"***.{rg.Substring(rg.Length - 6, 3)}.{rg.Substring(rg.Length - 3)}"
                : rg;
        }
    }

}
