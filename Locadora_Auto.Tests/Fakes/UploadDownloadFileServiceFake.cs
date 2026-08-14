using Locadora_Auto.Application.Configuration.Ultils.UploadArquivoServices;
using Locadora_Auto.Domain.Entidades;
using Microsoft.AspNetCore.Http;

namespace Locadora_Auto.Tests.Fakes
{
    /// <summary>
    /// O <c>LocacaoService</c> exige esta dependência no construtor, mas ela só é usada na foto de
    /// vistoria. Como nenhum teste daqui envia arquivo, o fake recusa a chamada em vez de fingir
    /// que gravou: se algum teste encostar nela, é sinal de que o cenário mudou e precisa de um
    /// fake de verdade — falhar alto é melhor do que passar por acidente.
    /// </summary>
    public sealed class UploadDownloadFileServiceFake : IUploadDownloadFileService
    {
        public Task<FotoBase> EnviarArquivoSimplesAsync(IFormFile arquivo)
            => throw new NotSupportedException("Upload de arquivo não é coberto por teste de unidade.");

        public byte[] BaixarArquivoSimples(string nomeArquivo, out string tipoConteudo)
            => throw new NotSupportedException("Download de arquivo não é coberto por teste de unidade.");
    }
}
