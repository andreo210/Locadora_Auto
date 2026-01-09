using Locadora_Auto.Application.Configuration;
using Locadora_Auto.Application.Jobs;
using Locadora_Auto.Application.Models.Dto;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Locadora_Auto.Application.Services.Email;

public class MailService(
    IOptions<EmailConfig> emailSettings,
    IWebHostEnvironment _enviroment,
    IConfiguration config) : IMailService
{
    private readonly EmailConfig _mailSettings = emailSettings.Value;

    static MailService()
    {
        ServicePointManager.ServerCertificateValidationCallback =
        delegate (object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        };
    }

    public async Task EnviarEmailSolicitacao(MensagemEmailSolicitacao mensagem)
    {
        var (assunto, corpo) = MontarEmailSolicitacao(mensagem);
        await EnviarEmail(mensagem.Email, assunto, corpo);
    }

    public (string Assunto, string Corpo) MontarEmailSolicitacao(MensagemEmailSolicitacao mensagem)
    {
        string corpo = ObterSaudacaoMensagem(mensagem.NomePessoa);
        corpo += ObterCorpoMensagem(mensagem);
        corpo += ObterAssinaturaMensagem();

        return ("Solicitação para o Programa de Autorização Especial", corpo);
    }

    private static string ObterSaudacaoMensagem(string nomeRequerente)
    {
        return "<meta http-equiv='Content-Type' content='text/html; charset=utf-8'><body>" +
            "<img src=cid:brasao-pg  id='img' alt='imagem brasão da Prefeitura de Praia Grande' width='60px' height='60px'/>" +
            "<b> PREFEITURA MUNICIPAL DE PRAIA GRANDE </b> " +
            $"<br /><br /> Olá, <b>{nomeRequerente}</b> , ";
    }

    private string ObterCorpoMensagem(MensagemEmailSolicitacao mensagem)
    {
        var areaUsuario = config.GetSection("ApiConfig").GetSection("AreaUsuario").Value;
        var linkAcompanhamento = config.GetSection("ApiConfig").GetSection("SiteAtualizacao").Value;
        
        var corpo = $"<br>A sua solicitação para o <b>Programa de Autorização Especial</b> ";

        if (mensagem.TipoAndamento == 1)
        {
            corpo += $" foi criada.<br> ";
        }
        else if (mensagem.TipoAndamento == 2)
        {
            corpo += $" foi finalizada.<br> ";
        }
        else
        {
            corpo += $" teve o status alterado.<br> ";
        }

        corpo +=
            $"<br/><hr><b>Status atual: </b>{mensagem.DescricaoTipoAndamento}<br>" +
            $"<br/><b>Data: </b>{DateTime.Now.ToShortDateString()}<br>" +
            $"<br/><b>Andamento: </b> {mensagem.Observacao}" +
            $"<hr>";

        corpo += $"Você pode acompanhar o andamento pelo <a href={linkAcompanhamento}>site</a> ou pela <a href={areaUsuario}>área do usuário</a> ";
        return corpo;
    }

    private static string ObterAssinaturaMensagem()
    {
        return "<p>Atenciosamente, </p>" +
            "<p>Secretaria de Finanças. </p> <br /><br />" +
            "<br /><br />*** Este e-mail foi enviado por um sistema automático que não processa respostas. ***" +
            "<br />*** Não responda a esta mensagem. ***" +
            "</body>";
    }

    public async Task EnviarEmail(string email, string assunto, string corpo)
    {
        string pasta = @"img\logo-email.png";
        string caminho_WebRoot = _enviroment.WebRootPath;
        string caminhoBrasao = Path.Combine(caminho_WebRoot, pasta);
        LinkedResource objBrasao = new(caminhoBrasao, MediaTypeNames.Image.Jpeg);
        objBrasao.ContentId = "brasao-pg";
        objBrasao.ContentType.Name = "img";

        SmtpClient client = new();
        MailMessage mailMessage = new();
        EmailDto mailRequest = new();

        mailMessage.To.Add(email);
        mailMessage.From = new MailAddress(_mailSettings.Mail, "Secretaria de Finanças de Praia Grande", Encoding.UTF8);
        mailMessage.Subject = assunto; //Assunto do e - mail
        mailMessage.IsBodyHtml = true;// se o conteúdo do e-mail for HTML
        mailMessage.Body = mailRequest.Body;//Conteúdo do e-mai
        mailMessage.BodyEncoding = Encoding.UTF8;
        mailMessage.BodyTransferEncoding = TransferEncoding.EightBit;

        // Cria o cliente SMTP com as configurações do seu provedor de e-mail
        client.Port = _mailSettings.Port;
        client.Host = _mailSettings.Host;
        client.EnableSsl = true;
        client.UseDefaultCredentials = false;
        client.Credentials = new NetworkCredential(_mailSettings.Mail, _mailSettings.Password);// Cria as credenciais do remetente
        client.DeliveryMethod = SmtpDeliveryMethod.Network;
        client.EnableSsl = _mailSettings.EnableSSL;

        // Anexar a imagem ao corpo do e-mail
        var formatarCorpo = AlternateView.CreateAlternateViewFromString(corpo, null, MediaTypeNames.Text.Html);
        formatarCorpo.LinkedResources.Add(objBrasao);
        mailMessage.AlternateViews.Add(formatarCorpo);

        await client.SendMailAsync(mailMessage);
    }
}