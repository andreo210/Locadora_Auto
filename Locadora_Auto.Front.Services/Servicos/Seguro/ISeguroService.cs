using Locadora_Auto.Front.Models.Request.Seguro;
using Locadora_Auto.Front.Models.Response;

namespace Locadora_Auto.Front.Services.Servicos.Seguro
{
    public interface ISeguroService
    {
        Task<SeguroResponse?> Inserir(CriarSeguroRequest request);
        Task<PaginatedResponse<SeguroResponse>> ObterTodos(
            string? termo = null,
            bool? ativo = null,
            int pagina = 1,
            int itensPorPagina = 10,
            string? ordenarPor = null,
            string? direcao = null,
            CancellationToken ct = default);
        Task<List<SeguroResponse>> ObterAtivos();
        Task<SeguroResponse?> ObterPorId(int id);
        Task<bool?> Atualizar(int id, EditarSeguroRequest request);
        Task<bool> Excluir(int id);
        Task<bool> Ativar(int id);
        Task<bool> Desativar(int id);
    }
}
