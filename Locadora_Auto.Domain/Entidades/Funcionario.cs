using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Locadora_Auto.Domain.Entidades
{
    public class Funcionario
    {
        public int IdFuncionario { get; set; }
        public string Matricula { get; set; } = null!;
        public string Nome { get; set; } = null!;
        public string? Cargo { get; set; }
        public string Status { get; set; } = "ATIVO";

        public ICollection<Locacao> Locacoes { get; set; } = [];
    }

}
