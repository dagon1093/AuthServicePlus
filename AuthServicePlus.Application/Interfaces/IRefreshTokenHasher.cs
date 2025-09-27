using AuthServicePlus.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthServicePlus.Application.Interfaces
{
    public interface IRefreshTokenHasher
    {
        (string rawToken, RefreshToken entity) Create(int userId, TimeSpan lifetime);

        bool Verify(string rawToken, string  salt, string storedHash );

        string ComputeHash(string rawToken);
    }
}
