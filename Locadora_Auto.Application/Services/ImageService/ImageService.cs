using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Locadora_Auto.Application.Services.ImageService
{
    public class ImageService : IImageService
    {
        public async Task<byte[]> RedimensionarAsync(byte[] imagem, int? width, int? height, int quality = 80)
        {
            using var ms = new MemoryStream(imagem);
            using var image = await Image.LoadAsync(ms);

            var (novoWidth, novoHeight) = CalcularNovasDimensoes(image.Width, image.Height, width, height);

            image.Mutate(x => x.Resize(novoWidth, novoHeight));

            using var outputMs = new MemoryStream();
            await image.SaveAsync(outputMs, new JpegEncoder { Quality = quality });
            return outputMs.ToArray();
        }

        public async Task<byte[]> RedimensionarAsync(string caminho, int? width, int? height, int quality = 80)
        {
            var bytes = await File.ReadAllBytesAsync(caminho);
            return await RedimensionarAsync(bytes, width, height, quality);
        }

        private (int width, int height) CalcularNovasDimensoes(int originalWidth,int originalHeight, int? targetWidth,int? targetHeight)
        {
            // Se não tem dimensões alvo, retorna original
            if (!targetWidth.HasValue && !targetHeight.HasValue)
                return (originalWidth, originalHeight);

            double scaleFactor;

            if (targetWidth.HasValue && targetHeight.HasValue)
            {
                // Ambos informados: usa a menor proporção
                double scaleWidth = (double)targetWidth.Value / originalWidth;
                double scaleHeight = (double)targetHeight.Value / originalHeight;
                scaleFactor = Math.Min(scaleWidth, scaleHeight);
            }
            else if (targetWidth.HasValue)
            {
                // Apenas largura informada
                scaleFactor = (double)targetWidth.Value / originalWidth;
            }
            else // apenas altura informada
            {
                scaleFactor = (double)targetHeight!.Value / originalHeight;
            }

            // Aplica a proporção
            int novoWidth = (int)(originalWidth * scaleFactor);
            int novoHeight = (int)(originalHeight * scaleFactor);

            return (novoWidth, novoHeight);
        }
    }
}
