using Hangfire;
using Locadora_Auto.Infra.Configuration;
using Locadora_Auto.Infra.ServiceHttp.Models.Views.Keycloak;
using Locadora_Auto.Infra.ServiceHttp.Servicos.LoginAdmin;
using System.Net;

namespace Locadora_Auto.Application.Services.JobsHangfire
{
    public class JobsLoginHandler : IJobsLoginHandler
    {
        private ILoginService _login;

        public JobsLoginHandler(ILoginService login)
        {
            _login = login;
        }

        [Obsolete]
        public void SetAdminTokenInterno()
        {
            SetBuscarTokenInterno();
            RecurringJob.AddOrUpdate("trocar o token interno", () =>
                SetBuscarTokenInterno()
             , "*/59 * * * *", TimeZoneInfo.Local);
        }

        public void SetBuscarTokenInterno()
        {
            (TokenServiceView dados, HttpStatusCode status, string mensagem) retorno = _login.ObterAutenticacaoAdminInterno();
            if (retorno.status == HttpStatusCode.OK)
            {
                TokenServiceView KEYCLOAKAUTH = _login.ObterAutenticacaoAdminInterno().dados;
                ValuesConfig.KEYCLOAKAUTHINTERNO = KEYCLOAKAUTH;
            }

        }

        [Obsolete]
        public void SetAdminTokenExterno()
        {
            SetBuscarTokenExterno();
            RecurringJob.AddOrUpdate("trocar o token Externo", () =>
                SetBuscarTokenExterno()
             , "*/59 * * * *", TimeZoneInfo.Local);
        }

        public void SetBuscarTokenExterno()
        {
            (TokenServiceView dados, HttpStatusCode status, string mensagem) retorno = _login.ObterAutenticacaoAdminExterno();
            if (retorno.status == HttpStatusCode.OK)
            {
                TokenServiceView KEYCLOAKAUTH = _login.ObterAutenticacaoAdminExterno().dados;
                ValuesConfig.KEYCLOAKAUTHEXTERNO = KEYCLOAKAUTH;
            }

        }   
        
    }
}
