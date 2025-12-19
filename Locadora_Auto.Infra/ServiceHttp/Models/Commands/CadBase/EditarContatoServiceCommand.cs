using Newtonsoft.Json;

namespace Locadora_Auto.Infra.ServiceHttp.Models.Commands.CadBase
{
    public class EditarContatoServiceCommand
    {
        [JsonProperty("codigoCentral")]
        public int CodigoCentral { get; set; }

        [JsonProperty("contato")]
        public string? Contato { get; set; }

        [JsonProperty("codigoPaisContato")]
        public int CodigoPaisContato { get; set; }

        [JsonProperty("dddContato")]
        public int DDDContato { get; set; }

        [JsonProperty("numero")]
        public int Numero { get; set; }

        [JsonProperty("whatsApp")]
        public bool? WhatsApp { get; set; }

        [JsonProperty("telegram")]
        public bool? Telegram { get; set; }

        [JsonProperty("sms")]
        public bool? Sms { get; set; }

        [JsonProperty("codigoTipoContato")]
        public int? CodigoTipoContato { get; set; }

        [JsonProperty("codigoServicoModificacao")]
        public int CodigoServicoModificacao { get; set; }
    }
}
