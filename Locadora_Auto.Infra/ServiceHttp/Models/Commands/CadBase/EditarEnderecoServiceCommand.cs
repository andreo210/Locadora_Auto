using Newtonsoft.Json;

namespace Locadora_Auto.Infra.ServiceHttp.Models.Commands.CadBase
{
    public class EditarEnderecoServiceCommand
    {
        [JsonProperty("codigoCentral")]
        public int CodigoCentral { get; set; }

        [JsonProperty("cep")]
        public string? CEP { get; set; }

        [JsonProperty("bairro")]
        public string? Bairro { get; set; }

        [JsonProperty("cidade")]
        public string? Cidade { get; set; }

        [JsonProperty("uf")]
        public string? Uf { get; set; }

        [JsonProperty("codigoTipoLogradouro")]
        public int CodigoTipoLogradouro { get; set; }

        [JsonProperty("logradouro")]
        public string? Logradouro { get; set; }

        [JsonProperty("numero")]
        public string? Numero { get; set; }

        [JsonProperty("codigoTipoComplemento")]
        public int? CodigoTipoComplemento { get; set; }

        [JsonProperty("complemento")]
        public string? Complemento { get; set; }

        [JsonProperty("codigoTipoEndereco")]
        public int? CodigoTipoEndereco { get; set; }

        [JsonProperty("principal")]
        public bool Principal { get; set; }

        [JsonProperty("codigoServicoModificacao")]
        public int CodigoServicoModificacao { get; set; }
    }
}
