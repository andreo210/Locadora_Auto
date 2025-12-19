using Newtonsoft.Json;

namespace Locadora_Auto.Infra.ServiceHttp.Models.Views.CadBase
{
    public class PessoaFisicaReceitaServiceView
    {
        [JsonProperty("cpf")]
        public string?  Cpf{ get; set; }
        [JsonProperty("nome")]
        public string? Nome { get; set; }
        [JsonProperty("nomeMae")]
        public string? NomeMae { get; set; }
        [JsonProperty("dataNascimento")]
        public string? DataNascimento { get; set; }

        [JsonProperty("descricaoSexo")]
        public string? DescricaoSexo { get; set; }
        [JsonProperty("anoObito")]
        public string? AnoObito { get; set; }

    }
}
