using Locadora_Auto.Application.Configuration.Ultils.NotificadorServices;
using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Application.Models.Mappers;
using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Domain.IRepositorio;
using Microsoft.IdentityModel.Tokens;

namespace Locadora_Auto.Application.Services.SeguroServices
{
    public class SeguroService : ISeguroService
    {
        private readonly ISeguroRepository _seguroRepository;
        private readonly INotificadorService _notificador;

        public SeguroService(ISeguroRepository seguroRepository, INotificadorService notificador)
        {
            _seguroRepository = seguroRepository;
            _notificador = notificador;
        }

        #region Consultas

        public async Task<SeguroDto?> ObterPorIdAsync(int idLocacao, CancellationToken ct = default)
        {
            var seguro = await _seguroRepository.ObterPrimeiroAsync(v => v.IdSeguro == idLocacao, ct: ct);

            return seguro?.ToDto();
        }

        public async Task<IReadOnlyList<SeguroDto>> ObterTodosAsync(CancellationToken ct = default)
        {
            var seguros = await _seguroRepository.ObterAsync(ct: ct);
            return seguros.ToDtoList();
        }

        public async Task<IReadOnlyList<SeguroDto>> ObterSeguroAtivoAsync(CancellationToken ct = default)
        {
            var seguros = await _seguroRepository.ObterAsync(filtro: v => v.Ativo==true,ct: ct);
            return seguros.ToDtoList();
        }

        #endregion

        #region CRUD

        public async Task<SeguroDto?> CriarAsync(CriarOuAtualizarSeguroDto dto, CancellationToken ct = default)
        {
            var validacao = await ValidadorSeguro(dto, ct);
            if (!validacao) return null;

            var seguro = Seguro.Criar(dto.Nome, dto.Descricao, dto.ValorDiaria, dto.Franquia, dto.Cobertura);

            await _seguroRepository.InserirSalvarAsync(seguro, ct);

            return await ObterPorIdAsync(seguro.IdSeguro, ct);
        }

        private async Task<bool> ValidadorSeguro(CriarOuAtualizarSeguroDto dto, CancellationToken ct = default)
        {
            if (await _seguroRepository.ExisteAsync(v => v.Nome == dto.Nome, ct))
            {
                _notificador.Add("Seguro já cadastrada");
            }

            if (dto.ValorDiaria < 0)
            {
                _notificador.Add("Valor diaria inválido");
            }
            if (dto.Franquia < 0)
            {
                _notificador.Add("Franquia inválida");
            }

            if (dto.Cobertura.IsNullOrEmpty())
            {
                _notificador.Add("Cobertura não pode ser nula ou vazia");
            }           
            if (_notificador.TemNotificacao()) return false;
            return true;
        }

        public async Task<bool> AtualizarAsync(int id, CriarOuAtualizarSeguroDto dto, CancellationToken ct = default)
        {
            var seguro = await _seguroRepository.ObterPrimeiroAsync(v => v.IdSeguro == id, rastreado: true, ct: ct);
            if (seguro == null)
            {
                _notificador.Add("Seguro não encontrado");
                return false;
            }
            var validacao = await ValidadorSeguro(dto, ct);
            if (!validacao) return false;

            seguro.Atualizar(dto.Nome, dto.Descricao, dto.ValorDiaria, dto.Franquia, dto.Cobertura);

            await _seguroRepository.SalvarAsync(ct);
            return true;
        }

        public async Task<bool> AtivarAsync(int id, CancellationToken ct = default)
        {
            var seguro = await _seguroRepository.ObterPorIdAsync(id);
            if (seguro == null)
            {
                _notificador.Add("Seguro não encontrado");
                return false;
            }
            if (seguro.Ativo == true)
            {
                _notificador.Add("Seguro ja esta ativo");
                return false;
            }

            seguro.Ativar();
            return await _seguroRepository.AtualizarSalvarAsync(seguro, ct);
        }

        public async Task<bool> DesativarAsync(int id, CancellationToken ct = default)
        {
            var seguro = await _seguroRepository.ObterPorIdAsync(id);
            if (seguro == null)
            {
                _notificador.Add("Seguro não encontrado");
                return false;
            }
            if (seguro.Ativo == false)
            {
                _notificador.Add("Seguro ja esta inativo");
                return false;
            }

            seguro.Desativar();
            return await _seguroRepository.AtualizarSalvarAsync(seguro, ct);
        }

        #endregion
    }
}
