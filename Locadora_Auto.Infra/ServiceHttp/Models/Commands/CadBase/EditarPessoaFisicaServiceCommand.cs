using Newtonsoft.Json;

namespace Locadora_Auto.Infra.ServiceHttp.Models.Commands.CadBase
{
    public class EditarPessoaFisicaServiceCommand
    {
        [JsonProperty("dataNascimento")]
        public DateTime? DataNascimento { get; set; }


        [JsonProperty("dataObito")]
        public DateTime? DataObito { get; set; }


        [JsonProperty("eMail")]
        public string? Email { get; set; }

        [JsonProperty("nomeSocial")]
        public string? NomeSocial { get; set; }


        [JsonProperty("codigoRaca")]
        public int? CodigoRaca { get; set; }


        [JsonProperty("codigoEstadoCivil")]
        public int? CodigoEstadoCivil { get; set; }


        [JsonProperty("codigoSexoBiologico")]
        public int? CodigoSexoBiologico { get; set; }


        [JsonProperty("codigoServicoModificacao")]
        public int? CodigoServicoModificacao { get; set; }


        [JsonProperty("motivoModificacao")]
        public string? MotivoModificacao { get; set; }

        [JsonProperty("rowVersion")]
        public string? RowVersion { get; set; }


        [JsonProperty("statusMerge")]
        public string? StatusMerge { get; set; }
    }
}
