using Locadora_Auto.Application.Configuration.Ultils.NotificadorServices;
using Locadora_Auto.Application.Configuration.Ultils.UploadArquivoServices;
using Locadora_Auto.Application.Models.Dto.Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Application.Models.Mappers;
using Locadora_Auto.Domain;
using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Domain.IRepositorio;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace Locadora_Auto.Application.Services.FilialServices;

public class FilialService : IFilialService
{
    private readonly IFilialRepository _filialRepository;
    private readonly ILogger<FilialService> _logger;
    private readonly INotificadorService _notificador;
    private readonly IUploadDownloadFileService _uploadDownloadFileService;

    public FilialService(
        IUploadDownloadFileService uploadDownloadFileService,
        IFilialRepository filialRepository,
        INotificadorService notificador,
        ILogger<FilialService> logger)
    {
        _filialRepository = filialRepository ?? throw new ArgumentNullException(nameof(filialRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _notificador = notificador ?? throw new ArgumentNullException(nameof(notificador));
        _uploadDownloadFileService = uploadDownloadFileService ?? throw new ArgumentNullException(nameof(uploadDownloadFileService));
    }

    //#region Operações de Consulta

    public async Task<FilialDto?> ObterPorIdAsync(int id, CancellationToken ct = default)
    {
        var filial = await _filialRepository.ObterPrimeiroAsync(
            f => f.IdFilial == id,
            incluir: e => e.Include(c => c.Endereco).Include(f => f.Fotos),
            rastreado: false,
            ct: ct);

        if (filial == null)
        {
            _notificador.Add($"Filial com ID {id} não encontrada.");
            return null;
        }

        return filial.ToDto();
    }

    private async Task<Filial?> ObterPorId(int id, CancellationToken ct = default)
    {
        var filtro = (Expression<Func<Filial, bool>>)(f => f.IdFilial == id);
        var filial = await _filialRepository.ObterPrimeiroAsync(filtro: filtro, incluir: e => e.Include(c => c.Endereco).Include(f=>f.Fotos), true, ct);
        if (filial == null)
            return null;
        return filial;
    }

    
    public async Task<PaginatedResult<FilialDto>> ObterTodosPaginadoAsync(int pagina, int itemPorPagina, string? nome = null, CancellationToken ct = default)
    {
        Expression<Func<Filial, bool>>? filtro = null;

        // No Postgres o LIKE é sensível a maiúsculas: comparar em minúsculas dos dois lados
        if (!string.IsNullOrWhiteSpace(nome))
        {
            var termo = nome.Trim().ToLower();
            filtro = f => f.Nome.ToLower().Contains(termo) || f.Cidade.ToLower().Contains(termo);
        }

        var filiais = await _filialRepository.ObterPaginadoComFiltroAsync(
                filtro: filtro,
                ordenarPor: (Func<IQueryable<Filial>, IOrderedQueryable<Filial>>?)(q => q.OrderBy(c => c.Nome)),
                pagina: pagina,
                itensPorPagina: itemPorPagina,
                asNoTracking: true,
                incluir: q => q.Include(c => c.Endereco).Include(c => c.Fotos),
                ct: ct);

        // Retornar resultado paginado com DTOs
        return new PaginatedResult<FilialDto>
        {
            Items = filiais.Items.Select(c => c.ToDto()).ToList(),
            Total = filiais.Total,
            Pagina = filiais.Pagina,
            TotalPaginas = filiais.TotalPaginas,
            ItensPorPagina = filiais.ItensPorPagina
        };

    }


    public async Task<IReadOnlyList<FilialDto>> ObterTodasAsync(CancellationToken ct = default)
    {        
        var filiais = await _filialRepository.ObterAsync(
            ordenarPor: q => q.OrderBy(f => f.Cidade).ThenBy(f => f.Nome),
            incluir: q => q.Include(f => f.Endereco),
            ct: ct);

        var resultado = new List<FilialDto>();

        foreach (var filial in filiais)
        {
            //var totalVeiculos = await _filialRepository.ContarVeiculosNaFilialAsync(filial.IdFilial, ct);
            //var veiculosDisponiveis = await _filialRepository.ContarVeiculosDisponiveisNaFilialAsync(filial.IdFilial, ct);

            resultado.Add(filial.ToDto());
        }
        return resultado;        
    }

    public async Task<FilialDto> CriarFilialAsync(CriarFilialDto filialDto, CancellationToken ct = default)
    {
        // Validações
        var validacao = await ValidarCriacaoFilialAsync(filialDto, ct);
        if (!validacao)
            return null;

        // Criar entidade
        var filial = Filial.Criar(filialDto.Nome, filialDto.Cidade,filialDto.Endereco.ToEntity());
        await _filialRepository.InserirSalvarAsync(filial, ct);
        return filial.ToDto();       
    }

    public async Task<bool> AtualizarFilialAsync(int id, AtualizarFilialDto filialDto, CancellationToken ct = default)
    {
        var filial = await _filialRepository.ObterPrimeiroAsync(f => f.IdFilial == id, incluir: e => e.Include(c => c.Endereco), rastreado: true, ct: ct);

        if (filial == null)
        {
            _notificador.Add($"Filial com ID {id} não encontrada.");
            return false;
        }

        if (!await ValidarAtualizacaoFilialAsync(id, filialDto, ct))
            return false;

        filial.Atualizar(filialDto.Nome, filialDto.Cidade,filialDto.Endereco.ToEntity());

        var rows = await _filialRepository.SalvarAsync(ct);
        if (rows == 0)
        {
            _notificador.Add("Nenhuma alteração foi realizada.");
            return false;
        }

        return true;
    }


    public async Task<bool> ExcluirFilialAsync(int id, CancellationToken ct = default)
    {
        // Verificar se filial existe
        var filial = await _filialRepository.ObterPrimeiroAsync(filtro: f => f.IdFilial == id, incluir: e => e.Include(c => c.Endereco), true, ct);
        if (filial == null)
        {
            _notificador.Add($"Filial com ID {id} não encontrada.");
            return false;
        }

        // Verificar se filial possui veículos
        //if (await FilialPossuiVeiculosAsync(id, ct))
        //    throw new InvalidOperationException("Não é possível excluir filial com veículos cadastrados.");

        //// Verificar se filial possui locações ativas
        //if (await FilialPossuiLocacoesAtivasAsync(id, ct))
        //    throw new InvalidOperationException("Filial possui locações ativas. Transfira as locações antes de excluir.");

        // Excluir filial
        await _filialRepository.ExcluirSalvarAsync(filial, ct);
        return true;
        
    }

    public async Task<bool> AtivarFilialAsync(int id, CancellationToken ct = default)
    {
        var filial = await ObterPorId(id, ct);
        if (filial == null)
        {
            _notificador.Add($"Filial com ID {id} não encontrada.");
            return false;
        }
        ;

        if (filial.Ativo)
            return true;

        filial.Ativar();
        var atualizado = await _filialRepository.AtualizarSalvarAsync(filial, ct);
        return atualizado;       
    }

    public async Task<bool> RegistarFotoFilialAsync(int id,List<IFormFile> fotos, CancellationToken ct = default)
    {
        var filial = await ObterPorId(id, ct);
        if (filial == null)
        {
            _notificador.Add($"Filial com ID {id} não encontrada.");
            return false;
        };
        var lista = await EnviarFoto(fotos);
        if (lista == null || !lista.Any())
        {
            _notificador.Add("Nenhuma foto foi enviada com sucesso.");
            return false;
        }
        filial.AdicionarFoto(lista);

        var atualizado = await _filialRepository.AtualizarSalvarAsync(filial, ct);
        return atualizado;
    }

    public async Task<bool> ExluirFotoFilialAsync(int id, int idFoto, CancellationToken ct = default)
    {
        var filial = await ObterPorId(id, ct);
        if (filial == null)
        {
            _notificador.Add($"Filial com ID {id} não encontrada.");
            return false;
        }

        var foto = filial.Fotos.FirstOrDefault(f => f.IdFoto == idFoto);
        if (foto == null)
        {
            _notificador.Add("Nenhuma foto foi encontrada.");
            return false;
        }

        filial.RemoverFoto(foto.IdFoto.Value);

        return await _filialRepository.AtualizarSalvarAsync(filial, ct);
    }

    private async Task<List<FotoFilial>> EnviarFoto(List<IFormFile> dto)
    {
        var documentosAnexos = new List<FotoFilial>();
        foreach (var doc in dto)
        {
            var arquivo = await _uploadDownloadFileService.EnviarArquivoSimplesAsync(doc);
            if (arquivo != null)
            {
                var fotoFilial = FotoFilial.Criar(
                     arquivo.NomeArquivo,
                     arquivo.Raiz,
                     arquivo.Diretorio,
                     arquivo.Extensao,
                     arquivo.QuantidadeBytes.Value
                );
                documentosAnexos.Add(fotoFilial);
            }
        }
        return documentosAnexos;
    }

    public async Task<bool> DesativarFilialAsync(int id, CancellationToken ct = default)
    {        
        var filial = await ObterPorId(id, ct);
        if (filial == null)
        {
            _notificador.Add($"Filial com ID {id} não encontrada.");
            return false;
        }

        if (!filial.Ativo)
            return true; // Já está inativa

        // Verificar se filial possui veículos
        //if (await FilialPossuiVeiculosAsync(id, ct))
        //    throw new InvalidOperationException("Filial possui veículos. Transfira os veículos antes de desativar.");

        filial.Desativar();
        var atualizado = await _filialRepository.AtualizarSalvarAsync(filial, ct);

        return atualizado;       
    }

    //#endregion


    //#region Validações

    public async Task<bool> ValidarCriacaoFilialAsync(CriarFilialDto filialDto, CancellationToken ct = default)
    {
        if (filialDto.Endereco == null)
        {
            _notificador.Add("Endereço é obrigatório.");
            return false;
        }

        var existe = await _filialRepository.ExisteAsync(f => f.Nome == filialDto.Nome, ct);

        if (existe)
        {
            _notificador.Add("Já existe uma filial com este nome.");
            return false;
        }

        return true;
    }

    public async Task<bool> ValidarAtualizacaoFilialAsync(int id, AtualizarFilialDto filialDto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(filialDto.Nome))
        {
            _notificador.Add("Nome é obrigatório.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(filialDto.Cidade))
        {
            _notificador.Add("Cidade é obrigatória.");
            return false;
        }

        if (filialDto.Endereco == null)
        {
            _notificador.Add("Endereço é obrigatório.");
            return false;
        }

        // A própria filial não conta como duplicidade de nome
        var nomeExiste = await _filialRepository.ExisteAsync(filtro: s => s.Nome == filialDto.Nome && s.IdFilial != id, ct);
        if (nomeExiste)
        {
            _notificador.Add("Já existe uma filial com este nome.");
            return false;
        }

        return true;
    }

    //#endregion

    //#region Métodos Auxiliares

    //public async Task<int> ContarVeiculosNaFilialAsync(int filialId, CancellationToken ct = default)
    //{
    //    return await _filialRepository.ContarVeiculosNaFilialAsync(filialId, ct);
    //}

    //public async Task<int> ContarVeiculosDisponiveisNaFilialAsync(int filialId, CancellationToken ct = default)
    //{
    //    return await _filialRepository.ContarVeiculosDisponiveisNaFilialAsync(filialId, ct);
    //}

    //#endregion
}