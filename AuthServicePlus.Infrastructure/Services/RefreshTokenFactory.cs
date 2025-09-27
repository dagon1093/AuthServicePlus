using AuthServicePlus.Domain.Entities;
using System.Security.Cryptography;


namespace AuthServicePlus.Infrastructure.Services
{
    public class RefreshTokenFactory
    {
        [Obsolete("Токен генерится через RefreshTokenHasher")]
        public static RefreshToken Create(int userId, TimeSpan lifetime)
        {
            // криптослучайный токен
            var tokenBytes = new byte[64];
            RandomNumberGenerator.Fill(tokenBytes);
            var token = Convert.ToBase64String(tokenBytes); 

            return new RefreshToken(
                token: token,
                expiration: DateTime.UtcNow.Add(lifetime),
                createdAt: DateTime.UtcNow,
                revokedAt: null,
                userId: userId
            );
        }
    }
}
