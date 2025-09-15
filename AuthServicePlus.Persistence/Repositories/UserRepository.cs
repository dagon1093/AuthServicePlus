using AuthServicePlus.Domain.Entities;
using AuthServicePlus.Domain.Interfaces;
using AuthServicePlus.Persistence.Context;
using Microsoft.EntityFrameworkCore;


namespace AuthServicePlus.Persistence.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;

        public UserRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByUsernameAsync(string username, bool track = true)
        {
            var query = _context.Users.Include(u => u.RefreshTokens).AsQueryable();
            if (!track) query = query.AsNoTracking();
            return await _context.Users.SingleOrDefaultAsync(u => u.Username == username);
        }

        public async Task AddUserAsync(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateUserAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }
        // добавим по мере необходимости другие методы

        public async Task<User?> GetByUserId(int userId)
        {
            return await _context.Users.SingleOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<User?> GetByRefreshToken(string refreshToken, bool track = true)
        {   
            var query = _context.Users.Include(u => u.RefreshTokens).AsQueryable();
            if (!track) query = query.AsNoTracking();
            return await _context.Users.SingleOrDefaultAsync(u => u.RefreshTokens.Any(t => t.Token == refreshToken));
        }

        public void AddRefreshToken(User user, RefreshToken token)
        {
            user.RefreshTokens.Add(token);
        }

        public bool RevokeRefreshToken(User user, string token)
        {
            var rt = user.RefreshTokens.FirstOrDefault(t => t.Token == token);
            if (rt != null || rt.RevokedAt != null)
                return false;
        
            rt.RevokedAt = DateTime.UtcNow;
            return true;
        }

        public Task SaveChangesAsync() => _context.SaveChangesAsync();

    }
}
