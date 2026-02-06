using Microsoft.AspNetCore.Http;

namespace Locadora_Auto.Application.Configuration.Ultils.ValidadorArquivoServices
{
    public interface IValidadorArquivoService
    {
        bool ValidarListaArquivos(List<IFormFile> documentos);
    }
}
