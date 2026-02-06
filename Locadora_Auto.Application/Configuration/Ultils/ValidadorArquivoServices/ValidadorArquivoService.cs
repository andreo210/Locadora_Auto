using Locadora_Auto.Application.Configuration.Ultils.NotificadorServices;
using Microsoft.AspNetCore.Http;

namespace Locadora_Auto.Application.Configuration.Ultils.ValidadorArquivoServices
{
    public class ValidadorArquivoService : IValidadorArquivoService
    {
        private readonly INotificadorService _notificador;

        public ValidadorArquivoService(INotificadorService notificador)
        {
            _notificador = notificador;
        }

        public bool ValidarListaArquivos(List<IFormFile> documentos)
        {
            var extensoesPermitidas = new[] { "jpg", "jpeg", "png", "gif", "bmp", "webp" };

            if (documentos == null || documentos.Count == 0)
            {
                _notificador.Add("Nenhum arquivo foi enviado.");
                return false;
            }

            for (int i = 0; i < documentos.Count; i++)
            {
                var arquivo = documentos[i];

                if (arquivo == null || arquivo.Length == 0)
                {
                    _notificador.Add($"Documento [{i}] inválido.");
                    continue;
                }

                var extensao = Path.GetExtension(arquivo.FileName)
                    ?.TrimStart('.')
                    .ToLowerInvariant();

                if (!extensoesPermitidas.Contains(extensao))
                {
                    _notificador.Add($"Documento [{i}] tipo '{extensao}' não permitido.");
                }
            }

            return !_notificador.TemNotificacao();
        }
    }
}
