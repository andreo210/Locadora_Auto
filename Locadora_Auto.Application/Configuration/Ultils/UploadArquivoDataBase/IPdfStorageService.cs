using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Locadora_Auto.Application.Configuration.Ultils.UploadArquivoDataBase
{
    public interface IPdfStorageService
    {
        Task<Guid> UploadPdfAsync(IFormFile file);
        Task<byte[]> DownloadPdfAsync(Guid id);
    }
}
