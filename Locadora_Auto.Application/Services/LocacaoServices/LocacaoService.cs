using Locadora_Auto.Application.Configuration.Ultils.NotificadorServices;
using Locadora_Auto.Application.Mappers;
using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Application.Models.Mappers;
using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Domain.IRepositorio;
using Microsoft.EntityFrameworkCore;

namespace Locadora_Auto.Application.Services.LocacaoServices
{
    public class LocacaoService : ILocacaoService
    {
        private readonly ILocacaoRepository _locacaoRepository;
        private readonly IClienteRepository _clienteRepository;
        private readonly IVeiculosRepository _veiculoRepository;
        private readonly IFilialRepository _filialRepository;
        private readonly IFuncionarioRepository _funcionarioRepository;
        private readonly INotificadorService _notificador;

        public LocacaoService(
            ILocacaoRepository locacaoRepository,
            IClienteRepository clienteRepository,
            IVeiculosRepository veiculoRepository,
            IFilialRepository filialRepository,
            IFuncionarioRepository funcionarioRepository,
            INotificadorService notificador)
        {
            _locacaoRepository = locacaoRepository;
            _clienteRepository = clienteRepository;
            _veiculoRepository = veiculoRepository;
            _filialRepository = filialRepository;
            _funcionarioRepository = funcionarioRepository;
            _notificador = notificador;
        }

        // ====================== CRIAR LOCACAO ======================
        public async Task<LocacaoDto?> CriarAsync(CriarLocacaoDto dto, CancellationToken ct = default)
        {
            var cliente = await _clienteRepository.ObterPorIdAsync(dto.IdCliente, false, ct);
            var veiculo = await _veiculoRepository.ObterPorIdAsync(dto.IdVeiculo, false, ct);
            var filial = await _filialRepository.ObterPorIdAsync(dto.IdFilialRetirada, false, ct);
            var funcionario = await _funcionarioRepository.ObterPorIdAsync(dto.IdFuncionario, false, ct);

            if (cliente == null) _notificador.Add("Cliente não encontrado");
            if (veiculo == null) _notificador.Add("Veículo não encontrado");
            if (funcionario == null) _notificador.Add("Funcionário não encontrado");
            if (veiculo != null && !veiculo.Disponivel) _notificador.Add("Veículo não disponível");
            if (dto.DataFimPrevista <= dto.DataInicio) _notificador.Add("Data fim prevista deve ser posterior à data início");

            if (_notificador.TemNotificacao())
                return null;

            var locacao = Locacao.Criar(
                cliente,
                veiculo!,
                funcionario!,
                dto.IdFilialRetirada,
                dto.DataInicio,
                dto.DataFimPrevista,
                dto.KmInicial,
                dto.ValorPrevisto
            );

            await _locacaoRepository.InserirSalvarAsync(locacao, ct);
            return locacao.ToDto();
        }

        // ====================== ATUALIZAR LOCACAO ======================
        public async Task<LocacaoDto?> AtualizarAsync(int id, AtualizarLocacaoDto dto, CancellationToken ct = default)
        {
            var locacao = await _locacaoRepository.ObterPorIdAsync(id, false, ct);
            if (locacao == null)
            {
                _notificador.Add("Locação não encontrada");
                return null;
            }

            try
            {
                locacao.AtualizarDados(dto.DataFimPrevista, dto.KmInicial, dto.ValorPrevisto);
                await _locacaoRepository.AtualizarSalvarAsync(locacao, ct);
                return locacao.ToDto();
            }
            catch (InvalidOperationException ex)
            {
                _notificador.Add(ex.Message);
                return null;
            }
        }

        // ====================== FINALIZAR LOCACAO ======================
        public async Task<bool> FinalizarAsync(int id, DateTime dataFimReal, int kmFinal, decimal valorFinal, int filialDevolucao, CancellationToken ct = default)
        {
            var locacao = await _locacaoRepository.ObterPrimeiroAsync(x=>x.IdLocacao == id,incluir: q => q.Include(c => c.Veiculo),rastreado:true);
            if (locacao == null)
            {
                _notificador.Add("Locação não encontrada");
                return false;
            }

            try
            {
                locacao.Finalizar(dataFimReal, kmFinal, valorFinal, filialDevolucao);
                await _locacaoRepository.AtualizarSalvarAsync(locacao, ct);
                return true;
            }
            catch (InvalidOperationException ex)
            {
                _notificador.Add(ex.Message);
                return false;
            }
        }

