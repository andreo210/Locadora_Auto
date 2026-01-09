using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Locadora_Auto.Domain.Entidades
{
    public class Locacao
    {
        public int IdLocacao { get; set; }

        public int IdCliente { get; set; }
        public int IdVeiculo { get; set; }
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

        public Clientes Cliente { get; set; } = null!;
        public Veiculo Veiculo { get; set; } = null!;
        public Funcionario Funcionario { get; set; } = null!;

        public ICollection<Pagamento> Pagamentos { get; set; } = [];
        public ICollection<Multa> Multas { get; set; } = [];
        public ICollection<LocacaoSeguro> Seguros { get; set; } = new List<LocacaoSeguro>();

    }

}
