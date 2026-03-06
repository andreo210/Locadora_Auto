using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Locadora_Auto.Front.Models.Response
{
    public class ViaCepResponse
    {
        public string? Cep { get; set; }
        public string? Logradouro { get; set; }
        public string? Complemento { get; set; }
        public string? Bairro { get; set; }
        public string? Localidade { get; set; } // ViaCEP usa "localidade" para cidade
        public string? Uf { get; set; } // ViaCEP usa "uf" para estado
        public string? Ibge { get; set; }
        public string? Gia { get; set; }
        public string? Ddd { get; set; }
        public string? Siafi { get; set; }

        // Propriedade para verificar se retornou erro
        public bool Erro { get; set; }
    }
}
