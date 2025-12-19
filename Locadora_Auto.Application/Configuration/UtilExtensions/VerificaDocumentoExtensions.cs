using Locadora_Auto.Application.Models.Dto;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Locadora_Auto.Application.Configuration.UtilExtensions
{
    public static class VerificaDocumentoExtensions
    {
        public static bool ValidarListaArquivos(List<DocumentosDto> documentos, ModelStateDictionary modelState)
        {
            ArgumentNullException.ThrowIfNull(modelState);

            var extensoesPermitidas = new[] { "jpg", "jpeg", "png", "gif", "bmp", "pdf", "webp" };
            var isValid = true;

            if (documentos.Count == 0)
            {
                modelState.AddModelError("Documentos", "Nenhum arquivo foi enviado.");
                return false;
            }

            for (int i = 0; i < documentos.Count; i++)
            {
                var documento = documentos[i];
                var fieldName = $"Documentos[{i}]";

                if (documento.File == null)
                {
                    modelState.AddModelError(fieldName, "Arquivo vazio ou inválido.");
                    isValid = false;
                    continue;
                }
                var arquivo = documento.File;

                if (arquivo.Length == 0)
                {
                    modelState.AddModelError($"{fieldName}.File", "Arquivo vazio ou inválido.");
                    isValid = false;
                    continue;
                }

                var nomeOriginal = Path.GetFileName(arquivo.FileName);
                var nomeBase = Path.GetFileNameWithoutExtension(arquivo.FileName);
                var extensao = Path.GetExtension(arquivo.FileName)?.TrimStart('.').ToLowerInvariant() ?? "desconhecido";

                if (!extensoesPermitidas.Contains(extensao))
                {
                    var extensoesFormatadas = string.Join(", ", extensoesPermitidas.Select(e => $".{e}"));
                    modelState.AddModelError(fieldName,
                        $"Tipo de arquivo '.{extensao}' não permitido. Extensões aceitas: {extensoesFormatadas}");
                    isValid = false;
                }
            }

            return isValid;
        }

        public static void ValidarListaArquivos(List<DocumentosDto> documentos)
        {
            var extensoesPermitidas = new[] { "jpg", "jpeg", "png", "gif", "bmp", "pdf", "webp" };
            var erros = new List<string>();

            if (documentos.Count == 0)
                throw new ArgumentException("Nenhum arquivo foi enviado.");

            for (int i = 0; i < documentos.Count; i++)
            {
                var documento = documentos[i];

                if (documento.File == null)
                {
                    erros.Add($"Documento [{i}]: Arquivo não foi enviado.");
                    continue;
                }

                var arquivo = documento.File;
                if (arquivo.Length == 0)
                {
                    erros.Add($"Documento [{i}]: Arquivo vazio ou inválido.");
                    continue;
                }

                var nomeOriginal = Path.GetFileName(arquivo.FileName);
                var nomeBase = Path.GetFileNameWithoutExtension(arquivo.FileName);
                var extensao = Path.GetExtension(arquivo.FileName)?.TrimStart('.').ToLowerInvariant() ?? "desconhecido";

                if (!extensoesPermitidas.Contains(extensao))
                {
                    var extensoesFormatadas = string.Join(", ", extensoesPermitidas.Select(e => $".{e}"));
                    erros.Add($"Documento [{i}] - '{nomeOriginal}': Tipo de arquivo '.{extensao}' não permitido. Extensões aceitas: {extensoesFormatadas}");
                }
            }

            if (erros.Count != 0)
            {
                var mensagemErro = "Erros de validação nos documentos:\n" + string.Join("\n", erros);
                throw new InvalidOperationException(mensagemErro);
            }
        }
    }
}