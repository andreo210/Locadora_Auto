namespace Locadora_Auto.Application.Services.Email
{
    public interface IMailService
    {
        Task EnviarEmailSolicitacao(MensagemEmailSolicitacao mensagem);
        (string Assunto, string Corpo) MontarEmailSolicitacao(MensagemEmailSolicitacao mensagem);
    }
}