using Locadora_Auto.Application.Configuration.Ultils.NotificadorServices;
using Locadora_Auto.Application.Configuration.UtilExtensions;
using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Application.Models.Mappers;
using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Domain.Entidades.Indentity;
using Locadora_Auto.Domain.IRepositorio;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace Locadora_Auto.Application.Services.ClienteServices
{
    public class ClienteService : IClienteService
    {
        private readonly IClienteRepository _clienteRepository;
        private readonly IReservaRepository _reservaRepository;
        private readonly ILocacaoRepository _locacaoRepository;
        private readonly ICategoriaVeiculosRepository _categoriaVeiculoRepository;
        private readonly IVeiculosRepository _veiculoRepository;
        private readonly UserManager<User> _userManager;
        private readonly INotificadorService _notificador;

        public ClienteService(
            UserManager<User> userManager,
            IClienteRepository clienteRepository,
            IUnitOfWork transaction,
            INotificadorService notificador,
            ICategoriaVeiculosRepository categoriaVeiculoRepository,
            IReservaRepository reservaRepository,
            IVeiculosRepository veiculoRepository,
            ILocacaoRepository locacaoRepository,
        ILogger<ClienteService> logger)
        {
            _clienteRepository = clienteRepository ?? throw new ArgumentNullException(nameof(clienteRepository));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(logger));
            _notificador = notificador ?? throw new ArgumentNullException(nameof(notificador));
            _categoriaVeiculoRepository = categoriaVeiculoRepository?? throw new ArgumentNullException(nameof(categoriaVeiculoRepository));
            _reservaRepository = reservaRepository ?? throw new ArgumentNullException(nameof(reservaRepository));
            _veiculoRepository = veiculoRepository ?? throw new ArgumentNullException(nameof(veiculoRepository));
            _locacaoRepository = locacaoRepository ?? throw new ArgumentNullException(nameof(locacaoRepository));
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
            var entidades = await _clienteRepository.ObterAsync( filtro: c => c.Ativo, ordenarPor: q => q.OrderBy(c => c.Usuario.NomeCompleto), ct: ct);
            return entidades.Select(d => d.ToDto()).ToList();

        }

        public async Task<IReadOnlyList<ClienteDto>> ObterPorNomeAsync(string nome, CancellationToken ct = default)
        {
            
            var entidades = await _clienteRepository.ObterAsync(filtro: c => c.Usuario.NomeCompleto.Contains(nome) && c.Ativo, ordenarPor: q => q.OrderBy(c => c.Usuario.NomeCompleto), ct: ct);
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

            var skip = (pagina - 1) * tamanhoPagina;

            var entidades = await _clienteRepository.ObterPaginadoAsync(
                filtro: c => c.Ativo,
                skip: skip,
                take: tamanhoPagina,
                ordenarPor: q => q.OrderBy(c => c.Usuario.NomeCompleto),
                ct: ct);
            return entidades.Select(d => d.ToDto()).ToList();
            
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
            if (!await ValidarAtualizacaoClienteAsync(clienteDto,ct)) return false;
           
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

        #region Reservar e Cancelar Reserva
        public async Task<bool> CriarReservaAsync(CriarReservaDto dto, CancellationToken ct = default)
        {
            var veiculo = await _categoriaVeiculoRepository.ObterPrimeiroAsync(x=>x.Id == dto.IdCategoriaVeiculo,null,true, ct);
            var cliente= await _clienteRepository.ObterPrimeiroAsync(x => x.IdCliente == dto.IdCliente, incluir:x=>x.Include(u=>u.Usuario), true, ct);
            if (!await PodeReservar(dto))
            {
                _notificador.Add("reserva não autorizada");
            }

            if (cliente == null)
            {
                _notificador.Add($"Cliente com ID {dto.IdCliente} não encontrado.");
            }
            if (!cliente.PodeLocar())
            {
                _notificador.Add("Cliente não está apto para realizar uma locação.");
            }
            if (veiculo== null)
            {
                _notificador.Add($"Categoria de Veículo com ID {dto.IdCategoriaVeiculo} não encontrado.");
            }

            if (dto.DataInicio< DateTime.Now)
            {
                _notificador.Add("Data de início da reserva não pode ser menor que a data atual.");
            }
            if (dto.DataFim < DateTime.Now)
            {
                _notificador.Add("Data de entrega da reserva não pode ser menor que a data atual.");
            }
            if (dto.DataFim < dto.DataInicio)
            {
                _notificador.Add("Data de entrega da reserva não pode ser menor que a data de início.");
            }
            if(_notificador.ObterNotificacoes().Any())
            {
                return false;
            }

            cliente.ReservarVeiculo(cliente.IdCliente, dto.DataInicio, dto.DataFim, dto.IdFilial, veiculo);
            return await _clienteRepository.AtualizarSalvarAsync(cliente, ct);
        }

        public async Task<bool> CancelarReservaAsync(int idReserva,int id, CancellationToken ct = default)
        {

            var cliente = await _clienteRepository.ObterPrimeiroAsync(x => x.IdCliente == id, incluir: x => x.Include(u => u.Reservas).ThenInclude(x=>x.CategoriaVeiculo), true, ct);

            var reserva = cliente.Reservas.FirstOrDefault(x => x.IdReserva == idReserva);

            if (reserva == null)
            {
                _notificador.Add($"Reserva com ID {idReserva} não encontrado.");
            }
            if (reserva.CategoriaVeiculo == null)
            {
                _notificador.Add($"Veículo não encontrado.");
            }           
            if (_notificador.ObterNotificacoes().Any())
            {
                return false;
            }
            cliente.CancelarReservar(reserva);
            return await _clienteRepository.AtualizarSalvarAsync(cliente);
        }

        #endregion Reservar e Cancelar Reserva

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

        private async Task<bool> PodeReservar(CriarReservaDto dto)
        {
            var veiculosDisponiveis = await _veiculoRepository.ContarAsync(x=>x.IdCategoria == dto.IdCategoriaVeiculo && x.FilialAtualId == dto.IdFilial && x.Disponivel == true);
            if (veiculosDisponiveis == 0)_notificador.Add("Não há veículos disponíveis para a categoria e filial selecionadas.");

            var locacoes = await _locacaoRepository.ContarAsync(x => x.Veiculo.IdCategoria == dto.IdCategoriaVeiculo && x.Veiculo.FilialAtualId == dto.IdFilial && x.Status !=StatusLocacao.Finalizada );
            var reservas = await _reservaRepository.ContarAsync(x => x.IdCategoria == dto.IdCategoriaVeiculo && x.IdFilial == dto.IdFilial && x.Status == StatusReserva.Reservado);


            return veiculosDisponiveis > (locacoes + reservas);
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

        

        private async Task<bool> ValidarAtualizacaoClienteAsync(AtualizarClienteDto clienteDto, CancellationToken ct)
        {
            // Verificar se email já existe
            if (!await VerificarDisponibilidadeEmailAsync(clienteDto.Email, ct)) _notificador.Add($"Email {clienteDto.Email} já cadastrado.");

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