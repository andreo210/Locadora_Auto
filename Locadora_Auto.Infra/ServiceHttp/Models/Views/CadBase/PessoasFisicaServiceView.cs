using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace Locadora_Auto.Infra.ServiceHttp.Models.Views.CadBase
{
    public class PessoaFisicaServiceView
    {
        [JsonProperty("codigoCentral")]
        public int CodigoCentral { get; set; }

        [JsonProperty("cpf")]
        public string? CPF { get; set; }

        [JsonProperty("nome")]
        public string? Nome { get; set; }

        [JsonProperty("rowVersion")]
        public string? RowVersion { get; set; }

    }
}
