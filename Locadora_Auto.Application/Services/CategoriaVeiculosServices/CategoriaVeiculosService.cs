using Locadora_Auto.Application.Configuration.Ultils.NotificadorServices;
using Locadora_Auto.Application.Configuration.Ultils.UploadArquivoServices;
using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Application.Models.Mappers;
using Locadora_Auto.Domain;
using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Domain.IRepositorio;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace Locadora_Auto.Application.Services.CategoriaVeiculosServices
{
    public class CategoriaVeiculoService : ICategoriaVeiculoService
    {
        private readonly ICategoriaVeiculosRepository _repository;
        private readonly INotificadorService _notificador;
        private readonly IUploadDownloadFileService _uploadDownloadFileService;

        public CategoriaVeiculoService(
            ICategoriaVeiculosRepository repository,
            INotificadorService notificador,
            IUploadDownloadFileService uploadDownloadFileService,
            ILogger<CategoriaVeiculoService> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _notificador = notificador ?? throw new ArgumentNullException(nameof(notificador));
            _uploadDownloadFileService = uploadDownloadFileService ?? throw new ArgumentNullException(nameof(uploadDownloadFileService));
        }

        #region Consultas

        public async Task<PaginatedResult<CategoriaVeiculoDto>> ObterTodosPaginadoAsync(int pagina, int itemPorPagina, CancellationToken ct = default)
        {
            var categorias = await _repository.ObterPaginadoComFiltroAsync(
                    filtro: (Expression<Func<CategoriaVeiculo, bool>>?)null,
                    ordenarPor: (Func<IQueryable<CategoriaVeiculo>, IOrderedQueryable<CategoriaVeiculo>>?)(q => q.OrderBy(c => c.Nome)),
                    pagina: pagina,
                    itensPorPagina: itemPorPagina,
                    asNoTracking: true,
                    incluir: q => q.Include(c => c.Fotos),
                    ct: ct);

            //return categorias.Select(c => c.ToDto()).ToList();

            // Retornar resultado paginado com DTOs
            return new PaginatedResult<CategoriaVeiculoDto>
            {
                Items = categorias.Items.Select(c => c.ToDto()).ToList(),
                Total = categorias.Total,
                Pagina = categorias.Pagina,
                TotalPaginas = categorias.TotalPaginas,
                ItensPorPagina = categorias.ItensPorPagina
            };

        }

        public async Task<CategoriaVeiculoDto?> ObterPorIdAsync(int id, CancellationToken ct = default)
        {
            var categoria = await _repository.ObterPrimeiroAsync(c => c.Id == id, rastreado: true, ct: ct, incluir: q => q.Include(x => x.Fotos));
            if (categoria == null)
            {
                _notificador.Add("Categoria não encontrada.");
                return null;
            }

            return categoria.ToDto();
        }
        
        private async Task<CategoriaVeiculo?> ObterPorId(int id, CancellationToken ct = default)
        {
            var categoria = await _repository.ObterPrimeiroAsync(c => c.Id == id, rastreado: true, ct: ct, incluir:q => q.Include(c => c.Fotos));
            if (categoria == null)
            {
                _notificador.Add("Categoria não encontrada.");
                return null;
            }
            return categoria;
        }

        #endregion

        #region CRUD

        public async Task<CategoriaVeiculoDto> CriarAsync(CriarCategoriaVeiculoDto dto, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(dto.Nome))
            {
                _notificador.Add("Nome da categoria é obrigatório.");
                return null;
            }

            var existe = await _repository.ExisteAsync(c => c.Nome == dto.Nome.Trim(), ct);

            if (existe)
            {
                _notificador.Add("Já existe uma categoria com esse nome.");
                return null;
            }

            if (dto.ValorDiaria <= 0)
            {
                _notificador.Add("Valor da diária deve ser maior que zero.");
                return null;
            }

            var entidade = CategoriaVeiculo.Criar(dto.Nome,dto.ValorDiaria,dto.LimiteKm.Value,dto.ValorKmExcedente.Value);
            await _repository.InserirSalvarAsync(entidade, ct);

            return entidade.ToDto();
        }

        public async Task<bool> AtualizarAsync(int id, AtualizarCategoriaVeiculoDto dto, CancellationToken ct = default)
        {
            var categoria = await _repository.ObterPrimeiroAsync(c => c.Id == id, rastreado: true, ct: ct);
            if (categoria == null)
            {
                _notificador.Add("Categoria não encontrada.");
                return false;
            }

            if (!string.IsNullOrWhiteSpace(dto.Nome))
            {
                var nomeExiste = await _repository.ExisteAsync(c => c.Nome == dto.Nome.Trim() && c.Id != id,ct);
                if (nomeExiste)
                {
                    _notificador.Add("Já existe outra categoria com esse nome.");
                    return false;
                }
            }

            categoria.Atualizar(dto.Nome,dto.ValorDiaria,dto.LimiteKm.Value,dto.ValorKmExcedente.Value);
            var alterado = await _repository.SalvarAsync(ct);

            if (alterado == 0)
            {
                _notificador.Add("Nenhuma alteração foi realizada.");
                return false;
            }

            return true;
        }

        public async Task<bool> ExcluirAsync(int id, CancellationToken ct = default)
        {
            var categoria = await _repository.ObterPrimeiroAsync(c => c.Id == id, ct: ct);

            if (categoria == null)
            {
                _notificador.Add("Categoria não encontrada.");
                return false;
            }

            if (categoria.Veiculos.Any())
            {
                _notificador.Add("Não é possível excluir a categoria pois existem veículos vinculados a ela.");
                return false;
            }

            await _repository.ExcluirSalvarAsync(categoria, ct);
            return true;
        }

        public async Task<bool> RegistarFotoCategoriaAsync(int id, List<IFormFile> fotos, CancellationToken ct = default)
        {
            var categoria = await ObterPorId(id, ct);
            if (categoria == null)
            {
                _notificador.Add($"Categoria com ID {id} não encontrada.");
                return false;
            }
            ;
            var lista = await EnviarFoto(fotos);
            if (lista == null || !lista.Any())
            {
                _notificador.Add("Nenhuma foto foi enviada com sucesso.");
                return false;
            }
            categoria.AdicionarFoto(lista);

            return await _repository.AtualizarSalvarAsync(categoria, ct);
        }

        private async Task<List<FotoCategoriaVeiculo>> EnviarFoto(List<IFormFile> dto)
        {
            var documentosAnexos = new List<FotoCategoriaVeiculo>();
            foreach (var doc in dto)
            {
                var arquivo = await _uploadDownloadFileService.EnviarArquivoSimplesAsync(doc);
                if (arquivo != null)
                {
                    var fotoCategoria = FotoCategoriaVeiculo.Criar(
                         arquivo.NomeArquivo,
                         arquivo.Raiz,
                         arquivo.Diretorio,
                         arquivo.Extensao,
                         arquivo.QuantidadeBytes.Value
                    );
                    documentosAnexos.Add(fotoCategoria);
                }
            }
            return documentosAnexos;
        }

        #endregion
    }
}
