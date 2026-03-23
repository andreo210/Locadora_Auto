using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Locadora_Auto.Front.Models.Response
{
    public class CategoriaResponse
    {
        public int Id { get; set; }
        public string? Nome { get; set; }
        public double? ValorDiaria { get; set; }
        public int? LimiteKm { get; set; }
        public double? ValorKmExcedente { get; set; }
        public int? Totalveiculos { get; set; }
    }    
}
