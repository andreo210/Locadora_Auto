namespace Locadora_Auto.Domain.Entidades
{
    public class RefreshToken
    {

        public int Id { get; set; }
        public string Token { get; set; } = null!;
        public DateTime ExpiraEm { get; set; }
        public bool Revogado { get; set; } = false;
        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
        public string UserId { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;
    }
}


