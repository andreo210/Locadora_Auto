using Locadora_Auto.Application.Models.Dto;
using System.Linq.Expressions;

namespace Locadora_Auto.Application.Services.ClienteServices
{
    
        /// <summary>
        /// Interface para serviços relacionados a Clientes
        /// </summary>
        public interface IClienteService
        {
         #region Operações de Consulta
            Task<ClienteDto?> ObterPorIdAsync(int id, CancellationToken ct = default);
            Task<ClienteDto?> ObterPorCpfAsync(string cpf, CancellationToken ct = default);
            Task<IReadOnlyList<ClienteDto>> ObterTodosAsync(CancellationToken ct = default);
            Task<IReadOnlyList<ClienteDto>> ObterAtivosAsync(CancellationToken ct = default);
            Task<IReadOnlyList<ClienteDto>> ObterPorNomeAsync(string nome, CancellationToken ct = default);
            Task<IReadOnlyList<ClienteDto>> ObterPorEmailAsync(string email, CancellationToken ct = default);
            Task<bool> ExisteClienteAsync(string cpf, CancellationToken ct = default);
            Task<int> ContarClientesAtivosAsync(CancellationToken ct = default);
            Task<IReadOnlyList<ClienteDto>> ObterPaginadoAsync(int pagina, int tamanhoPagina, CancellationToken ct = default);

        // Métodos genéricos para consultas complexas
        //Task<IReadOnlyList<ClienteDto>> ObterComFiltroAsync(
        //    Expression<Func<ClienteDto, bool>>? filtro = null,
        //    Func<IQueryable<ClienteDto>, IOrderedQueryable<ClienteDto>>? ordenarPor = null,
        //    CancellationToken ct = default);
        Task<List<ClienteDto>> ObterSolicitacoesComFiltroAsync(
           bool? status = null,
           string? cpf = null,
           string? nome = null,
           string? email = null
         );
        #endregion

        #region Operações de CRUD
        Task<ClienteDto> CriarClienteAsync(CriarClienteDto clienteDto, CancellationToken ct = default);
            Task<bool> AtualizarClienteAsync(int id, AtualizarClienteDto clienteDto, CancellationToken ct = default);
            Task<bool> ExcluirClienteAsync(int id, CancellationToken ct = default);
            Task<bool> AtivarClienteAsync(int id, CancellationToken ct = default);
            Task<bool> DesativarClienteAsync(int id, CancellationToken ct = default);
            #endregion

            #region Validações e Regras de Negócio
            //Task<bool> ValidarClienteParaLocacaoAsync(int id, CancellationToken ct = default);
            //Task<bool> ClientePossuiLocacoesAtivasAsync(int id, CancellationToken ct = default);
            //Task<bool> ClienteEstaEmDiaComPagamentosAsync(int id, CancellationToken ct = default);
            #endregion

            #region Métodos Auxiliares
            //Task<bool> VerificarDisponibilidadeEmailAsync(string email, int? idExcluir = null, CancellationToken ct = default);
            //Task<bool> VerificarDisponibilidadeCpfAsync(string cpf, int? idExcluir = null, CancellationToken ct = default);
            #endregion
        }

    }
