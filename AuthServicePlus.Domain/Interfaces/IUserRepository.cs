using AuthServicePlus.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthServicePlus.Domain.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByUsernameAsync(string username, bool track = true);
        Task AddUserAsync(User user);
        Task UpdateUserAsync(User user);
        Task<User?> GetByUserId(int userId);
        Task<User?> GetByRefreshTokenAsync(string refreshToken, bool track = true);
        void AddRefreshToken(User user, RefreshToken token);
        bool RevokeRefreshToken(User user, string refreshToken);
        Task SaveChangesAsync();
    }
}
