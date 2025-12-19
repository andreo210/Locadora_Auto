using Locadora_Auto.Infra.Exceptions;
using Newtonsoft.Json;
using System.Text;
using System.Text.Json;

namespace Locadora_Auto.Infra.ServiceHttp.Servicos
{
    public abstract class HttpService
    {
        protected StringContent? ObterConteudo(object dado)
        {
            var json = JsonConvert.SerializeObject(dado);
            var data = new StringContent(json, Encoding.UTF8, "application/json");
            return data;
        }

        protected async Task<T> DeserializarObjeto<T>(HttpResponseMessage resposta)
        {
            string content = await resposta.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(content);
        }

        protected async Task<T> DeserializarObjetoResponse<T>(HttpResponseMessage responseMessage)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            return System.Text.Json.JsonSerializer.Deserialize<T>(await responseMessage.Content.ReadAsStringAsync(), options);
        }

        protected bool TratarErrosResponse(HttpResponseMessage response)
        {
            switch ((int)response.StatusCode)
            {
                case 401:
                    throw new CustomHttpRequestException("Token invalido", response.StatusCode);
                case 403:
                    throw new CustomHttpRequestException("acesso negado", response.StatusCode);
                case 404:
                    return false;
                case 500:
                    throw new CustomHttpRequestException(response.Content.ReadAsStringAsync().Result, response.StatusCode);

                case 400:
                    return false;
            }
            response.EnsureSuccessStatusCode();
            return true;
        }

    }
}
