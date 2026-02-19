using Locadora_Auto.Application.Configuration.Ultils.NotificadorServices;
using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Application.Models.Mappers;
using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Domain.IRepositorio;

namespace Locadora_Auto.Application.Services.AdicionaisServices
{
    public class AdicionalService : IAdicionalService
    {
        private readonly IAdicionalRepository _repository;
        private readonly INotificadorService _notificador;

        public AdicionalService(IAdicionalRepository repository, INotificadorService notificador)
        {
            _repository = repository;
            _notificador = notificador;
        }

        #region Consultas

        public async Task<AdicionalDto?> ObterPorIdAsync(int idLocacao, CancellationToken ct = default)
        {
            var adicional = await ObterPorIdRastreado(idLocacao);
            return adicional?.ToDto();
        }

        public async Task<Adicional?> ObterPorIdRastreado(int idAdicional, CancellationToken ct = default)
        {
            return await _repository.ObterPrimeiroAsync(v => v.IdAdicional == idAdicional,rastreado:true, ct: ct);
        }

        public async Task<IReadOnlyList<AdicionalDto>> ObterTodosAsync(CancellationToken ct = default)
        {
            var adicional = await _repository.ObterAsync(ct: ct);
            return adicional.ToDtoList();
        }

        public async Task<IReadOnlyList<AdicionalDto>> ObterSeguroAtivoAsync(CancellationToken ct = default)
        {
            var seguros = await _repository.ObterAsync(filtro: v => v.Ativo==true,ct: ct);
            return seguros.ToDtoList();
        }

        #endregion

        #region CRUD

        public async Task<AdicionalDto?> CriarAsync(CriarAtualizarAdicionalDto dto, CancellationToken ct = default)
        {
            var validacao = await ValidadorAdicional(dto, ct);
            if (!validacao) return null;

            var seguro = Adicional.Criar(dto.Nome,  dto.ValorDiaria);

            var adi = await _repository.InserirSalvarAsync(seguro, ct);

            return adi.ToDto();
        }

        private async Task<bool> ValidadorAdicional(CriarAtualizarAdicionalDto dto, CancellationToken ct = default)
        {
            if (await _repository.ExisteAsync(v => v.Nome == dto.Nome, ct))
            {
                _notificador.Add("Adicionalo já cadastrada");
            }

            if (dto.ValorDiaria < 0)
            {
                _notificador.Add("Valor diaria inválido");
            }
                     
            if (_notificador.TemNotificacao()) return false;
            return true;
        }

        public async Task<bool> AtualizarAsync(int id, CriarAtualizarAdicionalDto dto, CancellationToken ct = default)
        {
            var adicional = await ObterPorIdRastreado(id);
            if (adicional == null)
            {
                _notificador.Add("Seguro não encontrado");
                return false;
            }
            var validacao = await ValidadorAdicional(dto, ct);
            if (!validacao) return false;

            adicional.Atualizar(dto.Nome,  dto.ValorDiaria);

            await _repository.AtualizarSalvarAsync(adicional,ct);
            return true;
        }

        public async Task<bool> AtivarAsync(int id, CancellationToken ct = default)
        {
            var adicional = await ObterPorIdRastreado(id);
            if (adicional == null)
            {
                _notificador.Add("Adicional não encontrado");
                return false;
            }
            if (adicional.Ativo == true)
            {
                _notificador.Add("Adicional ja esta ativo");
                return false;
            }

            adicional.Ativar();
            return await _repository.AtualizarSalvarAsync(adicional, ct);
        }

        public async Task<bool> DesativarAsync(int id, CancellationToken ct = default)
        {
            var adicional = await ObterPorIdRastreado(id);
            if (adicional == null)
            {
                _notificador.Add("Seguro não encontrado");
                return false;
            }
            if (adicional.Ativo == false)
            {
                _notificador.Add("Seguro ja esta inativo");
                return false;
            }

            adicional.Desativar();
            return await _repository.AtualizarSalvarAsync(adicional, ct);
        }

        #endregion
    }
}
