using Locadora_Auto.Application.Configuration.Ultils.NotificadorServices;
using Locadora_Auto.Application.Models;
using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Application.Models.Mappers;
using Locadora_Auto.Application.Services.CategoriaVeiculosServices;
using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Domain.IRepositorio;
using Microsoft.Extensions.Logging;

namespace Locadora_Auto.Application.Services
{
    public class CategoriaVeiculoService : ICategoriaVeiculoService
    {
        private readonly ICategoriaVeiculosRepository _repository;
        private readonly INotificadorService _notificador;
        private readonly ILogger<CategoriaVeiculoService> _logger;

        public CategoriaVeiculoService(
            ICategoriaVeiculosRepository repository,
            INotificadorService notificador,
            ILogger<CategoriaVeiculoService> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _notificador = notificador ?? throw new ArgumentNullException(nameof(notificador));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region Consultas

        public async Task<IReadOnlyList<CategoriaVeiculoDto>> ObterTodosAsync(CancellationToken ct = default)
        {
            var categorias = await _repository.ObterAsync(ordenarPor: q => q.OrderBy(c => c.Nome), ct: ct);

            return categorias.Select(c => c.ToDto()).ToList();
        }

        public async Task<CategoriaVeiculoDto?> ObterPorIdAsync(int id, CancellationToken ct = default)
        {
            var categoria = await _repository.ObterPrimeiroAsync(c => c.Id == id, ct: ct);
            if (categoria == null)
            {
                _notificador.Add("Categoria não encontrada.");
                return null;
            }

            return categoria.ToDto();
        }

        #endregion

        #region CRUD

        public async Task<bool> CriarAsync(CriarCategoriaVeiculoDto dto, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(dto.Nome))
            {
                _notificador.Add("Nome da categoria é obrigatório.");
                return false;
            }

            var existe = await _repository.ExisteAsync(c => c.Nome == dto.Nome.Trim(), ct);

            if (existe)
            {
                _notificador.Add("Já existe uma categoria com esse nome.");
                return false;
            }

            if (dto.ValorDiaria <= 0)
            {
                _notificador.Add("Valor da diária deve ser maior que zero.");
                return false;
            }

            var entidade = CategoriaVeiculo.Criar(dto.Nome,dto.ValorDiaria,dto.LimiteKm.Value,dto.ValorKmExcedente.Value);
            await _repository.InserirSalvarAsync(entidade, ct);

            return true;
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

        #endregion
    }
}
