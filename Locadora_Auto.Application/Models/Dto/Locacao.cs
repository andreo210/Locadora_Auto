using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Locadora_Auto.Application.Models.Dto
{
    public class LocacaoCreateDto
    {
        public int IdCliente { get; set; }
        public int IdVeiculo { get; set; }
        public int IdFuncionario { get; set; }
        public int IdFilialRetirada { get; set; }
        public DateTime DataInicio { get; set; }
        public DateTime DataFimPrevista { get; set; }
        public List<LocacaoAdicionalDto>? Adicionais { get; set; }
    }

    public class LocacaoDto
    {
        public int IdLocacao { get; set; }
        public ClienteDto Cliente { get; set; } = null!;
        public VeiculoDto Veiculo { get; set; } = null!;
        public DateTime DataInicio { get; set; }
        public DateTime DataFimPrevista { get; set; }
        public DateTime? DataFimReal { get; set; }
        public decimal ValorFinal { get; set; }
        public string Status { get; set; } = null!;
    }

    public class FecharLocacaoDto
    {
        public int KmFinal { get; set; }
        public DateTime DataFimReal { get; set; }
        public List<MultaDto>? Multas { get; set; }
        public List<DanoDto>? Danos { get; set; }
    }

    public class ResumoLocacoesDto
    {
        public int TotalAtivas { get; set; }
        public int TotalAtrasadas { get; set; }
        public decimal FaturamentoMes { get; set; }
    }

    public class LocacaoAdicionalDto
    {
        public int IdAdicional { get; set; }
        public int Quantidade { get; set; }
    }

}
