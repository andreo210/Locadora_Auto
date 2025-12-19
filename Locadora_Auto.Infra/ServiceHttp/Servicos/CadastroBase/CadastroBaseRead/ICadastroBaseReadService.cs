using Locadora_Auto.Infra.ServiceHttp.Models.Views.CadBase;
using System.Net;

namespace Locadora_Auto.Infra.ServiceHttp.Servicos.CadastroBase.CadastroBaseRead
{
    public interface ICadastroBaseReadService
    {
        //pessoa fisica
        Task<(string? mensagem, HttpStatusCode? status, PessoaFisicaServiceView? usuario)> ObterPessoaFisicaBasicoPorId(int codigoCentral);
        Task<(string? mensagem, HttpStatusCode? statusCode, PessoaFisicaDetalhadoServiceView? usuario)> ObterPessoaFisicaDetahadoPorId(int codigoCentral);
        Task<(string? mensagem, HttpStatusCode? status, PessoaFisicaServiceView? usuario)> ObterPessoaFisicaBasicoPorCpf(string cpf);
        Task<(string? mensagem, HttpStatusCode? statusCode, PessoaFisicaDetalhadoServiceView? usuario)> ObterPessoaFisicaDetahadoPorCpf(string cpf);
        Task<(string? mensagem, HttpStatusCode? statusCode, PessoaFisicaReceitaServiceView? usuario)> ObterPessoaFisicaReceitaFederal(string cpf);

        //contatos
        Task<(string? mensagem, HttpStatusCode? status, List<ContatoServiceView>? contato)> ObterContatoPorCodigoCentral(int? codigoCentral);
        Task<(string? mensagem, HttpStatusCode? status, ContatoServiceView? contato)> ObterContatoPorCodigo(int? codigo);


        //endereco
        Task<(string? mensagem, HttpStatusCode? status, List<EnderecoServiceView>? endereco)> ObterEnderecoPorCodigoCentral(int? codigoCentral);
        Task<(string? mensagem, HttpStatusCode? status, EnderecoServiceView? endereco)> ObterEnderecoPorCodigo(int? codigo);
        Task<(string? mensagem, HttpStatusCode? status, List<EnderecoServiceView>? endereco)> ObterEnderecoPorCpf(string? cpf);
    }
}
