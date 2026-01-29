using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Application.Models.Mappers;
using Locadora_Auto.Application.Services.Notificador;
using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Domain.Entidades.Indentity;
using Locadora_Auto.Domain.IRepositorio;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Locadora_Auto.Application.Services.FuncionarioServices
{
    public class FuncionarioService : IFuncionarioService
    {
        private readonly IFuncionarioRepository _funcionarioRepository;
        private readonly UserManager<User> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificadorService _notificador;

        public FuncionarioService(
            IFuncionarioRepository funcionarioRepository,
            UserManager<User> userManager,
            IUnitOfWork unitOfWork,
            INotificadorService notificador
            )
        {
            _funcionarioRepository = funcionarioRepository ?? throw new ArgumentNullException(nameof(funcionarioRepository));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _notificador = notificador ?? throw new ArgumentNullException(nameof(notificador));
        }

        // #region Operações de Consulta

        public async Task<FuncionarioDto?> ObterPorUsuarioIdAsync(string? id, CancellationToken ct = default)
        {
            var funcionario = await _funcionarioRepository.ObterPrimeiroAsync(c => c!.Usuario!.Id == id, ct: ct, incluir: q => q.Include(c => c.Usuario));

            if (funcionario == null)
                return null;

            // Obter estatísticas
            //var totalLocacoes = await _repositorioGlobal.ContarAsync<Locacao>(
            //    filtro: l => l.FuncionarioId == id, ct: ct);

            //var totalManutencoes = await _repositorioGlobal.ContarAsync<Manutencao>(
            //    filtro: m => m.FuncionarioId == id, ct: ct);

            return funcionario.ToDto();            
        }

        private async Task<Funcionario?> ObterPorIdAsync(int? id, CancellationToken ct = default)
        {
            var funcionario = await _funcionarioRepository.ObterPrimeiroAsync(c => c!.IdFuncionario == id, ct: ct, incluir: q => q.Include(c => c.Usuario),rastreado:true);
            if (funcionario == null)
                return null;
            return funcionario;
        }

        public async Task<FuncionarioDto?> ObterPorMatriculaAsync(string matricula, CancellationToken ct = default)
        {  
            var funcionario = await _funcionarioRepository.ObterPrimeiroAsync(c => c.Matricula == matricula, ct: ct, incluir: q => q.Include(c => c.Usuario));
            if (funcionario == null)
                return null;
            return funcionario.ToDto();            
        }

        public async Task<FuncionarioDto?> ObterPorFuncionarioCpfAsync(string cpf, CancellationToken ct = default)
        {   cpf = LimparCpf(cpf);
            var funcionario = await _funcionarioRepository.ObterPrimeiroAsync(c => c.Usuario!.Cpf == cpf, ct: ct, incluir: q => q.Include(c => c.Usuario));
            if (funcionario == null) return null;
            return funcionario.ToDto();           
        }

        public async Task<IReadOnlyList<FuncionarioDto>> ObterTodosAsync(CancellationToken ct = default)
        {           
            var funcionarios = await _funcionarioRepository.ObterAsync(incluir: q => q.Include(f => f.Usuario),ordenarPor: q => q.OrderBy(f => f.Matricula),ct: ct);
            return funcionarios.ToDtoList();           
        }

        public async Task<bool> ExisteFuncionarioAsync(string matricula, CancellationToken ct = default)
        {            
            return await _funcionarioRepository.ExisteAsync(f => f.Matricula == matricula, ct);           
        }

        public async Task<bool> ExisteFuncionarioPorCpfAsync(string cpf, CancellationToken ct = default)
        {
            var modelo = await ObterPorFuncionarioCpfAsync(cpf, ct);
            return modelo != null;
        }

        public async Task<int> ContarFuncionariosAtivosAsync(CancellationToken ct = default)
        {           
            return await _funcionarioRepository.ContarAsync(f => f.Ativo, ct);            
        }

        //public async Task<IReadOnlyList<FuncionarioDto>> ObterPaginadoAsync(
        //    int pagina, int tamanhoPagina, CancellationToken ct = default)
        //{
        //    try
        //    {
        //        if (pagina < 1) pagina = 1;
        //        if (tamanhoPagina < 1) tamanhoPagina = 10;
        //        if (tamanhoPagina > 100) tamanhoPagina = 100;

        //        var skip = (pagina - 1) * tamanhoPagina;

        //        var funcionarios = await _repositorioGlobal.ObterComFiltroAsync<Funcionario>(
        //            filtro: f => f.Status,
        //            incluir: q => q.Include(f => f.ApplicationUser),
        //            ordenarPor: q => q.OrderBy(f => f.Nome),
        //            asNoTracking: true,
        //            ct: ct);

        //        // Paginação em memória (para este exemplo simples)
        //        // Em produção, use método específico de paginação no repositório
        //        return funcionarios
        //            .Skip(skip)
        //            .Take(tamanhoPagina)
        //            .Select(f => FuncionarioDto.FromEntity(f))
        //            .ToList();
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Erro ao obter funcionários paginados");
        //        throw;
        //    }
        //}

        //public async Task<EstatisticasFuncionariosDto> ObterEstatisticasAsync(CancellationToken ct = default)
        //{
        //    try
        //    {
        //        _logger.LogInformation("Obtendo estatísticas de funcionários");

        //        var todosFuncionarios = await _repositorioGlobal.ObterComFiltroAsync<Funcionario>(
        //            asNoTracking: true, ct: ct);

        //        var funcionariosAtivos = todosFuncionarios.Where(f => f.Status).ToList();

        //        // Distribuição por cargo
        //        var distribuicaoCargos = funcionariosAtivos
        //            .GroupBy(f => f.Cargo)
        //            .ToDictionary(g => g.Key, g => g.Count());

        //        // Novos funcionários no mês
        //        var dataMesPassado = DateTime.UtcNow.AddMonths(-1);
        //        var novosFuncionariosMes = funcionariosAtivos
        //            .Count(f => f.DataAdmissao >= dataMesPassado);

        //        // Funcionários com locações registradas
        //        var funcionariosComLocacoes = await _repositorioGlobal.ObterComFiltroAsync<Locacao>(
        //            filtro: l => l.FuncionarioId != null,
        //            incluir: q => q.Include(l => l.Funcionario),
        //            asNoTracking: true,
        //            ct: ct);

        //        var idsFuncionariosComLocacoes = funcionariosComLocacoes
        //            .Select(l => l.FuncionarioId)
        //            .Distinct()
        //            .Count();

        //        return new EstatisticasFuncionariosDto
        //        {
        //            TotalFuncionarios = todosFuncionarios.Count,
        //            FuncionariosAtivos = funcionariosAtivos.Count,
        //            DistribuicaoCargos = distribuicaoCargos,
        //            SalarioMedio = funcionariosAtivos.Any()
        //                ? funcionariosAtivos.Average(f => f.Salario)
        //                : 0,
        //            NovosFuncionariosMes = novosFuncionariosMes,
        //            FuncionariosComLocacoes = idsFuncionariosComLocacoes
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Erro ao obter estatísticas de funcionários");
        //        throw;
        //    }
        //}

        //public async Task<IReadOnlyList<FuncionarioDesempenhoDto>> ObterDesempenhoFuncionariosAsync(
        //    DateTime dataInicio, DateTime dataFim, CancellationToken ct = default)
        //{
        //    try
        //    {
        //        _logger.LogInformation("Obtendo desempenho de funcionários de {DataInicio} a {DataFim}",
        //            dataInicio, dataFim);

        //        var funcionarios = await _repositorioGlobal.ObterComFiltroAsync<Funcionario>(
        //            filtro: f => f.Status,
        //            asNoTracking: true,
        //            ct: ct);

        //        var resultado = new List<FuncionarioDesempenhoDto>();

        //        foreach (var funcionario in funcionarios)
        //        {
        //            // Locações registradas pelo funcionário no período
        //            var locacoes = await _repositorioGlobal.ObterComFiltroAsync<Locacao>(
        //                filtro: l => l.FuncionarioId == funcionario.Id &&
        //                            l.DataLocacao >= dataInicio &&
        //                            l.DataLocacao <= dataFim,
        //                asNoTracking: true,
        //                ct: ct);

        //            // Manutenções realizadas pelo funcionário no período
        //            var manutencoes = await _repositorioGlobal.ObterComFiltroAsync<Manutencao>(
        //                filtro: m => m.FuncionarioId == funcionario.Id &&
        //                            m.DataInicio >= dataInicio &&
        //                            m.DataInicio <= dataFim,
        //                asNoTracking: true,
        //                ct: ct);

        //            resultado.Add(new FuncionarioDesempenhoDto
        //            {
        //                FuncionarioId = funcionario.Id,
        //                Nome = funcionario.Nome,
        //                Cargo = funcionario.Cargo,
        //                TotalLocacoes = locacoes.Count,
        //                TotalManutencoes = manutencoes.Count,
        //                ValorTotalLocacoes = locacoes.Sum(l => l.ValorTotal),
        //                DiasTrabalhados = 0 // Implementar cálculo baseado em registro de ponto
        //            });
        //        }

        //        return resultado;
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Erro ao obter desempenho de funcionários");
        //        throw;
        //    }
        //}

        public async Task<IReadOnlyList<FuncionarioDto>> ObterComFiltroAsync( 
            bool? ativos = true, 
            string? nome = null,
            string cargo = null,
            CancellationToken ct = default
            )
        {
            Expression<Func<Funcionario, bool>>? filtro = null;
            if (ativos != null || nome != null || cargo != null)
            {
                filtro = s =>( s.Ativo == ativos) &&  (s.Usuario.NomeCompleto == nome) && (s.Cargo == cargo);
            }


            var funcionarios = await _funcionarioRepository.ObterComFiltroAsync(
                filtro: filtro,
                incluir: q => q.Include(f => f.Usuario),
                ordenarPor: q=>q.OrderBy(f => f.Matricula),
                asNoTracking: true,
                asSplitQuery:true,
                ct: ct);

            return funcionarios.ToDtoList();           
        }

        //#endregion

        // #region Operações de CRUD

        public async Task<FuncionarioDto> CriarFuncionarioAsync( CriarFuncionarioDto funcionarioDto, CancellationToken ct = default)
        {               
            // Validações
           var validacao = await ValidarCriacaoFuncionarioAsync(funcionarioDto, ct);
            if(!validacao)
                return null;

            // 1. Criar usuário no Identity (dentro da transação)
            var user = User.Criar(funcionarioDto.Nome, funcionarioDto.Cpf, funcionarioDto.Telefone, funcionarioDto.Email);            
            user.Funcionario = Funcionario.Criar(funcionarioDto.Matricula, funcionarioDto.Cargo);

            var createUserResult = await _userManager.CreateAsync(user, funcionarioDto.Senha);
            if (!createUserResult.Succeeded)
                throw new InvalidOperationException(string.Join(", ", createUserResult.Errors.Select(e => e.Description)));

            // 2. Atribuir role de funcionário
            await _userManager.AddToRoleAsync(user, "Admin");
            await _funcionarioRepository.SalvarAsync();

            // 3. Atribuir permissões específicas
            //foreach (var permissao in funcionarioDto.Permissoes)
            //{
            //    await _userManager.AddClaimAsync(user, new System.Security.Claims.Claim("Permissao", permissao));
            //}

            var func = user.Funcionario.ToDto();
            func.Email = user.Email;
            func.Nome = user.NomeCompleto;
            func.Telefone = user.PhoneNumber;
            func.Cpf = user.Cpf;
            return func;

        }

        public async Task<bool> AtualizarFuncionarioAsync(int id, AtualizarFuncionarioDto funcionarioDto, CancellationToken ct = default)
        {
            var validacao = await ValidarAtualizacaoFuncionarioAsync(funcionarioDto);
            if (!validacao)
                return false;

            // Buscar funcionário existente
            var funcionario = await ObterPorIdAsync(id);
            if (funcionario == null)
            {
                _notificador.Add($"Funcionário com ID {id} não encontrado.");
                return false;
            } 
            //verifica se funcionario esta ativo
            if (!funcionario.Ativo)
            {
                _notificador.Add("Não é possível atualizar um funcionário inativo.");
                return false;
            }                    

            // Buscar usuário associado
            var user = await _userManager.FindByIdAsync(funcionario.Usuario.Id);
            if (user == null)
            {
                _notificador.Add("Usuário associado ao funcionário não encontrado.");
                return false;
            }

            // Validações de campos únicos
            if (!string.IsNullOrWhiteSpace(funcionarioDto.Email) && funcionarioDto.Email != user.Email)
            {
                var emailExists = await _userManager.FindByEmailAsync(funcionarioDto.Email);
                if (emailExists != null && emailExists.Id != user.Id)
                {
                    _notificador.Add($"Email {funcionarioDto.Email} já está em uso.");
                    return false;
                }
            }

            // Atualizar usuário (se email foi alterado)
            if (!string.IsNullOrWhiteSpace(funcionarioDto.Email) && funcionarioDto.Email != user.Email)
            {
                user.Email = funcionarioDto.Email;
                user.UserName = funcionarioDto.Email;
                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                    throw new InvalidOperationException("Erro ao atualizar email do usuário.");
            }

            // Atualizar permissões (se fornecidas)
            if (funcionarioDto.Permissoes != null)
            {
                // Remover claims existentes
                var existingClaims = await _userManager.GetClaimsAsync(user);
                var permissionClaims = existingClaims.Where(c => c.Type == "Permissao");
                foreach (var claim in permissionClaims)
                {
                    await _userManager.RemoveClaimAsync(user, claim);
                }

                // Adicionar novas permissões
                foreach (var permissao in funcionarioDto.Permissoes)
                {
                    await _userManager.AddClaimAsync(user,
                        new System.Security.Claims.Claim("Permissao", permissao));
                }
            } 

            // Atualizar no banco
            var atualizado = await _funcionarioRepository.AtualizarSalvarAsync(funcionario, ct);
            if (!atualizado)
                throw new InvalidOperationException("Falha ao atualizar funcionário.");
            return atualizado;           
        }

        public async Task<bool> ExcluirFuncionarioAsync(int id, CancellationToken ct = default)
        {
            await _unitOfWork.BeginTransactionAsync(ct);
            try
            {
                // Buscar funcionário
                var funcionario = await ObterPorIdAsync(id);
                if (funcionario == null)
                {
                    _notificador.Add($"Funcionário com ID {id} não encontrado.");
                    return false;
                }

                // Verificar se funcionário tem registros ativos
                //var temLocacoesAtivas = await _funcionarioRepository.ExisteAsync(
                //    l => l.IdFuncionario == id 
                //    && (l.Status == StatusLocacao.Ativa || l.Status == StatusLocacao.Atrasada),
                //    ct);

                //if (temLocacoesAtivas)
                //{
                //    throw new InvalidOperationException(
                //        "Funcionário possui locações ativas. Transfira as locações antes de excluir.");
                //}

                // Excluir usuário do Identity
                var user = await _userManager.FindByIdAsync(funcionario.Usuario.Id);
                if (user != null)
                {
                    var deleteResult = await _userManager.DeleteAsync(user);
                    if (!deleteResult.Succeeded)
                        throw new InvalidOperationException("Erro ao excluir usuário do sistema.");
                }

                // Excluir funcionário (cascata excluirá o usuário também)
                await _funcionarioRepository.Excluir(funcionario, ct);
                await _unitOfWork.CommitAsync(ct);
                return true;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync(ct);
                throw;
            }
        }

        public async Task<bool> AtivarFuncionarioAsync(int id, CancellationToken ct = default)
        {
            var funcionario = await ObterPorIdAsync(id, ct);
            if (funcionario == null)
            {
                _notificador.Add($"Funcionário com ID {id} não encontrado.");
                return false;
            }

            if (funcionario.Ativo)
                return true; // Já está ativo

            var user = await _userManager.FindByIdAsync(funcionario!.Usuario!.Id);
            if (user != null)
            {
                user.Ativar();
                await _userManager.UpdateAsync(user);
            }
            funcionario.Ativar();
            var atualizado = await _funcionarioRepository.AtualizarSalvarAsync(funcionario, ct);
            return atualizado;            
        }

        public async Task<bool> DesativarFuncionarioAsync(int id, CancellationToken ct = default)
        {
            var funcionario = await ObterPorIdAsync(id);
            if (funcionario == null)
            {
                _notificador.Add($"Funcionário com ID {id} não encontrado.");
                return false;
            }

            if (!funcionario.Ativo)
                return true; // Já está inativo

            // Verificar se funcionário tem locações ativas
            //var temLocacoesAtivas = await _repositorioGlobal.ExisteAsync<Locacao>(
            //    l => l.FuncionarioId == id &&
            //        (l.Status == StatusLocacao.Ativa || l.Status == StatusLocacao.Atrasada),
            //    ct);

            //if (temLocacoesAtivas)
            //{
            //    throw new InvalidOperationException(
            //        "Funcionário possui locações ativas. Finalize as locações antes de desativar.");
            //}

            // Desativar usuário também
            var user = await _userManager.FindByIdAsync(funcionario.Usuario.Id);
            if (user != null)
            {
                user.Desativar();
                await _userManager.UpdateAsync(user);
            }
            funcionario.Desativar();
            var atualizado = await _funcionarioRepository.AtualizarSalvarAsync(funcionario, ct);
            return atualizado;            
        }

        //#endregion

        //#region Operações Específicas de Funcionário

       

        //public async Task<bool> AtribuirLocacaoAsync(int funcionarioId, int locacaoId, CancellationToken ct = default)
        //{
        //    await _unitOfWork.BeginTransactionAsync(ct);

        //    try
        //    {
        //        _logger.LogInformation("Atribuindo locação {LocacaoId} ao funcionário {FuncionarioId}",
        //            locacaoId, funcionarioId);

        //        // Verificar se funcionário existe e está ativo
        //        var funcionario = await _funcionarioRepository.ObterPorId(funcionarioId);
        //        if (funcionario == null || !funcionario.Status)
        //            throw new InvalidOperationException("Funcionário não encontrado ou inativo.");

        //        // Verificar permissões do funcionário para locação
        //        if (!await ValidarPermissaoLocacaoAsync(funcionarioId, ct))
        //            throw new InvalidOperationException("Funcionário não tem permissão para registrar locações.");

        //        // Buscar locação
        //        var locacao = await _repositorioGlobal.ObterPrimeiroOuDefaultAsync<Locacao>(
        //            filtro: l => l.Id == locacaoId, ct: ct);

        //        if (locacao == null)
        //            throw new KeyNotFoundException($"Locação com ID {locacaoId} não encontrada.");

        //        if (locacao.Status != StatusLocacao.Pendente)
        //            throw new InvalidOperationException("Locação não está pendente de atribuição.");

        //        // Atribuir funcionário à locação
        //        locacao.FuncionarioId = funcionarioId;
        //        locacao.Status = StatusLocacao.Ativa;

        //        await _repositorioGlobal.AtualizarAsync(locacao, ct);
        //        await _unitOfWork.CommitAsync(ct);

        //        _logger.LogInformation("Locação {LocacaoId} atribuída ao funcionário {FuncionarioId} com sucesso",
        //            locacaoId, funcionarioId);

        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        await _unitOfWork.RollbackAsync(ct);
        //        _logger.LogError(ex, "Erro ao atribuir locação {LocacaoId} ao funcionário {FuncionarioId}",
        //            locacaoId, funcionarioId);
        //        throw;
        //    }
        //}

        //public async Task<IReadOnlyList<LocacaoDto>> ObterLocacoesRegistradasAsync(
        //    int funcionarioId, CancellationToken ct = default)
        //{
        //    try
        //    {
        //        _logger.LogInformation("Obtendo locações registradas pelo funcionário ID: {Id}", funcionarioId);

        //        var locacoes = await _repositorioGlobal.ObterComFiltroAsync<Locacao>(
        //            filtro: l => l.FuncionarioId == funcionarioId,
        //            incluir: q => q.Include(l => l.Cliente)
        //                          .Include(l => l.Veiculo),
        //            ordenarPor: q => q.OrderByDescending(l => l.DataLocacao),
        //            asNoTracking: true,
        //            ct: ct);

        //        return locacoes.Select(l => l.ToDto()).ToList();
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Erro ao obter locações registradas pelo funcionário ID: {Id}", funcionarioId);
        //        throw;
        //    }
        //}

        //public async Task<bool> RegistrarManutencaoAsync(
        //    int funcionarioId, int veiculoId, string descricao, CancellationToken ct = default)
        //{
        //    await _unitOfWork.BeginTransactionAsync(ct);

        //    try
        //    {
        //        _logger.LogInformation("Registrando manutenção pelo funcionário {FuncionarioId} no veículo {VeiculoId}",
        //            funcionarioId, veiculoId);

        //        // Verificar permissões do funcionário para manutenção
        //        if (!await ValidarPermissaoManutencaoAsync(funcionarioId, ct))
        //            throw new InvalidOperationException("Funcionário não tem permissão para registrar manutenções.");

        //        // Buscar veículo
        //        var veiculo = await _repositorioGlobal.ObterPrimeiroOuDefaultAsync<Veiculo>(
        //            filtro: v => v.Id == veiculoId, ct: ct);

        //        if (veiculo == null)
        //            throw new KeyNotFoundException($"Veículo com ID {veiculoId} não encontrado.");

        //        // Verificar se veículo não está alugado
        //        var veiculoAlugado = await _repositorioGlobal.ExisteAsync<Locacao>(
        //            l => l.VeiculoId == veiculoId &&
        //                (l.Status == StatusLocacao.Ativa || l.Status == StatusLocacao.Atrasada),
        //            ct);

        //        if (veiculoAlugado)
        //            throw new InvalidOperationException("Veículo está alugado. Não é possível realizar manutenção.");

        //        // Criar registro de manutenção
        //        var manutencao = new Manutencao
        //        {
        //            VeiculoId = veiculoId,
        //            FuncionarioId = funcionarioId,
        //            Descricao = descricao,
        //            Tipo = TipoManutencao.Corretiva,
        //            Status = StatusManutencao.EmAndamento,
        //            DataInicio = DateTime.UtcNow
        //        };

        //        // Atualizar status do veículo
        //        veiculo.Status = StatusVeiculo.Manutencao;
        //        await _repositorioGlobal.AtualizarAsync(veiculo, ct);

        //        await _repositorioGlobal.InserirAsync(manutencao, ct);
        //        await _unitOfWork.CommitAsync(ct);

        //        _logger.LogInformation("Manutenção registrada com sucesso. ID: {ManutencaoId}", manutencao.Id);

        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        await _unitOfWork.RollbackAsync(ct);
        //        _logger.LogError(ex, "Erro ao registrar manutenção pelo funcionário {FuncionarioId}", funcionarioId);
        //        throw;
        //    }
        //}

        //#endregion

        //#region Validações e Regras de Negócio

        //public async Task<bool> ValidarPermissaoLocacaoAsync(int funcionarioId, CancellationToken ct = default)
        //{
        //    try
        //    {
        //        var funcionario = await _funcionarioRepository.ObterPorId(funcionarioId);
        //        if (funcionario == null || !funcionario.Status)
        //            return false;

        //        // Verificar cargo do funcionário
        //        var cargosPermitidos = new[] { "Atendente", "Gerente", "Supervisor" };
        //        return cargosPermitidos.Contains(funcionario.Cargo);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Erro ao validar permissão de locação do funcionário ID: {Id}", funcionarioId);
        //        throw;
        //    }
        //}

        //public async Task<bool> ValidarPermissaoManutencaoAsync(int funcionarioId, CancellationToken ct = default)
        //{
        //    try
        //    {
        //        var funcionario = await _funcionarioRepository.ObterPorId(funcionarioId);
        //        if (funcionario == null || !funcionario.Status)
        //            return false;

        //        // Verificar cargo do funcionário
        //        var cargosPermitidos = new[] { "Mecânico", "Gerente", "Supervisor" };
        //        return cargosPermitidos.Contains(funcionario.Cargo);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Erro ao validar permissão de manutenção do funcionário ID: {Id}", funcionarioId);
        //        throw;
        //    }
        //}



        //#endregion

        //#region Métodos Auxiliares

        public async Task<bool> VerificarDisponibilidadeMatriculaAsync(string matricula, int? idExcluir = null, CancellationToken ct = default)
        {            
            var resultado = await _funcionarioRepository.ObterComFiltroAsync<Funcionario>(
                filtro: f => f.Matricula == matricula && (idExcluir == null || f.IdFuncionario != idExcluir.Value),
                ct: ct
            );
            return !resultado.Any();            
        }



        //#endregion

        //#region Métodos Privados
        private async Task<bool> ValidarCriacaoFuncionarioAsync(CriarFuncionarioDto funcionarioDto, CancellationToken ct)
        {
            // Validar CPF
            if (!ValidarCpf(funcionarioDto.Cpf)) _notificador.Add("CPF inválido.");

            // Validar email
            if (!ValidarEmail(funcionarioDto.Email)) _notificador.Add("Email inválido.");

            // Verificar se matrícula já existe
            if (await ExisteFuncionarioAsync(funcionarioDto.Matricula, ct))_notificador.Add($"Matrícula {funcionarioDto.Matricula} já cadastrada.");

            // Verificar se CPF já existe
            //if (await ExisteFuncionarioPorCpfAsync(funcionarioDto.Cpf, ct))
            //    throw new InvalidOperationException($"CPF {funcionarioDto.Cpf} já cadastrado.");

            // Verificar se email já existe no Identity
            var emailExists = await _userManager.FindByEmailAsync(funcionarioDto.Email);
            if (emailExists != null)
               _notificador.Add($"Email {funcionarioDto.Email} já está em uso.");

            if (_notificador.ObterNotificacoes().Any()) return false;
            return true;
        }

        private async Task<bool> ValidarAtualizacaoFuncionarioAsync(AtualizarFuncionarioDto funcionarioDto)
        {
            // Atualizar campos do funcionário
            if (string.IsNullOrWhiteSpace(funcionarioDto.Nome)) _notificador.Add("nome é obrigatório.");

            if (string.IsNullOrWhiteSpace(funcionarioDto.Telefone)) _notificador.Add("telefone é obrigatório.");

            if (string.IsNullOrWhiteSpace(funcionarioDto.Email)) _notificador.Add("email é obrigatório.");

            if (string.IsNullOrWhiteSpace(funcionarioDto.Cargo)) _notificador.Add("cargo é obrigatório.");

            if (string.IsNullOrWhiteSpace(funcionarioDto.Matricula)) _notificador.Add("matrícula é obrigatório.");

            if(_notificador.ObterNotificacoes().Any()) return false;

            return true;
        }

        private string LimparCpf(string cpf)
        {
            return new string(cpf.Where(char.IsDigit).ToArray());
        }

        private string LimparTelefone(string telefone)
        {
            return new string(telefone.Where(char.IsDigit).ToArray());
        }

        private bool ValidarCpf(string cpf)
        {
            var cpfLimpo = LimparCpf(cpf);
            return cpfLimpo.Length == 11;
        }

        private bool ValidarEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }        
    }
}