using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Locadora_Auto.Application.Models.Dto
{
    public class MultaDto
    {
        public int IdMulta { get; set; }
        public string Tipo { get; set; } = null!;
        public decimal Valor { get; set; }
        public string Status { get; set; } = null!;
    }

}
