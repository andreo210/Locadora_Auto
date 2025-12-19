using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Locadora_Auto.Domain.Entidades
{
    public class Pagamento
    {
        public int IdPagamento { get; set; }
        public int IdLocacao { get; set; }
        public int IdFormaPagamento { get; set; }
        public decimal Valor { get; set; }
        public DateTime DataPagamento { get; set; }
        public string Status { get; set; } = null!;

        public Locacao Locacao { get; set; } = null!;
        public FormaPagamento FormaPagamento { get; set; } = null!;
    }

}
