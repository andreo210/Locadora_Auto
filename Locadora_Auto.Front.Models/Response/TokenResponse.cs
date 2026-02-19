namespace Locadora_Auto.Front.Models.Response
{
    public class TokenResponse
    {
        public UserDto? user { get; set; }
        public string? AccessToken { get; set; }
        public double? ExpiresIn { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime CriadoEm { get; set; }
        public bool Utilizado { get; set; }
    }
}
