using Locadora_Auto.Front.Models.Request.Veiculo;
using Locadora_Auto.Front.Models.Response;

namespace Locadora_Auto.Front.Services.Servicos.Veiculo
{
    public interface IVeiculoService
    {
        Task<VeiculoResponse?> Inserir(CriarVeiculoRequest request);
        Task<PaginatedResponse<VeiculoResponse>> ObterTodos(
            string? termo = null,
            int? idCategoria = null,
            int? idFilial = null,
            int? idStatus = null,
            bool? ativo = null,
            int pagina = 1,
            int itensPorPagina = 10,
            string? ordenarPor = null,
            string? direcao = null,
            CancellationToken ct = default);
        Task<List<VeiculoResponse>> ObterDisponiveis(int? idFilial = null);
        Task<VeiculoResponse?> ObterPorId(int id);
        Task<bool?> Atualizar(int id, EditarVeiculoRequest request);
        Task<bool> Ativar(int id);
        Task<bool> Desativar(int id);

        // manutenção
        Task<List<ManutencaoResponse>> ObterManutencoes(int id);
        Task<bool> IniciarManutencao(int id, IniciarManutencaoRequest request);
        Task<bool> TerminarManutencao(int id, TerminarManutencaoRequest request);
        Task<bool> CancelarManutencao(int id, int idManutencao);
        Task<bool> AtualizarManutencao(int id, AtualizarManutencaoRequest request);
    }
}