        // ====================== CANCELAR LOCACAO ======================
        public async Task<bool> CancelarAsync(int id, CancellationToken ct = default)
        {
            var locacao = await ObterLocacao(id, ct);
            if (locacao == null)
            {
                _notificador.Add("Locação não encontrada");
                return false;
            }

            try
            {
                locacao.Cancelar();
                await _locacaoRepository.AtualizarSalvarAsync(locacao, ct);
                return true;
            }
            catch (InvalidOperationException ex)
            {
                _notificador.Add(ex.Message);
                return false;
            }
        }

        // ====================== ADICIONAR PAGAMENTO ======================
        public async Task<bool> AdicionarPagamentoAsync(int idLocacao, PagamentoDto dto, CancellationToken ct = default)
        {
            var locacao =  await ObterLocacao(idLocacao, ct); ;
            if (locacao == null)
            {
                _notificador.Add("Locação não encontrada");
                return false;
            }

            try
            {
                var pagamento = dto.ToEntity();
                //todo: entra o tipo pagamento e ja envia o pagamento
                locacao.AdicionarPagamento(pagamento);
                await _locacaoRepository.AtualizarSalvarAsync(locacao, ct);
                return true;
            }
            catch (InvalidOperationException ex)
            {
                _notificador.Add(ex.Message);
                return false;
            }
        }

        // ====================== ADICIONAR MULTA ======================
        public async Task<bool> AdicionarMultaAsync(int idLocacao, MultaDto dto, CancellationToken ct = default)
        {
            var locacao = await ObterLocacao(idLocacao, ct); ;
            if (locacao == null)
            {
                _notificador.Add("Locação não encontrada");
                return false;
            }

            try
            {
                var multa = dto.ToEntity();
                //todo: entra o tipo e ele ja manda a multa
                locacao.AdicionarMulta(multa);
                await _locacaoRepository.AtualizarSalvarAsync(locacao, ct);
                return true;
            }
            catch (InvalidOperationException ex)
            {
                _notificador.Add(ex.Message);
                return false;
            }
        }

        // ====================== ADICIONAR SEGURO ======================
        public async Task<bool> AdicionarSeguroAsync(int idLocacao, LocacaoSeguroDto dto, CancellationToken ct = default)
        {
            var locacao = await ObterLocacao(idLocacao, ct);
            if (locacao == null)
            {
                _notificador.Add("Locação não encontrada");
                return false;
            }
            //Todo vai buscar seguro e adicionar na entidade

            var locacaos = new LocacaoSeguro(); // substituir por busca real
            locacao.AdicionarSeguro(locacaos);
            await _locacaoRepository.AtualizarSalvarAsync(locacao, ct);
            return true;
        }

        // ====================== OBTER POR ID ======================
        public async Task<LocacaoDto?> ObterPorIdAsync(int id, CancellationToken ct = default)
        {
            var locacao = await ObterLocacao(id, ct);
            if (locacao == null)
            {
                _notificador.Add("Locação não encontrada");
                return null;
            }

            return locacao.ToDto();
        }

        // ====================== LISTAR TODAS ======================
        public async Task<IEnumerable<LocacaoDto>> ObterTodasAsync(CancellationToken ct = default)
        {
            var locacoes = await _locacaoRepository.ObterTodos().ToListAsync(ct);
            return locacoes.Select(x => x.ToDto()).ToList();
        }

        private async Task<Locacao> ObterLocacao(int id, CancellationToken ct)
        {
            var locacao = await _locacaoRepository.ObterPrimeiroAsync(
                x => x.IdLocacao == id, 
                incluir: q => q.Include(c => c.Veiculo),
                //.Include(c => c.Seguros)
                // .Include(m=>m.Multas),
                
                rastreado: true);
            return locacao!;
        }
    }
}
