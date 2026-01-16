using Locadora_Auto.Application.Configuration.UtilExtensions;
using Locadora_Auto.Application.Models;
using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Application.Models.Mappers;
using Locadora_Auto.Application.Services.Cliente;
using Locadora_Auto.Application.Services.Notificador;
using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Domain.Entidades.Indentity;
using Locadora_Auto.Domain.IRepositorio;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Locadora_Auto.Application.Services
{
    public class ClienteService : IClienteService
    {
        private readonly IClienteRepository _clienteRepository;
        private readonly IUnitOfWork _transaction;
        private readonly ILogger<ClienteService> _logger;
        private readonly UserManager<User> _userManager;
        private readonly INotificador _notificador;

        public ClienteService(
            UserManager<User> userManager,
            IClienteRepository clienteRepository,
            IUnitOfWork transaction,
            INotificador notificador,
            ILogger<ClienteService> logger)
        {
            _clienteRepository = clienteRepository ?? throw new ArgumentNullException(nameof(clienteRepository));
            _transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(logger));
            _notificador = notificador ?? throw new ArgumentNullException(nameof(notificador));
        }

        #region Operações de Consulta

        public async Task<ClienteDto?> ObterPorIdAsync(int id, CancellationToken ct = default)
        {   
            var entidade = await _clienteRepository.ObterPrimeiroAsync(c => c.IdCliente == id, ct: ct, incluir: q => q.Include(c => c.Endereco).Include(c => c.Usuario));
            return entidade?.ToDto();            
        }

        public async Task<ClienteDto?> ObterPorCpfAsync(string cpf, CancellationToken ct = default)
        {

            var cpfLimpo = LimparCpf(cpf);
            var entidade = await _clienteRepository.ObterPrimeiroAsync(c => c.Usuario.Cpf == cpfLimpo, ct: ct, incluir: q => q.Include(c => c.Endereco).Include(c => c.Usuario));
            return entidade?.ToDto();
      
        }

        public async Task<IReadOnlyList<ClienteDto>> ObterTodosAsync(CancellationToken ct = default)
        {
           
            var entidade = await _clienteRepository.ObterAsync(ordenarPor: q => q.OrderBy(c => c.Usuario.NomeCompleto), ct: ct,incluir: q => q.Include(c => c.Endereco).Include(c => c.Usuario));
            if(entidade == null)
            {
                return new List<ClienteDto>();
            }
            return entidade.Select(c => c.ToDto()).ToList();

        }

        public async Task<IReadOnlyList<ClienteDto>> ObterAtivosAsync(CancellationToken ct = default)
        {
            var entidades = await _clienteRepository.ObterAsync( filtro: c => c.Status, ordenarPor: q => q.OrderBy(c => c.Usuario.NomeCompleto), ct: ct);
            return entidades.Select(d => d.ToDto()).ToList();

        }

        public async Task<IReadOnlyList<ClienteDto>> ObterPorNomeAsync(string nome, CancellationToken ct = default)
        {
            
            var entidades = await _clienteRepository.ObterAsync(filtro: c => c.Usuario.NomeCompleto.Contains(nome) && c.Status, ordenarPor: q => q.OrderBy(c => c.Usuario.NomeCompleto), ct: ct);
            return entidades.Select(d=>d.ToDto()).ToList();

        }

        public async Task<IReadOnlyList<ClienteDto>> ObterPorEmailAsync(string email, CancellationToken ct = default)
        {

            var entidades = await _clienteRepository.ObterAsync(filtro: c => c.Usuario.Email == email && c.Status,ordenarPor: q => q.OrderBy(c => c.Usuario.NomeCompleto), ct: ct);
            return entidades.Select(d => d.ToDto()).ToList();

        }

        public async Task<bool> ExisteClienteAsync(string cpf, CancellationToken ct = default)
        {            
            var cpfLimpo = LimparCpf(cpf);
            return await _clienteRepository.ExisteAsync(c => c.Usuario.Cpf == cpfLimpo, ct);            
        }

        public async Task<int> ContarClientesAtivosAsync(CancellationToken ct = default)
        {            
            var model = await ObterTodosAsync();
            return model.Count(c => c.Status);           
        }

        public async Task<IReadOnlyList<ClienteDto>> ObterPaginadoAsync(int pagina,int tamanhoPagina, CancellationToken ct = default)
        {
            
            if (pagina < 1) pagina = 1;
            if (tamanhoPagina < 1) tamanhoPagina = 10;
            if (tamanhoPagina > 100) tamanhoPagina = 100;

            var skip = (pagina - 1) * tamanhoPagina;

            var entidades = await _clienteRepository.ObterPaginadoAsync(
                filtro: c => c.Status,
                skip: skip,
                take: tamanhoPagina,
                ordenarPor: q => q.OrderBy(c => c.Usuario.NomeCompleto),
                ct: ct);
            return entidades.Select(d => d.ToDto()).ToList();
            
        }

        public async Task<List<ClienteDto>> ObterSolicitacoesComFiltroAsync(
           bool? status = null,
           string? cpf = null,
           string? nome = null,
           string? email = null
         )
        {
            Expression<Func<Clientes, bool>>? filtro = null;
            if (status != null || cpf != null || nome != null || email != null)
            {
                filtro = s =>
                    (status == null || s.Status == status) &&
                    (cpf == null || s.Usuario.Cpf == cpf) &&
                    (nome == null || s.Usuario.NomeCompleto == nome) &&
                    (email == null || (s.Usuario.Email == email));
            }
            var entidades = await _clienteRepository.ObterComFiltroAsync(filtro);
            return entidades.Select(d => d.ToDto()).ToList(); 
        }

        //public async Task<IReadOnlyList<ClienteDto>> ObterComFiltroAsync(
        //    Expression<Func<ClienteDto, bool>> filtro = null,
        //    Func<IQueryable<ClienteDto>, IOrderedQueryable<ClienteDto>>? ordenarPor = null,
        //    CancellationToken ct = default)
        //{
        //    try
        //    {
        //        return await _clienteRepository.ObterAsync(filtro: filtro, ordenarPor: ordenarPor, ct: ct);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Erro ao buscar clientes com filtro");
        //        throw;
        //    }
        //}

        #endregion

        #region Operações de CRUD

        //public async Task<ClienteDto> CriarClienteAsync(CriarClienteDto clienteDto, CancellationToken ct = default)
        //{
        //    // ✅ TUDO dentro da transação para evitar race conditions
        //    return await _transaction.ExecuteTransactionAsync(async () =>
        //    {
        //        // Validações DENTRO da transação
        //        await ValidarCriacaoClienteAsync(clienteDto, ct);

        //        // Inserir no banco
        //        var model = await _clienteRepository.InserirAsync(clienteDto.CriarToEntity(), ct);

        //        // Operações adicionais que devem ser atômicas
        //        //await CriarAuditoriaClienteAsync(model.IdCliente, ct);
        //        // await EnviarNotificacaoCadastroAsync(model.Email, ct); // ⚠️ Cuidado com operações externas!

        //        return model.ToDto();
        //    }, ct);
        //}

        public async Task<ClienteDto> CriarClienteAsync(CriarClienteDto clienteDto, CancellationToken ct = default)
        {
            
            // Validações
            await ValidarCriacaoClienteAsync(clienteDto, ct);

            // Inserir no banco
            //var model = await _clienteRepository.InserirAsync(clienteDto.ToEntity(), ct);
            var user = new User
            {
                UserName = clienteDto.Cpf,
                Email = clienteDto.Email,
                NomeCompleto = clienteDto.Nome,
                Cpf = LimparCpf(clienteDto.Cpf),
                PhoneNumber = LimparTelefone(clienteDto.Telefone)                
            };

            var model = new Clientes
            {
                Usuario = user,
                Status = clienteDto.Status,
                NumeroHabilitacao = clienteDto.NumeroHabilitacao,
                ValidadeHabilitacao = clienteDto.ValidadeHabilitacao,
                Endereco = clienteDto.Endereco.ToEntity()
            };
            user.Cliente = model;

            var result = await _userManager.CreateAsync(user, clienteDto.Senha);

            if (!result.Succeeded)
                throw new InvalidOperationException(
                    string.Join(", ", result.Errors.Select(e => e.Description))
                );

            // Operações adicionais que devem ser atômicas
            //await CriarAuditoriaClienteAsync(model.IdCliente, ct);
            // await EnviarNotificacaoCadastroAsync(model.Email, ct); // ⚠️ Cuidado com operações externas!

            var cliente = model.ToDto();
            cliente.Cpf = user.Cpf;
            cliente.Email = user.Email;
            cliente.Telefone= user.PhoneNumber;
            cliente.Nome = user.NomeCompleto;
            return cliente;
           
        }


        public async Task<bool> AtualizarClienteAsync(int id, AtualizarClienteDto clienteDto, CancellationToken ct = default)
        {    
            
            // Buscar cliente existente
            var cliente = await _clienteRepository.ObterPrimeiroAsync(x=>x.IdCliente == id, incluir: q => q.Include(c => c.Endereco).Include(c => c.Usuario), rastreado:true);
            if (string.IsNullOrWhiteSpace(clienteDto.Nome))
                _notificador.Add(new Notificacao("Nome não pode ser nulo ou vazio"));

            if (string.IsNullOrWhiteSpace(clienteDto.Email))
                _notificador.Add(new Notificacao("Email não pode ser nulo ou vazio"));

            if (string.IsNullOrWhiteSpace(clienteDto.Telefone))
                _notificador.Add(new Notificacao("Telefone não pode ser nulo ou vazio"));

            if (cliente == null)
            {
                _notificador.Add(new Notificacao($"Cliente com ID {id} não encontrado."));
                return false;

            }

            if (!cliente.Status)
            {
                _notificador.Add(new Notificacao("Não é possível atualizar um cliente inativo."));
                return false;
            }

            // Validações de campos únicos
            //if (!string.IsNullOrWhiteSpace(clienteDto.Email) && clienteDto.Email != cliente.Email)
            //{
            //    if (await VerificarDisponibilidadeEmailAsync(clienteDto.Email, id, ct))
            //    {
            //        throw new InvalidOperationException($"Email {clienteDto.Email} já está em uso.");
            //    }
            //}

            // Atualizar campos
            cliente.Endereco = clienteDto.Endereco.ToEntity();
            cliente.DataModificacao = DateTime.Now;
            cliente.IdUsuarioModificacao = "TODO"; // Obter ID do usuário atual
            cliente.NumeroHabilitacao = clienteDto.NumeroHabilitacao;
            cliente.ValidadeHabilitacao = clienteDto.ValidadeHabilitacao;
            cliente.Usuario.NomeCompleto = clienteDto.Nome.Trim();
            cliente.Usuario.Email = clienteDto.Email.Trim().ToLower();
            cliente.Usuario.PhoneNumber = LimparTelefone(clienteDto.Telefone);


            // Atualizar no banco
            var atualizado = await _clienteRepository.SalvarAsync();
            if (atualizado == 0)
            {
                throw new InvalidOperationException("Falha ao atualizar cliente.");
            } 
            return true;
        }

        public async Task<bool> ExcluirClienteAsync(int id, CancellationToken ct = default)
        {
            // Buscar cliente existente
            var cliente = await _clienteRepository.ObterPrimeiroAsync(x => x.IdCliente == id, incluir: q => q.Include(c => c.Endereco).Include(c => c.Usuario),rastreado:true);
            if (cliente != null) {
                await _clienteRepository.ExcluirAsync(cliente, ct);
                return true;
            }
            _notificador.Add(new Notificacao($"Cliente com ID {id} não encontrado."));
            return false;
        }

        public async Task<bool> AtivarClienteAsync(int id, CancellationToken ct = default)
        {
            var cliente = await _clienteRepository.ObterPorId(id);
            if (cliente == null)
            {
                _notificador.Add(new Notificacao($"Cliente com ID {id} não encontrado."));
                return false;
            }

            if (cliente.Status)
            {
                return true; // Já está ativo
            }

            // Validar habilitação se necessário
            if (cliente.ValidadeHabilitacao.HasValue &&
                cliente.ValidadeHabilitacao.Value < DateTime.UtcNow)
            {
                throw new InvalidOperationException(
                    "Habilitação do cliente está vencida. Não é possível ativar.");
            }

            cliente.Status = true;
            var atualizado = await _clienteRepository.AtualizarAsync(cliente, ct);
            return atualizado;            
        }

        public async Task<bool> DesativarClienteAsync(int id, CancellationToken ct = default)
        {
            var cliente = await _clienteRepository.ObterPorId(id);
            if (cliente == null)
            {
                throw new KeyNotFoundException($"Cliente com ID {id} não encontrado.");
            }

            if (!cliente.Status)
            {
                return true; // Já está inativo
            }

            // Verificar se cliente possui locações ativas
            //if (await ClientePossuiLocacoesAtivasAsync(id, ct))
            //{
            //    throw new InvalidOperationException(
            //        "Cliente possui locações ativas. Finalize as locações antes de desativar.");
            //}

            cliente.Status = false;
            var atualizado = await _clienteRepository.AtualizarAsync(cliente, ct);
            return atualizado;
           
        }

        #endregion

        #region Validações e Regras de Negócio

        public async Task<bool> ValidarClienteParaLocacaoAsync(int id, CancellationToken ct = default)
        {            
            var cliente = await _clienteRepository.ObterPrimeiroAsync(
                c => c.IdCliente == id && c.Status,
                ct: ct);

            if (cliente == null)
            {
                throw new KeyNotFoundException($"Cliente com ID {id} não encontrado ou inativo.");
            }                

            // Verificar habilitação válida
            if (cliente.ValidadeHabilitacao.HasValue &&
                cliente.ValidadeHabilitacao.Value < DateTime.UtcNow)
            {
                return false;
            }

            // Verificar se está em dia com pagamentos
            //if (!await ClienteEstaEmDiaComPagamentosAsync(id, ct))
            //{
            //    return false;
            //}

            return true;           
        }

        //public async Task<bool> ClientePossuiLocacoesAtivasAsync(int id, CancellationToken ct = default)
        //{
        //    try
        //    {
        //        // Aqui você precisaria acessar o repositório de locações
        //        // Esta é uma implementação simplificada
        //        var query = from locacao in _dbContext.Set<Locacao>()
        //                    where locacao.ClienteId == id
        //                          && (locacao.Status == LocacaoStatus.Ativa ||
        //                              locacao.Status == LocacaoStatus.Atrasada)
        //                    select locacao.Id;

        //        return await query.AnyAsync(ct);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Erro ao verificar locações ativas do cliente ID: {Id}", id);
        //        throw;
        //    }
        //}

        //public async Task<bool> ClienteEstaEmDiaComPagamentosAsync(int id, CancellationToken ct = default)
        //{
        //    try
        //    {
        //        // Implementação simplificada - você precisaria ajustar para sua estrutura
        //        var query = from pagamento in _dbContext.Set<Pagamento>()
        //                    join locacao in _dbContext.Set<Locacao>()
        //                         on pagamento.LocacaoId equals locacao.Id
        //                    where locacao.ClienteId == id
        //                          && pagamento.Status == PagamentoStatus.Pendente
        //                          && pagamento.DataVencimento < DateTime.UtcNow
        //                    select pagamento.Id;

        //        return !await query.AnyAsync(ct);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Erro ao verificar pagamentos do cliente ID: {Id}", id);
        //        throw;
        //    }
        //}

        #endregion

        #region Métodos Auxiliares

        //public async Task<bool> VerificarDisponibilidadeEmailAsync(
        //    string email,
        //    int? idExcluir = null,
        //    CancellationToken ct = default)
        //{
        //    try
        //    {
        //        var emailNormalizado = email.Trim().ToLower();

        //        var query = _clienteRepository.(emailNormalizado)
        //            .Where(c => c.Email == emailNormalizado && c.Status == true);

        //        if (idExcluir.HasValue)
        //        {
        //            query = query.Where(c => c.Id != idExcluir.Value);
        //        }

        //        return !await query.AnyAsync(ct);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Erro ao verificar disponibilidade do email: {Email}", email);
        //        throw;
        //    }
        //}

        //public async Task<bool> VerificarDisponibilidadeCpfAsync(
        //    string cpf,
        //    int? idExcluir = null,
        //    CancellationToken ct = default)
        //{
        //    try
        //    {
        //        var cpfLimpo = LimparCpf(cpf);

        //        var query = _dbContext.Set<ClienteDto>()
        //            .Where(c => c.Cpf == cpfLimpo);

        //        if (idExcluir.HasValue)
        //        {
        //            query = query.Where(c => c.IdCliente != idExcluir.Value);
        //        }

        //        return !await query.AnyAsync(ct);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Erro ao verificar disponibilidade do CPF: {Cpf}", cpf);
        //        throw;
        //    }
        //}

        #endregion

        #region Métodos Privados

        private async Task ValidarCriacaoClienteAsync(CriarClienteDto clienteDto, CancellationToken ct)
        {
            // Validar CPF
            if (!DocumentoExtensionMethods.EhCpfValido(clienteDto.Cpf))
            {
                throw new ArgumentException("CPF inválido.");
            }

            // Verificar se CPF já existe
            if (await ExisteClienteAsync(clienteDto.Cpf, ct))
            {
                throw new InvalidOperationException($"CPF {clienteDto.Cpf} já cadastrado.");
            }

            // Verificar se email já existe
            //if (await VerificarDisponibilidadeEmailAsync(clienteDto.Email, null, ct))
            //{
            //    throw new InvalidOperationException($"Email {clienteDto.Email} já cadastrado.");
            //}

            // Validar email
            if (!ValidarEmail(clienteDto.Email))
            {
                throw new ArgumentException("Email inválido.");
            }
        }

        private string LimparCpf(string cpf)
        {
            return new string(cpf.Where(char.IsDigit).ToArray());
        }

        private string LimparTelefone(string telefone)
        {
            return new string(telefone.Where(char.IsDigit).ToArray());
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

        private bool ClienteMaiorDeIdade(DateTime dataNascimento)
        {
            var idade = DateTime.UtcNow.Year - dataNascimento.Year;

            // Ajustar se ainda não fez aniversário este ano
            if (DateTime.UtcNow < dataNascimento.AddYears(idade))
            {
                idade--;
            }

            return idade >= 18;
        }

        #endregion
    }
}