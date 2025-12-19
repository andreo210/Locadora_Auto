using Newtonsoft.Json;

namespace Locadora_Auto.Infra.ServiceHttp.Models.Commands.CadBase
{
    public class CadastroPessoaFisicaServiceCommand
    {
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

        [JsonProperty("codigoRaca")]
        public int CodigoRaca { get; set; }

        [JsonProperty("codigoEstadoCivil")]
        public int CodigoEstadoCivil { get; set; }

        [JsonProperty("codigoSexoBiologico")]
        public int CodigoSexoBilogico { get; set; }

        [JsonProperty("codigoServicoCadastro")]
        public int CodigoServicoCadastrado { get; set; }
    }
}
