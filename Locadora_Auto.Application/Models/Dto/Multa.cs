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
        public int Tipo { get; set; }
        public decimal Valor { get; set; }
    }
    public class CriarMultaDto
    {
        public int Tipo { get; set; }
        public decimal Valor { get; set; }
    }
    public class CompensarMultaDto
    {
        public int IdMulta { get; set; }
        public int IdLocacao { get; set; }
    }
}
