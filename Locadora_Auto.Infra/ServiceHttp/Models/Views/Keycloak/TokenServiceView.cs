namespace Locadora_Auto.Infra.ServiceHttp.Models.Views.Keycloak
{
    public class TokenServiceView
    {
        public TokenServiceView()
        {
            date_time_generation = DateTime.Now;
        }
        public string? access_token { get; set; }
        public int? expires_in { get; set; }
        public int? refresh_expires_in { get; set; }
        public string? token_type { get; set; }
        public string? not_before_policy { get; set; }
        public string? scope { get; set; }
        public DateTime? date_time_generation { get; private set; }
    }
}
