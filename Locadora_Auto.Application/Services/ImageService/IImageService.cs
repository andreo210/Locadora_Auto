using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Locadora_Auto.Application.Services.ImageService
{
    public interface IImageService
    {
        Task<byte[]> RedimensionarAsync(byte[] imagem, int? width, int? height, int quality = 80);
        Task<byte[]> RedimensionarAsync(string caminho, int? width, int? height, int quality = 80);
    }
}
