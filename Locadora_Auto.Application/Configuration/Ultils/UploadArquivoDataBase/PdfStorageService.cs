using Locadora_Auto.Application.Configuration.Ultils.UploadArquivoDataBase;
using Locadora_Auto.Application.Configuration.UtilExtensions;
using Microsoft.AspNetCore.Http;
using Modelo.Infra.Services.UtilExtensions;
using System.IO.Compression;

public class PdfStorageService : IPdfStorageService
{
    //private readonly AppDbContext _dbContext;

    //public PdfStorageService(AppDbContext dbContext)
    //{
    //    _dbContext = dbContext;
    //}

    // Upload: converte, compacta e salva
    public async Task<Guid> UploadPdfAsync(IFormFile file)
    {
     
        byte[] compressedBytes = await ConversaoCompressaoBytesExtensions.CompressBytesAsync(file);
        
        var pdfEntity = new PdfDocument
        {
            Id = Guid.NewGuid(),
            FileName = file.FileName,
            CompressedData = compressedBytes,
            UploadDate = DateTime.UtcNow
        };

        return pdfEntity.Id;
    }

    // Download: busca, descompacta e retorna bytes
    public async Task<byte[]> DownloadPdfAsync(Guid id)
    {
       // var pdfEntity = await _dbContext.PdfDocuments
           // .FirstOrDefaultAsync(p => p.Id == id);

        //if (pdfEntity == null)
        //    throw new FileNotFoundException("PDF não encontrado.");

       // byte[] decompressedBytes = DecompressBytes(pdfEntity.CompressedData);
        return null;
    }

    // Compacta com GZip
    private byte[] CompressBytes(byte[] input)
    {
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Optimal))
        {
            gzip.Write(input, 0, input.Length);
        }
        return output.ToArray();
    }

    // Descompacta com GZip
    private byte[] DecompressBytes(byte[] input)
    {
        using var inputStream = new MemoryStream(input);
        using var gzip = new GZipStream(inputStream, CompressionMode.Decompress);
        using var outputStream = new MemoryStream();
        gzip.CopyTo(outputStream);
        return outputStream.ToArray();
    }
}
public class PdfDocument
{
    public Guid Id { get; set; }
    public string FileName { get; set; }
    public byte[] CompressedData { get; set; }
    public DateTime UploadDate { get; set; }
}