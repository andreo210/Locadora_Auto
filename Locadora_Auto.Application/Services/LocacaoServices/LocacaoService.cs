using Locadora_Auto.Application.Configuration.Ultils.NotificadorServices;
using Locadora_Auto.Application.Configuration.Ultils.UploadArquivo;
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
        private readonly IUploadDownloadFileService _uploadDownloadFileService;
        private readonly IReservaRepository _reservaRepository;
        private readonly ILocacaoSeguroRepository _locacaoSeguroRepository;
        private readonly IClienteRepository _clienteRepository;
        private readonly IVeiculosRepository _veiculoRepository;
        private readonly IVistoriaRepository _vistoriaRepository;
        private readonly ISeguroRepository _seguroRepository;
        private readonly IFilialRepository _filialRepository;
        private readonly IFuncionarioRepository _funcionarioRepository;
        private readonly INotificadorService _notificador;

        public LocacaoService(
            ILocacaoRepository locacaoRepository,
            IClienteRepository clienteRepository,
            IVeiculosRepository veiculoRepository,
            IReservaRepository reservaRepository,
            IVistoriaRepository vistoriaRepository,
            IFilialRepository filialRepository,
            ISeguroRepository seguroRepository,
            ILocacaoSeguroRepository locacaoSeguroRepository,
            IFuncionarioRepository funcionarioRepository,
            IUploadDownloadFileService uploadDownloadFileService,
            INotificadorService notificador)
        {
            _locacaoRepository = locacaoRepository;
            _clienteRepository = clienteRepository;
            _veiculoRepository = veiculoRepository;
            _filialRepository = filialRepository;
            _funcionarioRepository = funcionarioRepository;
            _notificador = notificador;
            _seguroRepository = seguroRepository;
            _reservaRepository = reservaRepository;
            _locacaoSeguroRepository = locacaoSeguroRepository;
            _uploadDownloadFileService = uploadDownloadFileService;
            _vistoriaRepository = vistoriaRepository;
        }

        #region Locacao
        public async Task<LocacaoDto?> CriarAsync(CriarLocacaoDto dto, CancellationToken ct = default)
        {
            var reserva = await _reservaRepository.ObterPrimeiroAsync(r=>r.IdReserva == dto.idReserva.Value,null, true, ct);
            var veiculo = await _veiculoRepository.ObterPorIdAsync(dto.IdVeiculo, false, ct);
            if (reserva != null)
            {
                if (reserva.Ativo == false)
                {
                    _notificador.Add("Essa reserva foi cancelada");
                    return null;
                }
                if (reserva.IdCliente != dto.IdCliente)
                    _notificador.Add("Reserva não pertence ao cliente informado");
                if (reserva.IdCategoria != veiculo!.IdCategoria)
                    _notificador.Add("Veículo não pertence à categoria da reserva");
                if (reserva.DataInicio != dto.DataInicio || reserva.DataFim != dto.DataFimPrevista)
                    _notificador.Add("Datas da locação não coincidem com as datas da reserva");
                if (reserva.IdFilial != dto.IdFilialRetirada)
                    _notificador.Add("Filial de retirada não coincide com a filial da reserva");
                if (reserva.Status != StatusReserva.Reservado)
                    _notificador.Add("Reserva não está em status reservado");
                if (_notificador.TemNotificacao())
                    return null;

                dto.DataInicio = reserva.DataInicio;
                dto.DataFimPrevista = reserva.DataFim;
                dto.IdFilialRetirada = reserva.IdFilial;
                dto.IdCliente = reserva.IdCliente;
            }

            var cliente = await _clienteRepository.ObterPorIdAsync(dto.IdCliente, false, ct);
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
                reserva!,
                dto.IdFilialRetirada.Value,
                dto.DataInicio.Value,
                dto.DataFimPrevista.Value,
                dto.KmInicial.Value,
                dto.ValorPrevisto
            );

            await _locacaoRepository.InserirSalvarAsync(locacao, ct);
            return locacao.ToDto();
        }
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
        #endregion Locacao

        #region Pagamento
        public async Task<bool> AdicionarPagamentoAsync(int id,AdicionarPagamentoDto pagamento, CancellationToken ct = default)
        {
            var locacao = await _locacaoRepository.ObterPrimeiroAsync(x => x.IdLocacao == id, incluir: q => q.Include(l => l.Pagamentos), rastreado: true, ct);

            if (locacao == null)
            {
                _notificador.Add("Locação não encontrada");
                return false;
            }

            locacao.AdicionarPagamento(pagamento.Valor, (FormaPagamento)pagamento.IdFormaPagamento);
            await _locacaoRepository.AtualizarSalvarAsync(locacao, ct);
            return true;
           
        }

        public async Task<bool> ConfirmarPagamentoAsync(int id, int idPagamento, CancellationToken ct = default)
        {
            var locacao = await _locacaoRepository.ObterPrimeiroAsync(x => x.IdLocacao == id, incluir: q => q.Include(l => l.Pagamentos), rastreado: true, ct);

            if (locacao == null)
            {
                _notificador.Add("Locação não encontrada");
                return false;
            }

            locacao.ConfirmarPagamento(idPagamento);
            await _locacaoRepository.AtualizarSalvarAsync(locacao, ct);
            return true;

        }

        public async Task<bool> CancelarPagamentoAsync(int id, int idPagamento,string motivo, CancellationToken ct = default)
        {
            var locacao = await _locacaoRepository.ObterPrimeiroAsync(x => x.IdLocacao == id, incluir: q => q.Include(l => l.Pagamentos), rastreado: true, ct);

            if (locacao == null)
            {
                _notificador.Add("Locação não encontrada");
                return false;
            }

            locacao.CancelarPagamento(idPagamento,motivo);
            await _locacaoRepository.AtualizarSalvarAsync(locacao, ct);
            return true;
        }

        public async Task<bool> MarcarComoFalhaAsync(int id, int idPagamento,CancellationToken ct = default)
        {
            var locacao = await _locacaoRepository.ObterPrimeiroAsync(x => x.IdLocacao == id, incluir: q => q.Include(l => l.Pagamentos), rastreado: true, ct);

            if (locacao == null)
            {
                _notificador.Add("Locação não encontrada");
                return false;
            }

            locacao.MarcarComoFalha(idPagamento);
            await _locacaoRepository.AtualizarSalvarAsync(locacao, ct);
            return true;
        }


        #endregion Pagamento

        #region Caucao
        public async Task<bool> AdicionarCalcaoAsync(int idLocacao, decimal valor, CancellationToken ct = default)
        {
            var locacao = await ObterLocacao(idLocacao, ct); ;
            if (locacao == null)
            {
                _notificador.Add("Locação não encontrada");
                return false;
            }

            locacao.RegistrarCaucao(valor);
            await _locacaoRepository.AtualizarSalvarAsync(locacao, ct);
            return true;
        }

        public async Task<bool> DevolverCalcaoAsync(int idLocacao, int idCaucao, CancellationToken ct = default)
        {
            var locacao = await ObterLocacao(idLocacao, ct); ;
            if (locacao == null)
            {
                _notificador.Add("Locação não encontrada");
                return false;
            }

            locacao.DevolverCaucao(idCaucao);
            await _locacaoRepository.AtualizarSalvarAsync(locacao, ct);
            return true;
        }

        public async Task<bool>BloquearCalcaoAsync(int idLocacao, int idCaucao, CancellationToken ct = default)
        {
            var locacao = await ObterLocacao(idLocacao, ct); ;
            if (locacao == null)
            {
                _notificador.Add("Locação não encontrada");
                return false;
            }
            locacao.BloquearCaucao(idCaucao);
            await _locacaoRepository.AtualizarSalvarAsync(locacao, ct);
            return true;
        }

        public async Task<bool> DeduzirCalcaoAsync(int idLocacao, int idCaucao,decimal valor, CancellationToken ct = default)
        {
            var locacao = await ObterLocacao(idLocacao, ct); ;
            if (locacao == null)
            {
                _notificador.Add("Locação não encontrada");
                return false;
            }
            locacao.DeduzirCaucao(idCaucao, valor);
            await _locacaoRepository.AtualizarSalvarAsync(locacao, ct);
            return true;
        }
        #endregion Caucao

        #region multas
        public async Task<bool> AdicionarMultaAsync(int idLocacao, CriarMultaDto dto, CancellationToken ct = default)
        {
            var locacao = await ObterLocacao(idLocacao, ct); ;
            if (locacao == null)
            {
                _notificador.Add("Locação não encontrada");
                return false;
            }
            locacao.AdicionarMulta((TipoMulta)dto.Tipo,dto.Valor);
            await _locacaoRepository.AtualizarSalvarAsync(locacao, ct);
            return true;
          
        }

        public async Task<bool> PagarMultaAsync(int idLocacao, int idMulta, CancellationToken ct = default)
        {
            var locacao = await ObterLocacao(idLocacao, ct); ;
            if (locacao == null)
            {
                _notificador.Add("Locação não encontrada");
                return false;
            }
            var multa = locacao.Multas.Where(m => m.IdMulta == idMulta).FirstOrDefault();
            if (multa == null)
            {
                _notificador.Add("Multa não encontrada");
                return false;
            }
            if (multa.Status != StatusMulta.Pendente)
            {
                _notificador.Add("Somente multas pendentes podem ser pagas");
                return false;
            }
            locacao.PagarMulta(idMulta);
            await _locacaoRepository.AtualizarSalvarAsync(locacao, ct);
            return true;

        }

        public async Task<bool> CancelarMultaAsync(int idLocacao, int idMulta, CancellationToken ct = default)
        {
            var locacao = await ObterLocacao(idLocacao, ct); ;
            if (locacao == null)
            {
                _notificador.Add("Locação não encontrada");
                return false;
            }
            var multa = locacao.Multas.Where(m => m.IdMulta == idMulta).FirstOrDefault();
            if (multa == null)
            {
                _notificador.Add("Multa não encontrada");
                return false;
            }
            if (multa.Status == StatusMulta.Paga)
            {
                _notificador.Add("Multa paga não pode ser cancelada, ja foi paga");
                return false;
            }

            locacao.CancelarMulta(idMulta);
            await _locacaoRepository.AtualizarSalvarAsync(locacao, ct);
            return true;

        }

        public async Task<bool> CompensarMultaAsync(int idLocacao, int idMulta,CancellationToken ct = default)
        {
            // 1. Buscar locação pelo repositório
            var locacao = await ObterLocacao(idLocacao,ct);
            if (locacao == null)
                _notificador.Add("Locação não encontrada");

            // 2. Delegar para o aggregate Locacao
            locacao.CompensarMultaComCaucao(idMulta);

            // 3. Persistir mudanças
            var atualiza =await _locacaoRepository.AtualizarSalvarAsync(locacao,ct);
            if(!atualiza)
            {
                _notificador.Add("Erro ao atualizar locação");
                return false;
            }
            return true;
        }
        #endregion multas

        #region Seguro
        public async Task<bool> AdicionarSeguroAsync(int idLocacao, int idSeguro, CancellationToken ct = default)
        {
            var locacao = await ObterLocacao(idLocacao, ct);
            if (locacao == null)
            {
                _notificador.Add("Locação não encontrada");
                return false;
            }
            var seguro = await _seguroRepository.ObterPorIdAsync(idSeguro, false, ct);
            if(seguro == null)
            {
                _notificador.Add("Seguro não encontrado");
                return false;
            }

            locacao.AdicionarSeguro(idSeguro); // substituir por busca real
            //locacao.AdicionarSeguro(locacaos);
            await _locacaoRepository.AtualizarSalvarAsync(locacao, ct);
            return true;
        }

        public async Task<bool> CancelarSeguroAsync(int idLocacao, int idLocacaoSeguro, CancellationToken ct = default)
        {
            var locacao = await ObterLocacao(idLocacao, ct);
            if (locacao == null)
            {
                _notificador.Add("Locação não encontrada");
                return false;
            }
            var locacaoSeguro = await _locacaoSeguroRepository.ObterPorIdAsync(idLocacaoSeguro, false, ct);
            if(locacaoSeguro == null)
            {
                _notificador.Add("Locação Seguro não encontrado");
                return false;
            }
            locacao.CancelarSeguro(idLocacaoSeguro); // substituir por busca real
            await _locacaoRepository.AtualizarSalvarAsync(locacao, ct);
            return true;
        }
        #endregion Seguro

        #region Leitura
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
        public async Task<IEnumerable<LocacaoDto>> ObterTodasAsync(CancellationToken ct = default)
        {
            var locacao = await _locacaoRepository.ObterAsync(
                incluir: q => q
                .Include(c => c.Veiculo)
                .Include(c => c.Cliente).ThenInclude(u => u.Usuario)
                .Include(c => c.Funcionario).ThenInclude(u => u.Usuario)
                .Include(c => c.Caucoes)
                .Include(m => m.Multas)
                .Include(m => m.Pagamentos)
                .Include(m => m.Seguros),
                rastreado: true);
            return locacao.ToDtoList();
        }
        private async Task<Locacao> ObterLocacao(int id, CancellationToken ct)
        {
            var locacao = await _locacaoRepository.ObterPrimeiroAsync(
                x => x.IdLocacao == id, 
                incluir: q => q
                .Include(c => c.Veiculo)
                .Include(c => c.Cliente).ThenInclude(u=>u.Usuario)
                .Include(c => c.Funcionario).ThenInclude(u => u.Usuario)
                .Include(c => c.Caucoes)
                .Include(m=>m.Multas)
                .Include(m => m.Pagamentos)
                .Include(m => m.Vistorias)
                .Include(m => m.Seguros),

                rastreado: true);
            return locacao!;
        }

        #endregion Leitura

        #region Vistoria

        public async Task<bool> RegistrarVistoriaAsync(int idLocacao, CriarVistoriaDto dto, CancellationToken ct = default)
        {
            var locacao = await ObterLocacao(idLocacao, ct);

            if (locacao == null)
            {
                _notificador.Add("Locação não encontrada");
                return false;
            }

            locacao.RegistrarVistoria(dto.IdFuncionario, (TipoVistoria)dto.Tipo,(NivelCombustivel)dto.NivelCombustivel,dto.KmVeiculo, dto.Observacoes);

            await _locacaoRepository.AtualizarSalvarAsync(locacao);
            return true;
        }

        public async Task<bool> RegistrarFotoVistoriaAsync(int id, EnviarFotoVistoriaDto dto, CancellationToken ct = default)
        {
            var locacao = await ObterLocacao(id, ct);
            if (locacao == null)
            {
                _notificador.Add("Locação não encontrada");
                return false;
            }
            var vistoria = locacao.Vistorias.Where(x=>x.IdVistoria == dto.IdVistoria).FirstOrDefault();
            if (vistoria == null)
            {
                _notificador.Add("Vistoria não encontrada");
                return false;
            }
            var fotos = await EnviarFoto(dto);
            locacao.RegistrarFoto(fotos,dto.IdVistoria);
            await _locacaoRepository.AtualizarSalvarAsync(locacao);
            return true;
        }

        public async Task<bool> RegistrarDanoVistoriaAsync(int id, CriarDanoDto dto, CancellationToken ct = default)
        {
            var locacao = await ObterLocacao(id, ct);
            if (locacao == null)
            {
                _notificador.Add("Locação não encontrada");
                return false;
            }
            var vistoria = locacao.Vistorias.FirstOrDefault(x => x.IdVistoria == dto.IdVistoria);
            if (vistoria == null)
            {
                _notificador.Add("Vistoria não encontrada");
                return false;
            }
            locacao.RegistrarDanoVistoria(dto.IdVistoria, dto.Descricao,(TipoDano)dto.codigoTipoDano, dto.ValorEstimado);
            return await _locacaoRepository.AtualizarSalvarAsync(locacao);
        }

        public async Task<bool> RemoverDanoVistoriaAsync(int id,RemoverDanoDto dto, CancellationToken ct = default)
        {
            var locacao = await ObterLocacao(id, ct);
            if (locacao == null)
            {
                _notificador.Add("Locação não encontrada");
                return false;
            }
            var vistoria = locacao.Vistorias.FirstOrDefault(x => x.IdVistoria == dto.IdVistoria);
            if (vistoria == null)
            {
                _notificador.Add("Vistoria não encontrada");
                return false;
            }
            var dano = vistoria.Danos.FirstOrDefault(x => x.IdDano == dto.IdDano);
            if (dano == null)
            {
                _notificador.Add("Dano não encontrado");
                return false;
            }

            locacao.RemoverDanoVistoria(dto.IdVistoria, dto.IdDano);
            return await _locacaoRepository.AtualizarSalvarAsync(locacao);
        }

        private async Task<List<FotoVistoria>> EnviarFoto(EnviarFotoVistoriaDto dto)
        {
            var documentosAnexos = new List<FotoVistoria>();
            foreach (var doc in dto.Fotos!)
            {
                var arquivo = await _uploadDownloadFileService.EnviarArquivoSimplesAsync(doc);
                if(arquivo != null)
                {
                    var fotoVistoria = FotoVistoria.Criar(      
                         //dto.IdVistoria,
                         arquivo.NomeArquivo,
                         arquivo.Raiz,
                         arquivo.Diretorio,
                         arquivo.Extensao,
                         arquivo.QuantidadeBytes.Value
                    );
                    documentosAnexos.Add(fotoVistoria);
                }               
            }
            return documentosAnexos;
        }


        #endregion Vistoria
    }
}
