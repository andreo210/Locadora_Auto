using Locadora_Auto.Infra.ServiceHttp.Models.Commands.CadBase;
using Locadora_Auto.Infra.ServiceHttp.Models.Views.CadBase;
using System.Net;

namespace Locadora_Auto.Infra.ServiceHttp.Servicos.CadastroBase.CadadastroBase
{
    public interface ICadastroBaseService
    {
        //pessoa fisica
        Task<(string? mensagem, HttpStatusCode? status, PessoaFisicaServiceView? usuario)> CadastrarUsuario(CadastroPessoaFisicaServiceCommand user);
        Task<(string? mensagem, HttpStatusCode? status, PessoaFisicaServiceView? usuario)> EditarUsuario(EditarPessoaFisicaServiceCommand usuario, int? CodigoCentral);
        Task<(string? mensagem, HttpStatusCode? status, PessoaFisicaServiceView? usuario)> EditarNomeUsuario(AlterarNomeServiceCommand usuario, int? CodigoCentral);


        //contato
        Task<(string? mensagem, HttpStatusCode? status, RespostaServiceView? contato)> CadastrarContato(CadastrarContatoServiceCommand contatos);
        Task<(string? mensagem, HttpStatusCode? status, RespostaServiceView? contato)> EditarContato(EditarContatoServiceCommand contatos, int Codigo);
        Task<(string? mensagem, HttpStatusCode? status, RespostaServiceView? contato)> DeletarContato(DeletarServiceCommand contato, int? codigo);



        //endereço
        Task<(string? mensagem, HttpStatusCode? status, RespostaServiceView? endereco)> CadastrarEndereco(CadastrarEnderecoServiceCommand endereco);
        Task<(string? mensagem, HttpStatusCode? status, RespostaServiceView? endereco)> EditarEndereco(EditarEnderecoServiceCommand endereco, int? codigo);
        Task<(string? mensagem, HttpStatusCode? status, RespostaServiceView? endereco)> DeletarEndereco(DeletarServiceCommand endereco, int? codigo);




    }
}
