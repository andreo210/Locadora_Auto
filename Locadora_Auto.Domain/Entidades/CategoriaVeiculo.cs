using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Locadora_Auto.Domain.Entidades
{
    public class CategoriaVeiculo
    {
        public int IdCategoria { get; set; }
        public string Nome { get; set; } = null!;
        public decimal ValorDiaria { get; set; }
        public int? LimiteKm { get; set; }
        public decimal? ValorKmExcedente { get; set; }

        public ICollection<Veiculo> Veiculos { get; set; } = [];
    }

}
