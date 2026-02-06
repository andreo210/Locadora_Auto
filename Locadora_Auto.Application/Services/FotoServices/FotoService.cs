using Locadora_Auto.Application.Configuration.Ultils.NotificadorServices;
using Locadora_Auto.Application.Configuration.Ultils.UploadArquivo;
using Locadora_Auto.Application.Configuration.Ultils.ValidadorArquivoServices;
using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Application.Models.Mappers;
using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Domain.IRepositorio;
using static Locadora_Auto.Domain.Entidades.Foto;

namespace Locadora_Auto.Application.Services.FilialServices;

public class FotoService : IFotoService
{
    private readonly IFotoRepository _fotoRepository;
    private readonly IValidadorArquivoService _validadorArquivoService;
    private readonly IUploadDownloadFileService _uploadDownloadFile;
    private readonly INotificadorService _notificador;

    public FotoService(IFotoRepository fotoRepository, INotificadorService notificador, IUploadDownloadFileService uploadDownloadFile, IValidadorArquivoService validadorArquivoService)
    {
        _fotoRepository = fotoRepository ?? throw new ArgumentNullException(nameof(fotoRepository));
        _notificador = notificador ?? throw new ArgumentNullException(nameof(notificador));
        _uploadDownloadFile = uploadDownloadFile ?? throw new ArgumentNullException(nameof(uploadDownloadFile));
        _validadorArquivoService = validadorArquivoService ?? throw new ArgumentNullException(nameof(validadorArquivoService));
    }


    public async Task<FotoDto?> ObterPorIdAsync(int id, CancellationToken ct = default)
    {
        var foto = await _fotoRepository.ObterPrimeiroAsync(f => f.IdFoto == id, rastreado: false, ct: ct);

        if (foto == null)
        {
            _notificador.Add($"Foto com ID {id} não encontrada.");
            return null;
        }

        return foto.ToDto();
    }



    public async Task<IReadOnlyList<FotoDto>> ObterTodasAsync(CancellationToken ct = default)
    {
        var fotos = await _fotoRepository.ObterAsync(ordenarPor: q => q.OrderBy(f => f.Tipo), ct: ct);
        return fotos.ToDtoList();
    }



    public async Task<List<FotoDto>> UploadFotolAsync(EnviarFotoDto enviarFoto, CancellationToken ct = default)
    {
        // Validações
        var validacao = _validadorArquivoService.ValidarListaArquivos(enviarFoto.file);
        if (validacao == false) return null;        

        var arquivo = await EnviaDocumentos(enviarFoto);
        if (arquivo.Count== 0)
        {
            _notificador.Add("Nenhum arquivo foi enviado ou os arquivos enviados são inválidos.");
            return null;
        }
        await _fotoRepository.InserirSalvarListasAsync(arquivo, ct);
        return arquivo.ToDtoList();
    }

    private async Task<List<Foto>> EnviaDocumentos(EnviarFotoDto enviarFoto)
    {
        var documentosAnexos = new List<Foto>();
        foreach (var doc in enviarFoto.file)
        {
            var arquivo = await _uploadDownloadFile.EnviarArquivoSimplesAsync(doc, (TipoFoto)enviarFoto.IdTipo,enviarFoto.IdEntidade);
            documentosAnexos.Add(arquivo);
        }
        return documentosAnexos;
    }
}




 
 


