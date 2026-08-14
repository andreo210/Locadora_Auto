using Locadora_Auto.Application.Configuration.Ultils.NotificadorServices;
using Locadora_Auto.Application.Models.Consultas;
using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Application.Models.Mappers;
using Locadora_Auto.Domain;
using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Domain.IRepositorio;
using Microsoft.IdentityModel.Tokens;
using System.Linq.Expressions;

namespace Locadora_Auto.Application.Services.SeguroServices
{
    public class SeguroService : ISeguroService
    {
        private readonly ISeguroRepository _seguroRepository;
        private readonly ILocacaoSeguroRepository _locacaoSeguroRepository;
        private readonly INotificadorService _notificador;

        public SeguroService(
            ISeguroRepository seguroRepository,
            ILocacaoSeguroRepository locacaoSeguroRepository,
            INotificadorService notificador)
        {
            _seguroRepository = seguroRepository;
            _locacaoSeguroRepository = locacaoSeguroRepository;
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

        /// <summary>
        /// Colunas que a listagem aceita ordenar. Coluna desconhecida cai no nome.
        /// </summary>
        private static readonly OrdenacaoDeConsulta<Seguro> Ordenacoes =
            OrdenacaoDeConsulta<Seguro>.Padrao(s => s.Nome)
                .Com("nome", s => s.Nome)
                .Com("descricao", s => s.Descricao)
                .Com("valordiaria", s => s.ValorDiaria)
                .Com("franquia", s => s.Franquia)
                .Com("cobertura", s => s.Cobertura)
                .Com("ativo", s => s.Ativo);

        public async Task<PaginatedResult<SeguroDto>> ObterTodosPaginadoAsync(
            ConsultaPaginadaRequest consulta,
            bool? ativo = null,
            CancellationToken ct = default)
        {
            var busca = consulta.TermoNormalizado;

            Expression<Func<Seguro, bool>> filtro = s =>
                (busca == null
                    || s.Nome.ToLower().Contains(busca)
                    || s.Descricao.ToLower().Contains(busca)
                    || s.Cobertura.ToLower().Contains(busca))
                && (ativo == null || s.Ativo == ativo);

            var seguros = await _seguroRepository.ObterPaginadoComFiltroAsync(
                filtro: filtro,
                ordenarPor: Ordenacoes.Montar(consulta),
                pagina: consulta.Pagina,
                itensPorPagina: consulta.ItensPorPagina,
                asNoTracking: true,
                ct: ct);

            return seguros.ParaDto(SeguroMapper.ToDtoList);
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
            var validacao = await ValidadorSeguro(dto, ct: ct);
            if (!validacao) return null;

            var seguro = Seguro.Criar(dto.Nome, dto.Descricao, dto.ValorDiaria, dto.Franquia, dto.Cobertura);

            await _seguroRepository.InserirSalvarAsync(seguro, ct);

            return await ObterPorIdAsync(seguro.IdSeguro, ct);
        }

        /// <summary>
        /// Na atualização o próprio registro é ignorado na checagem de nome duplicado.
        /// </summary>
        private async Task<bool> ValidadorSeguro(CriarOuAtualizarSeguroDto dto, int? idIgnorar = null, CancellationToken ct = default)
        {
            if (dto.Nome.IsNullOrEmpty())
            {
                _notificador.Add("Nome não pode ser nulo ou vazio");
            }
            else
            {
                // no Postgres a comparação é sensível a maiúsculas: normalizar dos dois lados
                var nome = dto.Nome.Trim().ToLower();

                if (await _seguroRepository.ExisteAsync(
                        v => v.Nome.ToLower() == nome && (idIgnorar == null || v.IdSeguro != idIgnorar), ct))
                {
                    _notificador.Add("Seguro já cadastrado");
                }
            }

            if (dto.Descricao.IsNullOrEmpty())
            {
                _notificador.Add("Descrição não pode ser nula ou vazia");
            }

            if (dto.ValorDiaria <= 0)
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
            var validacao = await ValidadorSeguro(dto, idIgnorar: id, ct: ct);
            if (!validacao) return false;

            seguro.Atualizar(dto.Nome, dto.Descricao, dto.ValorDiaria, dto.Franquia, dto.Cobertura);

            await _seguroRepository.SalvarAsync(ct);
            return true;
        }

        public async Task<bool> ExcluirAsync(int id, CancellationToken ct = default)
        {
            var seguro = await _seguroRepository.ObterPrimeiroAsync(v => v.IdSeguro == id, rastreado: true, ct: ct);
            if (seguro == null)
            {
                _notificador.Add("Seguro não encontrado");
                return false;
            }

            // seguro já contratado em alguma locação faz parte do histórico: só pode ser desativado
            if (await _locacaoSeguroRepository.ExisteAsync(ls => ls.IdSeguro == id, ct))
            {
                _notificador.Add("Seguro possui locações vinculadas e não pode ser excluído. Desative-o em vez de excluir.");
                return false;
            }

            await _seguroRepository.ExcluirSalvarAsync(seguro, ct);
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
