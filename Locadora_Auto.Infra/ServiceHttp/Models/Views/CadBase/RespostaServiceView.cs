using Newtonsoft.Json;
using System.Text.Json;

namespace Locadora_Auto.Infra.ServiceHttp.Models.Views.CadBase
{
    public class RespostaServiceView
    {
        [JsonProperty("codigo")]
        public int Codigo { get; set; }

        [JsonProperty("codigoCentral")]
        public int CodigoCentral { get; set; }

        [JsonProperty("pessoaFisica")]
        public PessoaFisicaServiceView? Pessoa { get; set; }
    }
}

