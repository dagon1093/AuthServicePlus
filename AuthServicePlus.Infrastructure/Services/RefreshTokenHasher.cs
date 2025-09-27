using AuthServicePlus.Application.Interfaces;
using AuthServicePlus.Domain.Entities;
using AuthServicePlus.Infrastructure.Options;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AuthServicePlus.Infrastructure.Services
{
    public class RefreshTokenHasher: IRefreshTokenHasher
    {

        private readonly JwtOptions _jwtOptions;

        public RefreshTokenHasher(IOptions<JwtOptions> jwtOptions)
        {
            _jwtOptions = jwtOptions.Value;
        }

        private static string GenerateRawToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(bytes);
        }


        private static string HmacSha256(string input)
        {
            
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_jwtOptions.Key));
            var bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(input));

            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b  in bytes) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        public (string rawToken, RefreshToken entity) Create(int userId, TimeSpan lifetime)
        {
            var raw = GenerateRawToken();
            var hash = HmacSha256(raw);

            var now = DateTime.UtcNow;
            var entity = new RefreshToken
            {
                UserId = userId,
                TokenHash = hash,
                CreatedAt = now,
                Expiration = now.Add(lifetime)
            };

            return (raw, entity);
        }

        public string ComputeHash(string rawToken)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_jwtOptions.Key));
            var bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawToken));

            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes)
                sb.Append(b.ToString("x2")); 
            return sb.ToString();
        }

        public bool Verify(string rawToken, string salt, string storedHash) 
            => string.Equals(HmacSha256(rawToken), storedHash, StringComparison.Ordinal);

    }

        
}
