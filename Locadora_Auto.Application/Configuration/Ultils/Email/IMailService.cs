using Locadora_Auto.Application.Jobs;

namespace Locadora_Auto.Application.Configuration.Ultils.Email
{
    public interface IMailService
    {
        Task EnviarEmailSolicitacao(MensagemEmailSolicitacao mensagem);
        (string Assunto, string Corpo) MontarEmailSolicitacao(MensagemEmailSolicitacao mensagem);
    }
}