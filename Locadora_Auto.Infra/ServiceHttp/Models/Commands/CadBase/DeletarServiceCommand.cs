using Newtonsoft.Json;

namespace Locadora_Auto.Infra.ServiceHttp.Models.Commands.CadBase
{
    public class DeletarServiceCommand
    {
        [JsonProperty("codigoCentral")]
        public int CodigoCentral { get; set; }

        [JsonProperty("codigoServicoModificacao")]
        public int CodigoServicoModificacao { get; set; }

    }
}
