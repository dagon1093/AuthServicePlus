using AuthServicePlus.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AuthServicePlus.Infrastructure.Services
{
    public class PasswordHasher: IPasswordHasher
    {
        public string Hash(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        public bool Verify(string password, string hash)
        {
            var hashedInput = Hash(password);
            return hashedInput == hash;
        }
    }
}
