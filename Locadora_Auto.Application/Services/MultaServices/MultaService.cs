using Locadora_Auto.Application.Configuration.Ultils.NotificadorServices;
using Locadora_Auto.Application.Mappers;
using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Domain.IRepositorio;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Locadora_Auto.Application.Services.MultaServices
{
    public class MultaService : IMultaService
    {
        private readonly INotificadorService _notificador;
        private readonly IMultaRepository _multaRepository;
        private readonly ILocacaoRepository _locacaoRepository;

        public MultaService(INotificadorService notificador, IMultaRepository multaRepository, ILocacaoRepository locacaoRepository)
        {
            _notificador = notificador;
            _multaRepository = multaRepository;
            _locacaoRepository = locacaoRepository;
        }

        public Task<IEnumerable<MultaDto>> ObterMultasPendentesAsync(CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<MultaDto>> ObterMultasPorLocacaoAsync(int idLocacao, CancellationToken ct = default)
        {
            var entidade = await _locacaoRepository.ObterPrimeiroAsync(l => l.IdLocacao == idLocacao, incluir: l => l.Include(l => l.Multas), rastreado: false, ct);
            if (entidade == null)
            {
                _notificador.Add("Locação não encontrada.");
                return Enumerable.Empty<MultaDto>();
            }
            var multasDto = entidade.Multas.Select(m => new MultaDto
            {
                IdMulta = m.IdMulta,
                Tipo = m.Tipo.ToString(),
                Valor = m.Valor
            });
            return multasDto;
        }

        public async Task<IEnumerable<MultaDto>> ObterMultasPorTipoAsync(int tipo, CancellationToken ct = default)
        {
            var entidade = await _multaRepository.ObterAsync(l => l.Tipo == (TipoMulta)tipo);
            if (entidade == null)
            {
                _notificador.Add("Locação não encontrada.");
                return Enumerable.Empty<MultaDto>();
            }
            var multasDto = entidade.Select(m => new MultaDto
            {
                IdMulta = m.IdMulta,
                Tipo = m.Tipo.ToString(),
                Valor = m.Valor
            });
            return multasDto;
        }

        public async Task<IEnumerable<MultaDto>> ObterMultasStatusAsync(int status, CancellationToken ct = default)
        {
            var locacoes = await ObterTodasLocacaoComMulta(ct);

             var multas = locacoes.SelectMany(l => l.Multas).Where(m => m.Status == (StatusMulta)status);

            //if (tipo.HasValue)
            //    multas = multas.Where(m => m.Tipo == tipo.Value);

            return multas.Select(m => new MultaDto
            {
                IdMulta = m.IdMulta,
                Tipo = m.Tipo.ToString(),
                Valor = m.Valor,
            });
        }

        private async Task<IEnumerable<Locacao>> ObterTodasLocacaoComMulta(CancellationToken ct=default)
        {
            var entidade = await _locacaoRepository.ObterAsync(l => l.Multas.Count > 0,incluir: m=>m.Include(m=>m.Multas));
            if (entidade.Count == 0)
            {
                return Enumerable.Empty<Locacao>();
            }
           
            return entidade;
        }
    }
}
