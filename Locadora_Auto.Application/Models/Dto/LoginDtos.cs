namespace Locadora_Auto.Application.Models.Dto
{
    public class LoginDto
    {
        public string UserName { get; set; } = null!;
        public string Password { get; set; } = null!;
    }

    public class LoginResponseDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public DateTime AccessTokenExpiraEm { get; set; }

        public string RefreshToken { get; set; } = string.Empty;
        public DateTime RefreshTokenExpiraEm { get; set; }
    }

    public class RefreshTokenRequestDto
    {
        public string RefreshToken { get; set; } = string.Empty;
    }


    public class RefreshTokenDto
    {
        public int Id { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiraEm { get; set; }
        public bool Revogado { get; set; }
        public DateTime CriadoEm { get; set; }

        public string UserId { get; set; } = string.Empty;
        public UserDto? User { get; set; }
    }

    public class TokenDto
    {
        public UserDto? user { get; set; }
        public string? AccessToken { get; set; }
        public double? ExpiresIn { get; set; }       
        public string? RefreshToken { get; set; }
        public DateTime CriadoEm { get; set; }
        public bool Utilizado { get; set; }
    }

}
