using Locadora_Auto.Infra.Configuration;
using Locadora_Auto.Infra.ServiceHttp.Models.Views.CadBase;
using Locadora_Auto.Infra.ServiceHttp.Servicos;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Headers;

namespace Locadora_Auto.Infra.ServiceHttp.Servicos.CadastroBase.CadastroBaseRead
{
    public class CadastroBaseReadService : HttpService, ICadastroBaseReadService
    {
        private readonly HttpClient _httpClient;
        private readonly ApiConfig _UrlApi;

        public CadastroBaseReadService(HttpClient httpClient, IOptions<ApiConfig> urlApi)
        {
            _UrlApi = urlApi.Value;
           // httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ValuesConfig.KEYCLOAKAUTHINTERNO.access_token);
            httpClient.BaseAddress = new Uri(_UrlApi.BaseUrlCadastroBaseRead);
            _httpClient = httpClient;
        }

        //pessoa fisica
        public async Task<(string? mensagem, HttpStatusCode? status, PessoaFisicaServiceView? usuario)> ObterPessoaFisicaBasicoPorId(int codigoCentral)
        {            
            var resposta = await _httpClient.GetAsync($"DadosBiograficos/{codigoCentral}");
            if (TratarErrosResponse(resposta))
            {
                var usuario = await DeserializarObjeto<PessoaFisicaServiceView>(resposta);
                return ("OK", resposta.StatusCode, usuario);
            }
            return ("ERRO", resposta.StatusCode, null);
        }
        public async Task<(string? mensagem, HttpStatusCode? statusCode, PessoaFisicaDetalhadoServiceView? usuario)> ObterPessoaFisicaDetahadoPorId(int codigoCentral)
        {            
            var resposta = await _httpClient.GetAsync($"DadosBiograficos/{codigoCentral}/Dados");
            if (TratarErrosResponse(resposta))
            {
                var usuario = await DeserializarObjeto<PessoaFisicaDetalhadoServiceView>(resposta); 
                return ("OK", resposta.StatusCode, usuario);
            }
            return ("ERRO", resposta.StatusCode, null);           
        }
        public async Task<(string? mensagem, HttpStatusCode? status, PessoaFisicaServiceView? usuario)> ObterPessoaFisicaBasicoPorCpf(string cpf)
        {            
            var resposta = await _httpClient.GetAsync($"DadosBiograficos/PorCPF/{cpf}");
            if (TratarErrosResponse(resposta))
            {
                var usuario = await DeserializarObjeto<PessoaFisicaServiceView>(resposta);
                return ("OK", resposta.StatusCode, usuario);
            }
            return ("ERRO", resposta.StatusCode, null);            
        }
        public async Task<(string? mensagem, HttpStatusCode? statusCode, PessoaFisicaDetalhadoServiceView? usuario)> ObterPessoaFisicaDetahadoPorCpf(string cpf)
        {            
            var resposta = await _httpClient.GetAsync($"DadosBiograficos/PorCPF/{cpf}/Dados");
            var resp = resposta.Content.ReadAsStringAsync().Result;
            if (TratarErrosResponse(resposta))
            {
                var usuario = await DeserializarObjeto<PessoaFisicaDetalhadoServiceView>(resposta);
                return ("OK", resposta.StatusCode, usuario);
            }
            return (resposta.Content.ReadAsStringAsync().Result, resposta.StatusCode, null);            
        }

        public async Task<(string? mensagem, HttpStatusCode? statusCode, PessoaFisicaReceitaServiceView? usuario)> ObterPessoaFisicaReceitaFederal(string cpf)
        {
            var resposta = await _httpClient.GetAsync($"/ReceitaFederal/{cpf}");
            if (TratarErrosResponse(resposta))
            {
                var usuario = await DeserializarObjeto<PessoaFisicaReceitaServiceView>(resposta);
                return ("OK", resposta.StatusCode, usuario);
            }
            return ("ERROR", resposta.StatusCode, null);
        }



        //contato
        public async Task<(string? mensagem, HttpStatusCode? status, List<ContatoServiceView>? contato)> ObterContatoPorCodigoCentral(int? codigoCentral)
        {            
            var retorno = await _httpClient.GetAsync($"DadosBiograficosContato/PorCentral/{codigoCentral}");
            if (TratarErrosResponse(retorno))
            {
                var contatos = await DeserializarObjeto<List<ContatoServiceView>>(retorno);
                return ("OK", retorno.StatusCode, contatos);
            }
            return ("ERRO", retorno.StatusCode, null);
        }
        public async Task<(string? mensagem, HttpStatusCode? status, ContatoServiceView? contato)> ObterContatoPorCodigo(int? codigo)
        {            
            var retorno = await _httpClient.GetAsync($"DadosBiograficosContato/{codigo}");
            if (TratarErrosResponse(retorno))
            {
                var contatos = await DeserializarObjeto<ContatoServiceView>(retorno);
                return ("OK", retorno.StatusCode, contatos);
            }            
            return ("ERRO", retorno.StatusCode, null);    
        }


        //endereco
        public async Task<(string? mensagem, HttpStatusCode? status, List<EnderecoServiceView>? endereco)> ObterEnderecoPorCodigoCentral(int? codigoCentral)
        {
            var retorno = await _httpClient.GetAsync($"DadosBiograficosEndereco/PorCentral/{codigoCentral}");
            if (retorno.StatusCode == HttpStatusCode.InternalServerError)
            {
                return ("NÃO ENCONTRADO", retorno.StatusCode, null);
            }

            if (TratarErrosResponse(retorno))
            {
                var endereco = await DeserializarObjeto<List<EnderecoServiceView>>(retorno);
                return ("OK", retorno.StatusCode, endereco);
            }            
            return ("ERRO", retorno.StatusCode, null);                     
        }
        public async Task<(string? mensagem, HttpStatusCode? status, EnderecoServiceView? endereco)> ObterEnderecoPorCodigo(int? codigo)
        {            
            var retorno = await _httpClient.GetAsync($"DadosBiograficosEndereco/{codigo}");
            if (TratarErrosResponse(retorno))
            {
                var endereco = await DeserializarObjeto<EnderecoServiceView>(retorno);
                return ("OK", retorno.StatusCode, endereco);
            }            
            return ("ERRO", retorno.StatusCode, null);            
        }
        public async Task<(string? mensagem, HttpStatusCode? status, List<EnderecoServiceView>? endereco)> ObterEnderecoPorCpf(string? cpf)
        {
           var retorno = await _httpClient.GetAsync($"DadosBiograficosEndereco/PorCpf/{cpf}");
            if (TratarErrosResponse(retorno))
            {
                var endereco = await DeserializarObjeto<List<EnderecoServiceView>>(retorno);
                return ("OK", retorno.StatusCode, endereco);
            }            
            return ("ERRO", retorno.StatusCode, null);        
        }
    }
}
