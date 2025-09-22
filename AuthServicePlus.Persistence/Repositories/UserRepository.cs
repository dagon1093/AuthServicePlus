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
            return await query.SingleOrDefaultAsync(u => u.Username == username);
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

        public async Task<User?> GetByUserIdAsync(int userId)
        {
            var q = _context.Users.Include(u => u.RefreshTokens.Where(rt => rt.RevokedAt == null)).AsNoTracking();

            return await q.FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<User?> GetByRefreshTokenAsync(string refreshToken, bool track = true)
        {
            var rt = await _context.RefreshTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Token == refreshToken);

            if (rt == null) return null;
            if (!track) _context.Entry(rt.User).State = EntityState.Detached;
            return await _context.Users.SingleOrDefaultAsync(u => u.RefreshTokens.Any(t => t.Token == refreshToken));
        }

        public void AddRefreshToken(User user, RefreshToken token)
        {
            user.RefreshTokens.Add(token);
        }

        public bool RevokeRefreshToken(User user, string token)
        {
            var rt = user.RefreshTokens.FirstOrDefault(t => t.Token == token);
            if (rt == null || rt.RevokedAt != null)
                return false;

            rt.RevokedAt = DateTime.UtcNow;
            return true;
        }

        public Task<int> RevokeAllRefreshTokensAsync(int userId)
        {
            return _context.RefreshTokens
                .Where(t => t.UserId == userId && t.RevokedAt == null)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, _ => DateTime.UtcNow));
        }

        public async Task<User?> GetByIdWithTokensAsync(int id)
        {
            return await _context.Users
                .Include(u => u.RefreshTokens)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<RefreshToken?> GetRefreshTokenForUserAsync(int userId, int tokenId, bool track = true)
        {
            var q = _context.RefreshTokens.Where(t => t.Id == tokenId && t.UserId == userId);
            if ( !track ) q = q.AsNoTracking();

            return await q.FirstOrDefaultAsync();
        }

        public Task SaveChangesAsync() => _context.SaveChangesAsync();

    }
}
