using Locadora_Auto.Application.Configuration.Ultils.NotificadorServices;
using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Application.Models.Mappers;
using Locadora_Auto.Domain;
using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Domain.IRepositorio;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Locadora_Auto.Application.Services.ReservaServices
{
    /// <summary>
    /// Reserva é entidade do agregado Cliente: <c>Reserva.Criar</c> é internal e a raiz
    /// (<c>Clientes</c>) é a única porta de entrada para criar e cancelar — mesma convenção de
    /// <c>Multa</c>, <c>Vistoria</c> e <c>Caucao</c> dentro de <c>Locacao</c>.
    ///
    /// Duas exceções deliberadas, ambas já existentes no projeto:
    /// <c>Finalizar</c> é disparado por <c>Locacao.Criar</c> (outro agregado) e a expiração em lote
    /// é uma varredura por tempo, que percorre reservas em vez de carregar todos os clientes.
    /// As consultas usam o <c>IReservaRepository</c> — leitura não atravessa invariante.
    /// </summary>
    public class ReservaService : IReservaService
    {
        private readonly IReservaRepository _reservaRepository;
        private readonly IClienteRepository _clienteRepository;
        private readonly ICategoriaVeiculosRepository _categoriaVeiculoRepository;
        private readonly IFilialRepository _filialRepository;
        private readonly IVeiculosRepository _veiculoRepository;
        private readonly ILocacaoRepository _locacaoRepository;
        private readonly INotificadorService _notificador;

        public ReservaService(
            IReservaRepository reservaRepository,
            IClienteRepository clienteRepository,
            ICategoriaVeiculosRepository categoriaVeiculoRepository,
            IFilialRepository filialRepository,
            IVeiculosRepository veiculoRepository,
            ILocacaoRepository locacaoRepository,
            INotificadorService notificador)
        {
            _reservaRepository = reservaRepository;
            _clienteRepository = clienteRepository;
            _categoriaVeiculoRepository = categoriaVeiculoRepository;
            _filialRepository = filialRepository;
            _veiculoRepository = veiculoRepository;
            _locacaoRepository = locacaoRepository;
            _notificador = notificador;
        }

        /// <summary>
        /// Traz cliente (com usuário), filial e categoria para o mapper preencher os nomes na listagem.
        /// </summary>
        private static Func<IQueryable<Reserva>, IQueryable<Reserva>> IncluirRelacionados =>
            q => q.Include(r => r.Cliente).ThenInclude(c => c.Usuario)
                  .Include(r => r.Filial)
                  .Include(r => r.CategoriaVeiculo);

        #region Consultas

        public async Task<ReservaDto?> ObterPorIdAsync(int id, CancellationToken ct = default)
        {
            var reserva = await _reservaRepository.ObterPrimeiroAsync(
                r => r.IdReserva == id,
                incluir: IncluirRelacionados,
                ct: ct);

            return reserva?.ToDto();
        }

        public async Task<PaginatedResult<ReservaDto>> ObterTodosPaginadoAsync(
            int pagina,
            int itensPorPagina,
            string? termo = null,
            int? status = null,
            int? idFilial = null,
            int? idCliente = null,
            string? ordenarPor = null,
            string? direcao = null,
            CancellationToken ct = default)
        {
            // No Postgres o LIKE é sensível a maiúsculas: comparar em minúsculas dos dois lados
            var busca = string.IsNullOrWhiteSpace(termo) ? null : termo.Trim().ToLower();

            // Status é gravado com HasConversion<int>: comparar como enum, não com cast dentro da expressão
            var situacao = status.HasValue ? (StatusReserva?)status.Value : null;

            Expression<Func<Reserva, bool>> filtro = r =>
                (busca == null
                    || (r.Cliente.Usuario != null && r.Cliente.Usuario.NomeCompleto!.ToLower().Contains(busca))
                    || r.Filial.Nome.ToLower().Contains(busca)
                    || r.CategoriaVeiculo.Nome!.ToLower().Contains(busca))
                && (situacao == null || r.Status == situacao.Value)
                && (idFilial == null || r.IdFilial == idFilial)
                && (idCliente == null || r.IdCliente == idCliente);

            var reservas = await _reservaRepository.ObterPaginadoComFiltroAsync(
                filtro: filtro,
                ordenarPor: MontarOrdenacao(ordenarPor, direcao),
                incluir: IncluirRelacionados,
                pagina: pagina,
                itensPorPagina: itensPorPagina,
                asNoTracking: true,
                ct: ct);

            return new PaginatedResult<ReservaDto>
            {
                Items = reservas.Items.ToDtoList(),
                Total = reservas.Total,
                Pagina = reservas.Pagina,
                TotalPaginas = reservas.TotalPaginas,
                ItensPorPagina = reservas.ItensPorPagina
            };
        }

        /// <summary>
        /// Traduz a coluna clicada na tela para o OrderBy correspondente.
        /// Coluna desconhecida cai na data de início, da mais recente para a mais antiga.
        /// </summary>
        private static Func<IQueryable<Reserva>, IOrderedQueryable<Reserva>> MontarOrdenacao(string? ordenarPor, string? direcao)
        {
            var descendente = string.Equals(direcao, "desc", StringComparison.OrdinalIgnoreCase);

            return (ordenarPor?.ToLower()) switch
            {
                "nomecliente" => q => descendente
                    ? q.OrderByDescending(r => r.Cliente.Usuario!.NomeCompleto)
                    : q.OrderBy(r => r.Cliente.Usuario!.NomeCompleto),
                "nomefilial" => q => descendente
                    ? q.OrderByDescending(r => r.Filial.Nome)
                    : q.OrderBy(r => r.Filial.Nome),
                "nomecategoriaveiculo" => q => descendente
                    ? q.OrderByDescending(r => r.CategoriaVeiculo.Nome)
                    : q.OrderBy(r => r.CategoriaVeiculo.Nome),
                "datafim" => q => descendente ? q.OrderByDescending(r => r.DataFim) : q.OrderBy(r => r.DataFim),
                "status" => q => descendente ? q.OrderByDescending(r => r.Status) : q.OrderBy(r => r.Status),
                "ativo" => q => descendente ? q.OrderByDescending(r => r.Ativo) : q.OrderBy(r => r.Ativo),
                "datainicio" => q => descendente ? q.OrderByDescending(r => r.DataInicio) : q.OrderBy(r => r.DataInicio),
                _ => q => q.OrderByDescending(r => r.DataInicio)
            };
        }

        public async Task<IReadOnlyList<ReservaDto>> ObterPorClienteAsync(int idCliente, CancellationToken ct = default)
        {
            var reservas = await _reservaRepository.ObterAsync(
                filtro: r => r.IdCliente == idCliente,
                ordenarPor: q => q.OrderByDescending(r => r.DataInicio),
                incluir: IncluirRelacionados,
                ct: ct);

            return reservas.ToDtoList();
        }

        #endregion Consultas

        #region Gravacao

        public async Task<ReservaDto?> CriarAsync(CriarReservaDto dto, CancellationToken ct = default)
        {
            // as datas precisam estar em UTC antes de qualquer comparação: a entidade valida contra UtcNow
            var inicio = NormalizarUtc(dto.DataInicio);
            var fim = NormalizarUtc(dto.DataFim);

            // a raiz do agregado precisa vir rastreada: é ela que cria a reserva
            var cliente = await _clienteRepository.ObterPrimeiroAsync(
                c => c.IdCliente == dto.IdCliente,
                rastreado: true,
                ct: ct);

            if (cliente == null)
                _notificador.Add($"Cliente com ID {dto.IdCliente} não encontrado.");
            else if (!cliente.Ativo)
                _notificador.Add("Cliente inativo não pode realizar reservas.");

            if (!await _categoriaVeiculoRepository.ExisteAsync(c => c.Id == dto.IdCategoriaVeiculo, ct))
                _notificador.Add($"Categoria de veículo com ID {dto.IdCategoriaVeiculo} não encontrada.");

            if (!await _filialRepository.ExisteAsync(f => f.IdFilial == dto.IdFilial, ct))
                _notificador.Add($"Filial com ID {dto.IdFilial} não encontrada.");

            if (inicio <= DateTime.UtcNow)
                _notificador.Add("Data de início da reserva não pode ser menor ou igual à data atual.");

            if (fim <= inicio)
                _notificador.Add("Data de entrega da reserva não pode ser menor que a data de início.");

            // só vale checar disponibilidade se categoria, filial e período já passaram
            if (!_notificador.TemNotificacao())
                await ValidarDisponibilidade(dto, inicio, fim, ct);

            if (_notificador.TemNotificacao()) return null;

            cliente!.ReservarVeiculo(cliente.IdCliente, inicio, fim, dto.IdFilial, dto.IdCategoriaVeiculo);
            await _clienteRepository.SalvarAsync(ct);

            // o cliente foi carregado sem as reservas: a coleção contém apenas a que acabou de ser criada
            var criada = cliente.Reservas.OrderByDescending(r => r.IdReserva).FirstOrDefault();
            if (criada == null) return null;

            return await ObterPorIdAsync(criada.IdReserva, ct);
        }

        public async Task<bool> CancelarAsync(int id, CancellationToken ct = default)
        {
            // cancelar é operação da raiz: carrega o cliente dono da reserva com a coleção rastreada
            var cliente = await _clienteRepository.ObterPrimeiroAsync(
                c => c.Reservas.Any(r => r.IdReserva == id),
                incluir: q => q.Include(c => c.Reservas),
                rastreado: true,
                ct: ct);

            var reserva = cliente?.Reservas.FirstOrDefault(r => r.IdReserva == id);
            if (reserva == null)
            {
                _notificador.Add($"Reserva com ID {id} não encontrada.");
                return false;
            }

            // a entidade lança DomainException nesse caso: aqui a falha de regra vira notificação
            if (reserva.Status != StatusReserva.Reservado)
            {
                _notificador.Add($"Somente reservas ativas podem ser canceladas. Situação atual: {reserva.Status}.");
                return false;
            }

            cliente!.CancelarReservar(reserva);
            await _clienteRepository.SalvarAsync(ct);
            return true;
        }

        /// <summary>
        /// Contrapartida manual do que <c>Locacao.Criar</c> faz ao abrir uma locação vinda de reserva:
        /// por isso age direto na entidade, como o agregado Locacao já faz.
        /// </summary>
        public async Task<bool> FinalizarAsync(int id, CancellationToken ct = default)
        {
            var reserva = await _reservaRepository.ObterPrimeiroAsync(r => r.IdReserva == id, rastreado: true, ct: ct);
            if (reserva == null)
            {
                _notificador.Add($"Reserva com ID {id} não encontrada.");
                return false;
            }

            if (reserva.Status != StatusReserva.Reservado)
            {
                _notificador.Add($"Somente reservas ativas podem ser finalizadas. Situação atual: {reserva.Status}.");
                return false;
            }

            reserva.Finalizar();
            await _reservaRepository.SalvarAsync(ct);
            return true;
        }

        public async Task<int> ExpirarVencidasAsync(CancellationToken ct = default)
        {
            var agora = DateTime.UtcNow;

            var candidatas = await _reservaRepository.ObterAsync(
                filtro: r => r.Status == StatusReserva.Reservado && r.DataInicio < agora,
                rastreado: true,
                ct: ct);

            if (candidatas.Count == 0) return 0;

            // Expirar só age quando a data de início ficou para trás em dias inteiros:
            // conferir o status depois da chamada é o que dá a contagem real
            var expiradas = 0;
            foreach (var reserva in candidatas)
            {
                reserva.Expirar(agora);
                if (reserva.Status == StatusReserva.Expirado) expiradas++;
            }

            if (expiradas > 0)
                await _reservaRepository.SalvarAsync(ct);

            return expiradas;
        }

        #endregion Gravacao

        #region Validações e Regras de Negócio

        /// <summary>
        /// A frota da categoria naquela filial precisa cobrir as locações em aberto mais as reservas
        /// cujo período se sobrepõe ao solicitado.
        /// </summary>
        private async Task ValidarDisponibilidade(CriarReservaDto dto, DateTime inicio, DateTime fim, CancellationToken ct)
        {
            var veiculosDisponiveis = await _veiculoRepository.ContarAsync(
                v => v.IdCategoria == dto.IdCategoriaVeiculo
                     && v.FilialAtualId == dto.IdFilial
                     && v.Disponivel, ct);

            if (veiculosDisponiveis == 0)
            {
                _notificador.Add("Não há veículos disponíveis para a categoria e filial selecionadas.");
                return;
            }

            var locacoesAbertas = await _locacaoRepository.ContarAsync(
                l => l.Veiculo.IdCategoria == dto.IdCategoriaVeiculo
                     && l.Veiculo.FilialAtualId == dto.IdFilial
                     && l.Status != StatusLocacao.Finalizada, ct);

            var reservasNoPeriodo = await _reservaRepository.ContarAsync(
                r => r.IdCategoria == dto.IdCategoriaVeiculo
                     && r.IdFilial == dto.IdFilial
                     && r.Status == StatusReserva.Reservado
                     && r.DataInicio < fim
                     && r.DataFim > inicio, ct);

            if (veiculosDisponiveis <= locacoesAbertas + reservasNoPeriodo)
                _notificador.Add("Não há veículos disponíveis para a categoria e filial selecionadas no período informado.");
        }

        /// <summary>
        /// Datas vindas de DTO chegam como Unspecified. Segue a mesma regra do conversor global do
        /// LocadoraDbContext: Local vira UTC, Unspecified é remarcado como UTC.
        /// </summary>
        private static DateTime NormalizarUtc(DateTime data) => data.Kind switch
        {
            DateTimeKind.Utc => data,
            DateTimeKind.Local => data.ToUniversalTime(),
            _ => DateTime.SpecifyKind(data, DateTimeKind.Utc)
        };

        #endregion Validações e Regras de Negócio
    }
}
