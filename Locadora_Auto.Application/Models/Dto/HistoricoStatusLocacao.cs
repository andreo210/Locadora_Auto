using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Locadora_Auto.Application.Models.Dto
{
    public class HistoricoStatusLocacaoDto
    {
        public string Status { get; set; } = null!;
        public DateTime DataStatus { get; set; }
        public string Funcionario { get; set; } = null!;
    }

}
