using Locadora_Auto.Application.Models.Dto;

namespace Locadora_Auto.Application.Services.FuncionarioServices
{
    public interface IFuncionarioService
    {
        //#region Operações de Consulta
        //Task<FuncionarioDto?> ObterPorIdAsync(int id, CancellationToken ct = default);
        //Task<FuncionarioDto?> ObterPorMatriculaAsync(string matricula, CancellationToken ct = default);
        //Task<FuncionarioDto?> ObterPorUsuarioIdAsync(string usuarioId, CancellationToken ct = default);
        Task<FuncionarioDto?> ObterPorFuncionarioCpfAsync(string cpf, CancellationToken ct = default);
        Task<IReadOnlyList<FuncionarioDto>> ObterTodosAsync(CancellationToken ct = default);

        //Task<IReadOnlyList<FuncionarioDto>> ObterAtivosAsync(CancellationToken ct = default);
        //Task<IReadOnlyList<FuncionarioDto>> ObterPorNomeAsync(string nome, CancellationToken ct = default);
        //Task<IReadOnlyList<FuncionarioDto>> ObterPorCargoAsync(string cargo, CancellationToken ct = default);
        //Task<IReadOnlyList<FuncionarioDto>> ObterPorDepartamentoAsync(string departamento, CancellationToken ct = default);
        //Task<bool> ExisteFuncionarioAsync(string matricula, CancellationToken ct = default);
        //Task<bool> ExisteFuncionarioPorCpfAsync(string cpf, CancellationToken ct = default);
        //Task<int> ContarFuncionariosAtivosAsync(CancellationToken ct = default);
        //Task<IReadOnlyList<FuncionarioDto>> ObterPaginadoAsync(int pagina, int tamanhoPagina, CancellationToken ct = default);

        //// Métodos para relatórios e estatísticas
        //Task<EstatisticasFuncionariosDto> ObterEstatisticasAsync(CancellationToken ct = default);
        //Task<IReadOnlyList<FuncionarioDesempenhoDto>> ObterDesempenhoFuncionariosAsync(
        //    DateTime dataInicio, DateTime dataFim, CancellationToken ct = default);

        //// Método genérico para consultas complexas
        //Task<IReadOnlyList<FuncionarioDto>> ObterComFiltroAsync(
        //    Expression<Func<Funcionario, bool>>? filtro = null,
        //    Func<IQueryable<Funcionario>, IOrderedQueryable<Funcionario>>? ordenarPor = null,
        //    CancellationToken ct = default);
        //#endregion

       // #region Operações de CRUD
        Task<FuncionarioDto> CriarFuncionarioAsync(CriarFuncionarioDto funcionarioDto, CancellationToken ct = default);
        //Task<bool> AtualizarFuncionarioAsync(int id, AtualizarFuncionarioDto funcionarioDto, CancellationToken ct = default);
        //Task<bool> ExcluirFuncionarioAsync(int id, CancellationToken ct = default);
        //Task<bool> AtivarFuncionarioAsync(int id, CancellationToken ct = default);
        //Task<bool> DesativarFuncionarioAsync(int id, CancellationToken ct = default);
        //Task<bool> AlterarCargoAsync(int id, string novoCargo, CancellationToken ct = default);
        //Task<bool> AtualizarSalarioAsync(int id, decimal novoSalario, CancellationToken ct = default);
        //#endregion

        //#region Operações Específicas de Funcionário
        //Task<bool> RegistrarEntradaAsync(int funcionarioId, CancellationToken ct = default);
        //Task<bool> RegistrarSaidaAsync(int funcionarioId, CancellationToken ct = default);
        //Task<IReadOnlyList<PontoDto>> ObterRegistroPontoAsync(int funcionarioId, DateTime data, CancellationToken ct = default);
        //Task<IReadOnlyList<PontoDto>> ObterRegistroPontoPeriodoAsync(int funcionarioId, DateTime inicio, DateTime fim, CancellationToken ct = default);

        //Task<bool> AtribuirLocacaoAsync(int funcionarioId, int locacaoId, CancellationToken ct = default);
        //Task<bool> FinalizarLocacaoAsync(int funcionarioId, int locacaoId, CancellationToken ct = default);
        //Task<IReadOnlyList<LocacaoDto>> ObterLocacoesRegistradasAsync(int funcionarioId, CancellationToken ct = default);

        //Task<bool> RegistrarManutencaoAsync(int funcionarioId, int veiculoId, string descricao, CancellationToken ct = default);
        //Task<bool> FinalizarManutencaoAsync(int manutencaoId, int funcionarioId, decimal custo, CancellationToken ct = default);
        //#endregion

        //#region Validações e Regras de Negócio
        //Task<bool> ValidarPermissaoLocacaoAsync(int funcionarioId, CancellationToken ct = default);
        //Task<bool> ValidarPermissaoManutencaoAsync(int funcionarioId, CancellationToken ct = default);
        //Task<bool> ValidarPermissaoRelatorioAsync(int funcionarioId, CancellationToken ct = default);
        //Task<bool> FuncionarioEstaDisponivelAsync(int funcionarioId, CancellationToken ct = default);
        //#endregion

        //#region Métodos Auxiliares
        //Task<bool> VerificarDisponibilidadeMatriculaAsync(string matricula, int? idExcluir = null, CancellationToken ct = default);
        //Task<bool> VerificarDisponibilidadeCpfAsync(string cpf, int? idExcluir = null, CancellationToken ct = default);
        //Task<bool> VerificarDisponibilidadeEmailAsync(string email, int? idExcluir = null, CancellationToken ct = default);
        //#endregion
    }

    //#region DTOs
    


    public class FuncionarioResumoDto
    {
        public int Id { get; set; }
        public string Matricula { get; set; } = string.Empty;
        public string Nome { get; set; } = string.Empty;
        public string Cargo { get; set; } = string.Empty;
        public string? Departamento { get; set; }
        public bool Status { get; set; }
    }

    public class PontoDto
    {
        public DateTime DataHora { get; set; }
        public string Tipo { get; set; } = string.Empty; // "Entrada" ou "Saída"
        public string? Observacao { get; set; }
        public string? Localizacao { get; set; }
    }

    public class EstatisticasFuncionariosDto
    {
        public int TotalFuncionarios { get; set; }
        public int FuncionariosAtivos { get; set; }
        public int FuncionariosPorCargo { get; set; }
        public Dictionary<string, int> DistribuicaoCargos { get; set; } = new();
        public decimal SalarioMedio { get; set; }
        public int NovosFuncionariosMes { get; set; }
        public int FuncionariosComLocacoes { get; set; }
    }

    public class FuncionarioDesempenhoDto
    {
        public int FuncionarioId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Cargo { get; set; } = string.Empty;
        public int TotalLocacoes { get; set; }
        public int TotalManutencoes { get; set; }
        public decimal ValorTotalLocacoes { get; set; }
        public decimal MediaAvaliacao { get; set; }
        public int DiasTrabalhados { get; set; }
    }
   // #endregion
}