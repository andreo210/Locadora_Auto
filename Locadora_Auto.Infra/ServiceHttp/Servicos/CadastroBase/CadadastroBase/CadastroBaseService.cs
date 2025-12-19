using Locadora_Auto.Infra.Configuration;
using Locadora_Auto.Infra.ServiceHttp.Models.Commands.CadBase;
using Locadora_Auto.Infra.ServiceHttp.Models.Views.CadBase;
using Locadora_Auto.Infra.ServiceHttp.Servicos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net;

namespace Locadora_Auto.Infra.ServiceHttp.Servicos.CadastroBase.CadadastroBase
{
    public class CadastroBaseService : HttpService, ICadastroBaseService
    {
        private readonly HttpClient _httpClient;
        private readonly ApiConfig _UrlApi;
        private int _CodigoServico;

        public CadastroBaseService(HttpClient httpClient, IOptions<ApiConfig> urlApi)
        {
           _UrlApi = urlApi.Value;
            httpClient.BaseAddress = new Uri(_UrlApi.BaseUrlCadastroBase);
            _httpClient = httpClient;
            _CodigoServico = Convert.ToInt32(urlApi.Value.CodigoServicoCadastroBase);
        }

        //pessoa fisica
        public async Task<(string? mensagem, HttpStatusCode? status, PessoaFisicaServiceView? usuario)> CadastrarUsuario(CadastroPessoaFisicaServiceCommand user)
        {            
            user.CodigoServicoCadastrado = _CodigoServico;
            user.CodigoRaca = 1;
            user.CodigoEstadoCivil = 1;
            user.CodigoSexoBilogico = 1;

            var data = ObterConteudo(user);
            var resultado = await _httpClient.PostAsync($"DadosBiograficos", data);
            var resposta = resultado.Content.ReadAsStringAsync().Result;
            if (resposta.Contains("CPF não encontrado na receita federal!"))
            {
                return ("CPF não encontrado!", HttpStatusCode.BadRequest, null);
            }
            if (TratarErrosResponse(resultado))
            {
                var usuario = await DeserializarObjeto<PessoaFisicaServiceView>(resultado);
                return ("OK", resultado.StatusCode, usuario);
            }
            else
            {
                var erro = await DeserializarObjeto<ValidationProblemDetails>(resultado);
                return (erro.Title, resultado.StatusCode, null);
            }            
        }
        public async Task<(string? mensagem, HttpStatusCode? status, PessoaFisicaServiceView? usuario)> EditarUsuario(EditarPessoaFisicaServiceCommand usuario, int? CodigoCentral)
        {            
            usuario.CodigoServicoModificacao = _CodigoServico;
            var dados = ObterConteudo(usuario);
            var resposta = await _httpClient.PutAsync($"DadosBiograficos/{CodigoCentral}", dados);
            var res = resposta.Content.ReadAsStringAsync().Result;
            if (TratarErrosResponse(resposta))
            {
                var user = await DeserializarObjeto<PessoaFisicaServiceView>(resposta);
                return ("OK", resposta.StatusCode, user);
            }
            else
            {
                var erro = await DeserializarObjeto<ValidationProblemDetails>(resposta);
                return (erro.Errors.FirstOrDefault().Value[0].ToString(), resposta.StatusCode, null);
            }            
        }
        public async Task<(string? mensagem, HttpStatusCode? status, PessoaFisicaServiceView? usuario)> EditarNomeUsuario(AlterarNomeServiceCommand usuario, int? CodigoCentral)
        {            
            usuario.CodigoServicoModificacao = _CodigoServico;
            var dados = ObterConteudo(usuario);
            var resposta = await _httpClient.PatchAsync($"DadosBiograficos/{CodigoCentral}", dados);
            if (TratarErrosResponse(resposta))
            {
                var user = await DeserializarObjeto<PessoaFisicaServiceView>(resposta);
                return ("OK", resposta.StatusCode, user);
            }
            else
            {
                var erro = await DeserializarObjeto<ValidationProblemDetails>(resposta);
                return (resposta.Content.ReadAsStringAsync().Result, resposta.StatusCode, null);
            }            
        }


