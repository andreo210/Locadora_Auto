using Locadora_Auto.Application.Configuration.Ultils.NotificadorServices;
using Locadora_Auto.Application.Configuration.UtilExtensions;
using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Application.Models.Mappers;
using Locadora_Auto.Domain;
using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Domain.Entidades.Indentity;
using Locadora_Auto.Domain.IRepositorio;
using Locadora_Auto.Infra.Data.Repositorio;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace Locadora_Auto.Application.Services.ClienteServices
{
    public class ClienteService : IClienteService
    {
        private readonly IClienteRepository _clienteRepository;
        private readonly UserManager<User> _userManager;
        private readonly INotificadorService _notificador;

        public ClienteService(
            UserManager<User> userManager,
            IClienteRepository clienteRepository,
            IUnitOfWork transaction,
            INotificadorService notificador,
        ILogger<ClienteService> logger)
        {
            _clienteRepository = clienteRepository ?? throw new ArgumentNullException(nameof(clienteRepository));
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
            var entidade = await _clienteRepository.ObterPrimeiroAsync(c => c.Usuario.Cpf == cpfLimpo, ct: ct, incluir: q => q.Include(c => c.Endereco).Include(c => c.Usuario).Include(c => c.Reservas));
            return entidade?.ToDto();
      
        }

        public async Task<IReadOnlyList<ClienteDto>> ObterTodosAsync(CancellationToken ct = default)
        {
           
            var entidade = await _clienteRepository.ObterAsync(ordenarPor: q => q.OrderBy(c => c.Usuario.NomeCompleto), ct: ct,incluir: q => q.Include(c => c.Endereco).Include(c => c.Usuario).Include(c => c.Reservas));
            if(entidade == null)
            {
                return new List<ClienteDto>();
            }
            return entidade.Select(c => c.ToDto()).ToList();

        }

        public async Task<IReadOnlyList<ClienteDto>> ObterAtivosAsync(CancellationToken ct = default)
        {
            var entidades = await _clienteRepository.ObterAsync( filtro: c => c.Ativo, ordenarPor: q => q.OrderBy(c => c.Usuario.NomeCompleto), ct: ct);
            return entidades.Select(d => d.ToDto()).ToList();

        }

        public async Task<IReadOnlyList<ClienteDto>> ObterPorNomeAsync(string nome, CancellationToken ct = default)
        {
            
            //ToLower nos dois lados: no Postgres o LIKE é sensível a maiúsculas, diferente do MySQL
            var nomeBusca = nome.ToLower();
            var entidades = await _clienteRepository.ObterAsync(filtro: c => c.Usuario.NomeCompleto.ToLower().Contains(nomeBusca) && c.Ativo, ordenarPor: q => q.OrderBy(c => c.Usuario.NomeCompleto), ct: ct);
            return entidades.Select(d=>d.ToDto()).ToList();

        }

        public async Task<IReadOnlyList<ClienteDto>> ObterPorEmailAsync(string email, CancellationToken ct = default)
        {

            var entidades = await _clienteRepository.ObterAsync(filtro: c => c.Usuario.Email == email && c.Ativo,ordenarPor: q => q.OrderBy(c => c.Usuario.NomeCompleto), ct: ct);
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
            return model.Count(c => c.Ativo);           
        }

        public async Task<IReadOnlyList<ClienteDto>> ObterPaginadoAsync(int pagina,int tamanhoPagina, CancellationToken ct = default)
        {
            
            if (pagina < 1) pagina = 1;
            if (tamanhoPagina < 1) tamanhoPagina = 10;
            if (tamanhoPagina > 100) tamanhoPagina = 100;



            var entidades = await _clienteRepository.ObterPaginadoAsync(
                filtro: c => c.Ativo,
                pagina: pagina,
                ItemPorPagina: tamanhoPagina,
                ordenarPor: q => q.OrderBy(c => c.Usuario.NomeCompleto),
                ct: ct);
            return entidades.Select(d => d.ToDto()).ToList();
            
        }


        public async Task<PaginatedResult<ClienteDto>> ObterPaginadoAsync(
            bool? ativos = null, // Mude de true para null
            string? nome = null,
            string? cpf = null,
            string? ordenarPor = null,
            string? ordem = null,
            int pagina = 1, // Adicionar parâmetros de paginação
            int itensPorPagina = 10,
            CancellationToken ct = default)
        {
            // Lista para armazenar as condições
            var condicoes = new List<Expression<Func<Clientes, bool>>>();

            // Adiciona cada condição separadamente
            if (ativos.HasValue)
            {
                condicoes.Add(f => f.Ativo == ativos.Value);
            }

            if (!string.IsNullOrWhiteSpace(nome))
            {
                //ToLower nos dois lados: no Postgres o LIKE é sensível a maiúsculas, diferente do MySQL
                var nomeBusca = nome.ToLower();
                condicoes.Add(f => f.Usuario.NomeCompleto.ToLower().Contains(nomeBusca));
            }

            if (!string.IsNullOrWhiteSpace(cpf))
            {
                condicoes.Add(f => f.Usuario.Cpf == cpf);
            }
            if(string.IsNullOrEmpty(ordem)) ordem = "asc";
            if (string.IsNullOrEmpty(ordenarPor)) ordem = "numeroHabilitacao";

            // Combina todas as condições com AND
            Expression<Func<Clientes, bool>>? filtro = null;

            if (condicoes.Any())
            {
                filtro = condicoes.Aggregate((atual, proxima) =>
                {
                    // Combina duas expressions com AND
                    var parameter = Expression.Parameter(typeof(Clientes));
                    var body = Expression.AndAlso(
                        Expression.Invoke(atual, parameter),
                        Expression.Invoke(proxima, parameter)
                    );
                    return Expression.Lambda<Func<Clientes, bool>>(body, parameter);
                });
            }

            // Passar os parâmetros de paginação para o repositório
            var clientes = await _clienteRepository.ObterPaginadoComFiltroAsync(
                filtro: filtro,
                incluir: q => q.Include(f => f.Usuario),
                ordenarPor: ObterOrdenacao(ordenarPor, ordem),
                pagina: pagina,
                itensPorPagina: itensPorPagina,
                asNoTracking: true,
                asSplitQuery: true,
                ct: ct);

            // Mapear manualmente para DTO (incluindo IdFuncionario)
            var itemsDto = clientes.Items.Select(f => new ClienteDto
            {
                IdCliente = f.IdCliente, 
                Cpf = f.Usuario.Cpf,
                Nome = f.Usuario?.NomeCompleto ?? string.Empty,
                Email = f.Usuario?.Email ?? string.Empty,
                Telefone = f.Usuario.PhoneNumber, // Adicionar Telefone se existir
                NumeroHabilitacao = f.NumeroHabilitacao,
                ValidadeHabilitacao = f.ValidadeHabilitacao,
                Ativo = f.Ativo
            }).ToList();

            // Retornar resultado paginado com DTOs
            return new PaginatedResult<ClienteDto>
            {
                Items = itemsDto,
                Total = clientes.Total,
                Pagina = clientes.Pagina,
                TotalPaginas = clientes.TotalPaginas,
                ItensPorPagina = clientes.ItensPorPagina
            };
        }

        public async Task<List<ClienteDto>> ObterSolicitacoesComFiltroAsync(
           bool? ativo = null,
           string? cpf = null,
           string? nome = null,
           string? email = null
         )
        {
            Expression<Func<Clientes, bool>>? filtro = null;
            if (ativo != null || cpf != null || nome != null || email != null)
            {
                filtro = s =>
                    (ativo == null || s.Ativo == ativo) &&
                    (cpf == null || s.Usuario.Cpf == cpf) &&
                    (nome == null || s.Usuario.NomeCompleto == nome) &&
                    (email == null || (s.Usuario.Email == email));
            }
            var entidades = await _clienteRepository.ObterComFiltroAsync(filtro);
            return entidades.Select(d => d.ToDto()).ToList(); 
        }

        private Func<IQueryable<Clientes>, IOrderedQueryable<Clientes>>? ObterOrdenacao(string? ordenarPor, string? ordem)
        {
            if (string.IsNullOrWhiteSpace(ordenarPor))
                return null;

            var ascendente = ordem?.ToLower() != "desc";

            return ordenarPor.ToLower() switch
            {
                "nome" => ascendente
                    ? q => q.OrderBy(f => f.Usuario.NomeCompleto)
                    : q => q.OrderByDescending(f => f.Usuario.NomeCompleto),

                "email" => ascendente
                    ? q => q.OrderBy(f => f.Usuario.Email)
                    : q => q.OrderByDescending(f => f.Usuario.Email),

                "cpf" => ascendente
                    ? q => q.OrderBy(f => f.Usuario.Cpf)
                    : q => q.OrderByDescending(f => f.Usuario.Cpf),

                "status" => ascendente
                    ? q => q.OrderBy(f => f.Ativo)
                    : q => q.OrderByDescending(f => f.Ativo),

                "telefone" => ascendente
                    ? q => q.OrderBy(f => f.Usuario.PhoneNumber)
                    : q => q.OrderByDescending(f => f.Usuario.PhoneNumber),

                "id" => ascendente
                    ? q => q.OrderBy(f => f.IdCliente)
                    : q => q.OrderByDescending(f => f.IdCliente),

                // Padrão: ordenar por matrícula
                _ => ascendente
                    ? q => q.OrderBy(f => f.NumeroHabilitacao)
                    : q => q.OrderByDescending(f => f.NumeroHabilitacao),
            };
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

        #region Operações de gravação Cliente

        public async Task<ClienteDto> CriarClienteAsync(CriarClienteDto clienteDto, CancellationToken ct = default)
        {
            clienteDto.Cpf = clienteDto.Cpf.Replace("-", "").Replace(".", "");
            // Validações
            if(!await ValidarCriacaoClienteAsync(clienteDto, ct)) return null;

            // Inserir no banco
            var user = User.Criar(clienteDto.Nome, clienteDto.Cpf, clienteDto.Telefone, clienteDto.Email);
            user.Cliente = Clientes.Criar(clienteDto.NumeroHabilitacao, clienteDto.ValidadeHabilitacao.Value,clienteDto.Endereco.ToEntity());

            var result = await _userManager.CreateAsync(user, clienteDto.Senha);

            if (!result.Succeeded)
                throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));

            // Operações adicionais que devem ser atômicas
            // await EnviarNotificacaoCadastroAsync(model.Email, ct); // ⚠️ Cuidado com operações externas!

            var cliente = user.Cliente.ToDto();
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

            if (cliente == null)
            {
                _notificador.Add($"Cliente com ID {id} não encontrado.");
                return false;

            }

            if (!cliente.Ativo)
            {
                _notificador.Add("Não é possível atualizar um cliente inativo.");
                return false;
            }

            // Validações de campos únicos
            if (!await ValidarAtualizacaoClienteAsync(clienteDto,cliente,ct)) return false;
           
            // Atualizar campos
            cliente.Atualizar(clienteDto!.NumeroHabilitacao,clienteDto.ValidadeHabilitacao!.Value,clienteDto.Endereco.ToEntity());
            cliente.Usuario.Atualizar(clienteDto.Nome, clienteDto.Telefone, clienteDto.Email);


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
                await _clienteRepository.ExcluirSalvarAsync(cliente, ct);
                return true;
            }
            _notificador.Add($"Cliente com ID {id} não encontrado.");
            return false;
        }

        public async Task<bool> AtivarClienteAsync(int id, CancellationToken ct = default)
        {
            var cliente = await _clienteRepository.ObterPorIdAsync(id);
            if (cliente == null)
            {
                _notificador.Add($"Cliente com ID {id} não encontrado.");
                return false;
            }

            if (cliente.Ativo)
            {
                return true; // Já está ativo
            }

            // Validar habilitação se necessário
            if (cliente.ValidadeHabilitacao.HasValue && cliente.ValidadeHabilitacao.Value < DateTime.UtcNow)
            {
                _notificador.Add("Habilitação do cliente está vencida. Não é possível ativar.");
                return false;
            }

            cliente.Ativar();
            var atualizado = await _clienteRepository.AtualizarSalvarAsync(cliente, ct);
            return atualizado;            
        }

        public async Task<bool> DesativarClienteAsync(int id, CancellationToken ct = default)
        {
            var cliente = await _clienteRepository.ObterPorIdAsync(id);
            if (cliente == null)
            {
               _notificador.Add($"Cliente com ID {id} não encontrado.");
                return false;
            }

            if (!cliente.Ativo)
            {
                return true; // Já está inativo
            }

            // Verificar se cliente possui locações ativas
            //if (await ClientePossuiLocacoesAtivasAsync(id, ct))
            //{
            //    throw new InvalidOperationException(
            //        "Cliente possui locações ativas. Finalize as locações antes de desativar.");
            //}

            cliente.Desativar();
            var atualizado = await _clienteRepository.AtualizarSalvarAsync(cliente, ct);
            return atualizado;
           
        }

        #endregion Operações de gravação Cliente

        #region Validações e Regras de Negócio

        public async Task<bool> ValidarClienteParaLocacaoAsync(int id, CancellationToken ct = default)
        {            
            var cliente = await _clienteRepository.ObterPrimeiroAsync(
                c => c.IdCliente == id && c.Ativo,
                ct: ct);

            if (cliente == null)
            {
                _notificador.Add($"Cliente com ID {id} não encontrado ou inativo.");
                return false;
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



        #endregion

        #region Métodos Auxiliares
        private async Task<bool> VerificarDisponibilidadeEmailAsync(string email, CancellationToken ct = default)
        {
            var entidade = await _clienteRepository.ObterPrimeiroAsync(c => c.Usuario.Email == email, ct: ct, incluir: q => q.Include(c => c.Usuario));
            if (entidade == null)
            {
                return true;
            }
            return false;
        }


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

        private async Task<bool> VerificarDisponibilidadeCpfAsync(string cpf, CancellationToken ct = default)
        {
            var entidade = await _clienteRepository.ObterPrimeiroAsync(c => c.Usuario.UserName == cpf, ct: ct, incluir: q => q.Include(c => c.Usuario));
            if (entidade == null)
            {
                return true;
            }
            return false;
        }

        #endregion

        #region Métodos Privados

        private async Task<bool> ValidarCriacaoClienteAsync(CriarClienteDto clienteDto, CancellationToken ct)
        {
            // Validar CPF
            if (!DocumentoExtensionMethods.EhCpfValido(clienteDto.Cpf)) _notificador.Add("CPF inválido.");

            // Verificar se CPF já existe
            if (await ExisteClienteAsync(clienteDto.Cpf, ct)) _notificador.Add($"CPF {clienteDto.Cpf} já cadastrado.");

            // Verificar se email já existe
            if (!await VerificarDisponibilidadeEmailAsync(clienteDto.Email, ct)) _notificador.Add($"Email {clienteDto.Email} já cadastrado.");

            // Verificar se email já existe
            if (!await VerificarDisponibilidadeCpfAsync(clienteDto.Cpf, ct)) _notificador.Add($"CPF {clienteDto.Cpf} já cadastrado.");            

            // Validar email
            if (!ValidarEmail(clienteDto.Email)) _notificador.Add("Email inválido.");

            //Validar Cnh
            if (clienteDto.ValidadeHabilitacao < DateTime.Today) _notificador.Add("CNH inválida.");


            var notificacoes = _notificador.ObterNotificacoes();
            if (notificacoes.Any()) return false;
            return true;
        }

        

        private async Task<bool> ValidarAtualizacaoClienteAsync(AtualizarClienteDto clienteDto,Clientes clientes, CancellationToken ct)
        {
            // Verificar se email já existe
            if (!await VerificarDisponibilidadeEmailAsync(clienteDto.Email, ct))
            {
                if(clientes.Usuario.Email != clienteDto.Email) _notificador.Add($"Email {clienteDto.Email} já cadastrado.");
            }

            // Validar email
            if (!ValidarEmail(clienteDto.Email)) _notificador.Add("Email inválido.");

            //Validar Cnh
            if (clienteDto.ValidadeHabilitacao < DateTime.Today) _notificador.Add("CNH inválida.");


            var notificacoes = _notificador.ObterNotificacoes();
            if (notificacoes.Any()) return false;
            return true;
        }
               

        private string LimparCpf(string cpf)
        {
            return new string(cpf.Where(char.IsDigit).ToArray());
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

        #endregion
    }
}