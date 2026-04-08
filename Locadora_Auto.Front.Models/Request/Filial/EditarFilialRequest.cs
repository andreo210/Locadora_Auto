using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Locadora_Auto.Front.Models.Request.Filial
{
    public class EditarFilialRequest
    {
        public string? Nome { get; set; }
        public string? Cidade { get; set; }
        public EnderecoRequest? Endereco { get; set; }
    }
}
