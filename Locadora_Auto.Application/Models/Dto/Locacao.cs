using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Locadora_Auto.Application.Models.Dto
{
    public class LocacaoDto
    {
        public int IdLocacao { get; set; }
        //public int IdCliente { get; set; }
        //public int IdVeiculo { get; set; }
        public int IdFuncionario { get; set; }
        public int IdFilialRetirada { get; set; }
        public int? IdFilialDevolucao { get; set; }
        public DateTime DataInicio { get; set; }
        public DateTime DataFimPrevista { get; set; }
        public DateTime? DataFimReal { get; set; }
        public int KmInicial { get; set; }
        public int? KmFinal { get; set; }
        public decimal ValorPrevisto { get; set; }
        public decimal? ValorFinal { get; set; }
        public string Status { get; set; } = null!;
        public ClienteDto ClienteDto { get; set; } = null!;
        public FuncionarioDto FuncionarioDto { get; set; } = null!;
        public List<MultaDto> Multas { get; set; } = new List<MultaDto>();
        public List<PagamentoDto> Pagamentos { get; set; } = new List<PagamentoDto>();
    }

    public class CriarLocacaoDto
    {
        public int IdCliente { get; set; }
        public int IdVeiculo { get; set; }
        public int IdFuncionario { get; set; }
        public int IdFilialRetirada { get; set; }
        public DateTime DataInicio { get; set; }
        public DateTime DataFimPrevista { get; set; }
        public int KmInicial { get; set; }
        public decimal ValorPrevisto { get; set; }
    }

    public class AtualizarLocacaoDto
    {
        public int IdFuncionario { get; set; }
        public int? IdFilialDevolucao { get; set; }
        public DateTime? DataFimReal { get; set; }
        public int? KmFinal { get; set; }
        public decimal? ValorFinal { get; set; }
        public string? Status { get; set; }
        public DateTime DataInicio { get; set; }
        public DateTime DataFimPrevista { get; set; }
        public int KmInicial { get; set; }
        public decimal ValorPrevisto { get; set; }
    }
    public class FinalizarLocacaoDto
    {
        public int Id { get; set; }
        public int IdFilialDevolucao { get; set; }
        public DateTime DataFimReal { get; set; }
        public int KmFinal { get; set; }
        public decimal ValorFinal { get; set; }
    }

}
