using AuthServicePlus.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AuthServicePlus.Infrastructure.Services
{
    public class RefreshTokenFactory
    {
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