        //contato
        public async Task<(string? mensagem, HttpStatusCode? status, RespostaServiceView? contato)> CadastrarContato(CadastrarContatoServiceCommand contatos)
        {
            try
            {
                contatos.CodigoServicoCadastro = _CodigoServico;
                var data = ObterConteudo(contatos);
                var resposta = _httpClient.PostAsync($"PessoaFisicaContato", data).Result;
                var retorno = resposta.Content.ReadAsStringAsync().Result;

                if (TratarErrosResponse(resposta))
                {
                    var user = await DeserializarObjeto<RespostaServiceView>(resposta);
                    return ("OK", resposta.StatusCode, user);
                }
                else
                {
                    var erro = await DeserializarObjeto<ValidationProblemDetails>(resposta);
                    return (erro.Title, resposta.StatusCode, null);
                }
            }catch (Exception ex)
            {
                return (ex.Message, HttpStatusCode.InternalServerError, null);
            }
        }
        public async Task<(string? mensagem, HttpStatusCode? status, RespostaServiceView? contato)> EditarContato(EditarContatoServiceCommand contatos, int Codigo)
        {
            try
            {
                contatos.CodigoServicoModificacao = _CodigoServico;
                var data = ObterConteudo(contatos);
                var resposta = await _httpClient.PutAsync($"PessoaFisicaContato/{Codigo}/Modificar", data);
                var retorno = resposta.Content.ReadAsStringAsync().Result;
                //if (retorno.Contains("Contato já cadastrado!"))
                //{
                //    return ("Contato já cadastrado !", HttpStatusCode.BadRequest, null);
                //}
                if (TratarErrosResponse(resposta))
                {
                    var user = await DeserializarObjeto<RespostaServiceView>(resposta);
                    return ("OK", resposta.StatusCode, user);
                }
                else
                {
                    var erro = await DeserializarObjeto<ValidationProblemDetails>(resposta);
                    return (erro.Title, resposta.StatusCode, null);
                }
            } catch (Exception ex)
            {
                return (ex.Message, HttpStatusCode.InternalServerError, null);
            }
           
        }
        public async Task<(string? mensagem, HttpStatusCode? status, RespostaServiceView? contato)> DeletarContato(DeletarServiceCommand contato, int? codigo)
        {
            try
            {
                contato.CodigoServicoModificacao = _CodigoServico;
                var data = ObterConteudo(contato);
                var resposta = await _httpClient.PutAsync($"PessoaFisicaContato/{codigo}/Deletar", data);
                if (TratarErrosResponse(resposta))
                {
                    var end = await DeserializarObjeto<RespostaServiceView>(resposta);
                    return ("OK", resposta.StatusCode, end);
                }
                else
                {
                    var erro = await DeserializarObjeto<ValidationProblemDetails>(resposta);
                    return (erro.Title, resposta.StatusCode, null);
                }
            }catch (Exception ex)
            {
                return (ex.Message, HttpStatusCode.InternalServerError, null);
            }
            
        }

        //endereço        
        public async Task<(string? mensagem, HttpStatusCode? status, RespostaServiceView? endereco)> CadastrarEndereco(CadastrarEnderecoServiceCommand endereco)
        {            
            endereco.CodigoServicoCadastro = _CodigoServico;

            var data = ObterConteudo(endereco);
            var resposta = await _httpClient.PostAsync($"PessoaFisicaEndereco", data);
            var retorno = await resposta.Content.ReadAsStringAsync();
            if (TratarErrosResponse(resposta))
            {
                var end = await DeserializarObjeto<RespostaServiceView>(resposta);
                return ("OK", resposta.StatusCode, end);
            }
            else
            {
                var erro = await DeserializarObjeto<ValidationProblemDetails>(resposta);
                return (erro.Title, resposta.StatusCode, null);
            }            
        }
        public async Task<(string? mensagem, HttpStatusCode? status, RespostaServiceView? endereco)> EditarEndereco(EditarEnderecoServiceCommand endereco, int? codigo)
        {            
            endereco.CodigoServicoModificacao = _CodigoServico;
            var data = ObterConteudo(endereco);
            var resposta = await _httpClient.PutAsync($"PessoaFisicaEndereco/{codigo}/Modificar", data);
            var retorno = await resposta.Content.ReadAsStringAsync();
            if (TratarErrosResponse(resposta))
            {
                var end = await DeserializarObjeto<RespostaServiceView>(resposta);
                return ("OK", resposta.StatusCode, end);
            }
            else
            {
                var erro = await DeserializarObjeto<ValidationProblemDetails>(resposta);
                return (erro.Title, resposta.StatusCode, null);
            }            
        }
        public async Task<(string? mensagem, HttpStatusCode? status, RespostaServiceView? endereco)> DeletarEndereco(DeletarServiceCommand endereco, int? codigo)
        {           
            endereco.CodigoServicoModificacao = _CodigoServico;
            var data = ObterConteudo(endereco);
            var resposta = await _httpClient.PutAsync($"PessoaFisicaEndereco/{codigo}/Deletar", data);
            var retorno = await resposta.Content.ReadAsStringAsync();
            if (TratarErrosResponse(resposta))
            {
                var end = await DeserializarObjeto<RespostaServiceView>(resposta);
                return ("OK", resposta.StatusCode, end);
            }
            else
            {
                var erro = await DeserializarObjeto<ValidationProblemDetails>(resposta);
                return (erro.Title, resposta.StatusCode, null);
            }            
        }
    }
}
