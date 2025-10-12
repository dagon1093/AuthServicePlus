using AuthServicePlus.Application.DTOs;
using AuthServicePlus.Application.Interfaces;
using AuthServicePlus.Domain.Entities;
using AuthServicePlus.Domain.Interfaces;
using AuthServicePlus.Infrastructure.Options;
using AuthServicePlus.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AuthServicePlus.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IRefreshTokenHasher _refreshTokenHasher;
        public readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly JwtOptions _jwtOptions;
        private readonly ILogger<AuthService> _logger;



        public AuthService(IUserRepository userRepository, IPasswordHasher passwordHasher, IJwtTokenGenerator jwtTokenGenerator, IOptions<JwtOptions> jwtOptions, IRefreshTokenHasher refreshTokenHasher, ILogger<AuthService> logger)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _jwtTokenGenerator = jwtTokenGenerator;
            _jwtOptions = jwtOptions.Value;
            _refreshTokenHasher = refreshTokenHasher;
            _logger = logger;
        }

        public async Task RegisterAsync(RegisterUserDto dto)
        {
            var existingUser = await _userRepository.GetByUsernameAsync(dto.Username);

            if (existingUser is not null)
                throw new Exception("Пользователь уже существует");

            var hashedPassword = _passwordHasher.Hash(dto.Password);

            var user = new User
            {
                Username = dto.Username,
                PasswordHash = hashedPassword,
                Role = dto.Role
            };

            await _userRepository.AddUserAsync(user);
        }

        public async Task<AuthResponseDto> LoginAsync(LoginUserDto dto)
        {
            _logger.LogDebug("Checking username: {Username}", dto.Username);
            var user = await _userRepository.GetByUsernameAsync(dto.Username);

            if (user == null)
            {
                _logger.LogWarning("Username: {username} not found", dto.Username);
                throw new Exception("Пользователь не найден");
            }
            if (!_passwordHasher.Verify(dto.Password, user.PasswordHash))
            {

                throw new Exception("Неверный логин или пароль");
            }


            // создать access
            var access = _jwtTokenGenerator.GenerateToken(user);
            var accessTtlSeconds = (int)TimeSpan.FromMinutes(_jwtOptions.AccessTokenMinutes).TotalSeconds;

            // создать refresh
            var (rawRefresh, refreshEntity) = _refreshTokenHasher.Create(user.Id, TimeSpan.FromDays(_jwtOptions.RefreshTokenDays));
            user.RefreshTokens.Add(refreshEntity);

            await _userRepository.UpdateUserAsync(user);

            
            return new AuthResponseDto
            {
                AccessToken = access,
                RefreshToken = rawRefresh,
                ExpiresIn = accessTtlSeconds,
                TokenType = "Bearer"
            };

        }

        public async Task<AuthResponseDto> RefreshAsync(string rawRefresh)
        {
            _logger.LogDebug($"Compute hash for refreshingToken: {rawRefresh}");
            var hash = _refreshTokenHasher.ComputeHash(rawRefresh);
            _logger.LogDebug($"Hash computed: {hash}");

            _logger.LogDebug($"Получение токена по хэшу");
            var rt = await _userRepository.GetRefreshTokenByHashAsync(hash, includeUser: true, track: true);

            if (rt is null) throw new UnauthorizedAccessException("Invalid refresh token.");
            if (rt.RevokedAt != null) throw new UnauthorizedAccessException("Token was revoked.");
            if (rt.Expiration <= DateTime.UtcNow) throw new UnauthorizedAccessException("Token expired.");

            var newAccess = _jwtTokenGenerator.GenerateToken(rt.User);

            //Ротация
            rt.RevokedAt = DateTime.UtcNow;
            var (rawNew, newEntity) = _refreshTokenHasher.Create(rt.UserId, TimeSpan.FromDays(_jwtOptions.RefreshTokenDays));
            rt.User.RefreshTokens.Add(newEntity);

            _logger.LogDebug("Сохранение изменений");
            await _userRepository.SaveChangesAsync();

            return new AuthResponseDto
            {
                AccessToken = newAccess,
                RefreshToken = rawNew,
                ExpiresIn = _jwtOptions.AccessTokenMinutes * 60,
                TokenType = "Bearer"
            };



        }

        public async Task LogoutAsync(string refreshToken)
        {
            var refreshTokenHash = _refreshTokenHasher.ComputeHash(refreshToken);
            var user = await _userRepository.GetByRefreshTokenAsync(refreshTokenHash, track: true);
            if (user is null) return; // не возвращаем состояния

            _logger.LogDebug($"Попытка отзыва токена: {refreshToken}");
            var ok = _userRepository.RevokeRefreshToken(user, refreshTokenHash);
            if (ok)
            {
                _logger.LogDebug($"Токен {refreshToken} успешно отозван");
                await _userRepository.SaveChangesAsync();
            }
        }

        public async Task LogoutAllAsync(int userId)
        {
            await _userRepository.RevokeAllRefreshTokensAsync(userId);
            await _userRepository.SaveChangesAsync();
        }

        public async Task<IEnumerable<SessionDto>> GetSessionsAsync(int userId)
        {
            var user = await _userRepository.GetByIdWithTokensAsync(userId);
            if (user == null ) return Enumerable.Empty<SessionDto>();

            return user.RefreshTokens.Select(rt => new SessionDto
            {
                Id = rt.Id,
                CreatedAt = rt.CreatedAt,
                Expiration = rt.Expiration,
                RevokedAt = rt.RevokedAt
            });
        }

        public async Task<bool> RevokeSessionAsync(int userId, int tokenId)
        {
            var token = await _userRepository.GetRefreshTokenForUserAsync(userId, tokenId, track: true);
            if (token is null) return false;

            if (token.RevokedAt != null) return true;

            token.RevokedAt = DateTime.UtcNow;
            await _userRepository.SaveChangesAsync();
            return true;

        }
    }
}
