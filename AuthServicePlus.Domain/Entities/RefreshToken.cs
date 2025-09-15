

namespace AuthServicePlus.Domain.Entities
{
    public class RefreshToken
    {
        public int Id { get; set; }
        public string Token { get; set; }
        public DateTime Expiration { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? RevokedAt { get; set; } = null;
        public int UserId { get; set; }
        public User User { get; set; } = null!; //для навигации EF


        public RefreshToken() { }

        public RefreshToken(string token, DateTime expiration, DateTime createdAt, DateTime? revokedAt, int userId)
        {
            Token = token;
            Expiration = expiration;
            CreatedAt = createdAt;
            RevokedAt = revokedAt;
            UserId = userId;

        }

    } 
}
