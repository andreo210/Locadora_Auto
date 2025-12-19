using Newtonsoft.Json;

namespace Locadora_Auto.Infra.ServiceHttp.Models.Views.CadBase
{
    public class PessoaFisicaDetalhadoServiceView
    {
        [JsonProperty("codigoCentral")]
        public int CodigoCentra { get; set; }
        [JsonProperty("cpf")]
        public string? CPF { get; set; }

        [JsonProperty("nome")]
        public string? Nome { get; set; }

        [JsonProperty("nomeSocial")]
        public string? NomeSocial { get; set; }

        [JsonProperty("dataNascimento")]
        public DateTime DataNascimento { get; set; }

        [JsonProperty("eMail")]
        public string? Email { get; set; }

        [JsonProperty("raca")]
        public AtributoServiceView? Raca { get; set; }

        [JsonProperty("estadoCivil")]
        public AtributoServiceView? EstadoCivil { get; set; }

        [JsonProperty("sexoBiologico")]
        public AtributoServiceView? SexoBiologico { get; set; }

        [JsonProperty("checarEmail")]
        public bool ChecarEmail { get; set; }

        [JsonProperty("rowVersion")]
        public string? RowVersion { get; set; }
    }
}
