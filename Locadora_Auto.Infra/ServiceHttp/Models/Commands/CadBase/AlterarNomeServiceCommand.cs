using Newtonsoft.Json;

namespace Locadora_Auto.Infra.ServiceHttp.Models.Commands.CadBase
{
    public class AlterarNomeServiceCommand
    {
        [JsonProperty("nome")]
        public string? Nome { get; set; }

        [JsonProperty("motivoModificacao")]
        public string? MotivoModificacao { get; set; }

        [JsonProperty("codigoServicoModificacao")]
        public int CodigoServicoModificacao { get; set; }

        [JsonProperty("rowVersion")]
        public string? RowVersion { get; set; }
    }
}
