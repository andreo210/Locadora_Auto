using Newtonsoft.Json;

namespace Locadora_Auto.Infra.ServiceHttp.Models.Views.CadBase
{
    public class AtributoServiceView
    {
        [JsonProperty("codigo")]
        public int Codigo { get; set; }

        [JsonProperty("descricao")]
        public string? Descricao { get; set; }
    }
}
