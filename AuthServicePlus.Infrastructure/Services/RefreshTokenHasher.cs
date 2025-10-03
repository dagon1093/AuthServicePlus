using AuthServicePlus.Application.Interfaces;
using AuthServicePlus.Domain.Entities;
using AuthServicePlus.Infrastructure.Options;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;


namespace AuthServicePlus.Infrastructure.Services
{
    public class RefreshTokenHasher : IRefreshTokenHasher
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


      
        public (string rawToken, RefreshToken entity) Create(int userId, TimeSpan lifetime)
        {
            var raw = GenerateRawToken();
            var hash = ComputeHash(raw);

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
            if (rawToken is null)
            {
                throw new ArgumentNullException(nameof(rawToken));
            }

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_jwtOptions.Key));
            var bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawToken));
  
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        public bool Verify(string rawToken, string storedHash)
        {
            if ( rawToken is null )
            {
                throw new ArgumentNullException(nameof(rawToken));
            }

            if ( storedHash is null )
            {
                return false;
            }

            var computed = ComputeHash(rawToken);

            byte[] computeBytes = Convert.FromHexString(storedHash);
            byte[] storedBytes;

            try
            {
                storedBytes = Convert.FromHexString(storedHash);
            }
            catch (FormatException)
            {
                return false;
            }

            if (storedBytes.Length != computeBytes.Length)
            {
                return false;
            }

            return CryptographicOperations.FixedTimeEquals(computeBytes, storedBytes);

        }

    }

        
}
