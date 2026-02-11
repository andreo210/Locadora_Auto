using Locadora_Auto.Domain.Entidades;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using System.Globalization;
using System.Text;

namespace Locadora_Auto.Application.Configuration.Ultils.UploadArquivo
{
    /// <summary>
    /// Serviço responsável por realizar upload e download de arquivos,
    /// com suporte a criptografia AES-256 e GPG, além de arquivos simples.
    /// </summary>
    public class UploadDownloadFileService : IUploadDownloadFileService
    {
        // Pasta onde os arquivos simples (sem criptografia) são armazenados
        private readonly string _caminhoSimples;

        /// <summary>
        /// Construtor que injeta as configurações da aplicação via IConfiguration.
        /// </summary>
        public UploadDownloadFileService(IConfiguration config)
        {
            var path = config["FileStorage:Path"]
                ?? throw new InvalidOperationException("Configuração 'FileStorage:Path' não encontrada.");

            // Define os caminhos com base nas configurações do appsettings.json
            _caminhoSimples = Path.Combine(Directory.GetCurrentDirectory(), path);

            // Garante que os diretórios existem
            Directory.CreateDirectory(_caminhoSimples);
        }

        /// <summary>
        /// Realiza o upload de um arquivo simples, sem criptografia.
        /// </summary>
        public async Task<FotoBase> EnviarArquivoSimplesAsync(IFormFile arquivo)
        {
            try
            {
                if (arquivo.Length == 0)
                    throw new ArgumentException("Arquivo inválido.");

                // Obtém nome e extensão e nome base
                
                var nomeBase = Path.GetFileNameWithoutExtension(arquivo.FileName);
                var extensao = Path.GetExtension(arquivo.FileName)?.TrimStart('.') ?? "desconhecido";
                var nomeOriginal = $"{Guid.NewGuid()}.{extensao}";
                // Validação de extensões permitidas
                var extensoesPermitidas = new[] { "jpg", "jpeg", "png", "gif", "bmp",  "webp" };
                if (!extensoesPermitidas.Contains(extensao))
                {
                    var extensoesFormatadas = string.Join(", ", extensoesPermitidas.Select(e => $".{e}"));
                    throw new InvalidOperationException(
                        $"Tipo de arquivo não permitido ({extensao}). Extensões aceitas: {extensoesFormatadas}");
                }

                // Define caminhos
                string raiz = _caminhoSimples;
                if (string.IsNullOrWhiteSpace(raiz))
                    throw new InvalidOperationException("Caminho raiz para upload não configurado.");

                string diretorio = Path.Combine(raiz);

                // Cria o diretório, se necessário
                if (!Directory.Exists(diretorio))
                    Directory.CreateDirectory(diretorio);

                // Gera nome único para o arquivo
                string nomeArquivo = GerarNomeUnico(nomeBase, extensao, diretorio, nomeOriginal);
                // Caminho completo do arquivo
                string caminhoCompleto = Path.Combine(diretorio, nomeArquivo);

                // Pode acontecer de ter o arquivo com o mesmo nome, por isso estou usando FileMode.CreateNew
                // Salva o arquivo fisicamente
                using (var stream = new FileStream(caminhoCompleto, FileMode.CreateNew, FileAccess.Write, FileShare.None, bufferSize: 1024 * 64, useAsync: true))
                {
                    await arquivo.CopyToAsync(stream);
                }

                // Retorna a classe Arquivo preenchida
                return new FotoBase 
                { 
                   Diretorio = diretorio,
                   Extensao = extensao,
                   NomeArquivo = nomeArquivo,
                   QuantidadeBytes = arquivo.Length,
                   Raiz = raiz
                };

            }
            catch (ArgumentException ex)
            {
                // Erro de parâmetro inválido
                throw new InvalidOperationException($"Falha ao processar o arquivo: {ex.Message}", ex);
            }
            catch (IOException ex)
            {
                // Erros de leitura/escrita no disco
                throw new IOException($"Erro ao gravar o arquivo no disco. Verifique permissões e espaço disponível: {ex.Message}", ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                // Erros de permissão no sistema de arquivos
                throw new UnauthorizedAccessException($"Permissão negada para salvar o arquivo no diretório especificado:{ex.Message}", ex);
            }
            catch (Exception ex)
            {
                // Qualquer outro erro inesperado
                throw new Exception($"Ocorreu um erro inesperado durante o upload do arquivo: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Realiza o download de um arquivo simples, sem descriptografia.
        /// </summary>
        public byte[] BaixarArquivoSimples(string nomeArquivo, out string tipoConteudo)
        {
            string caminhoCompleto = Path.Combine(_caminhoSimples, nomeArquivo);
            if (!File.Exists(caminhoCompleto))
                throw new FileNotFoundException("Arquivo não encontrado.");

            tipoConteudo = ObterTipoMime(nomeArquivo);
            return File.ReadAllBytes(caminhoCompleto);
        }

        /// <summary>
        /// Detecta o tipo MIME com base na extensão do arquivo.
        /// </summary>
        private static string ObterTipoMime(string nomeArquivo)
        {
            var provider = new FileExtensionContentTypeProvider();
            return provider.TryGetContentType(nomeArquivo, out string tipo)
                ? tipo
                : "application/octet-stream"; // Tipo genérico caso não seja reconhecido
        }

        // Método para gerar nome unico
        private static string GerarNomeUnico(string nomeBase, string extensao, string diretorio, string nomeArquivo)
        {
            nomeBase = RemoverAcentos(nomeBase);
            nomeArquivo = RemoverAcentos(nomeArquivo);
            var contador = 1;

            // Se não existir, usa o nome original
            if (!File.Exists(Path.Combine(diretorio, nomeArquivo)))
                return nomeArquivo;

            // Se existir, adiciona número sequencial
            do
            {
                nomeArquivo = $"{nomeBase} ({contador}).{extensao}";
                contador++;
            } while (File.Exists(Path.Combine(diretorio, nomeArquivo)) && contador <= 1000);

            //return Guid.NewGuid().ToString();
            return nomeArquivo;

        }

        public static string RemoverAcentos(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                throw new ArgumentException("Nome arquivo inválido");

            var normalizedString = texto.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
