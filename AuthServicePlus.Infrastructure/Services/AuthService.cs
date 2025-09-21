using AuthServicePlus.Application.DTOs;
using AuthServicePlus.Application.Interfaces;
using AuthServicePlus.Domain.Entities;
using AuthServicePlus.Domain.Interfaces;
using AuthServicePlus.Infrastructure.Options;
using AuthServicePlus.Infrastructure.Services;
using Microsoft.Extensions.Options;

namespace AuthServicePlus.Persistence.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        public readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly JwtOptions _jwtOptions;


        public AuthService(IUserRepository userRepository, IPasswordHasher passwordHasher, IJwtTokenGenerator jwtTokenGenerator, IOptions<JwtOptions> jwtOptions)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _jwtTokenGenerator = jwtTokenGenerator;
            _jwtOptions = jwtOptions.Value;
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
            var user = await _userRepository.GetByUsernameAsync(dto.Username);

            if ( user == null || !_passwordHasher.Verify(dto.Password, user.PasswordHash))
            {
                throw new Exception("Неверный логин или пароль");
            }


            // создать access
            var access = _jwtTokenGenerator.GenerateToken(user);
            var accessTtlSeconds = 3600; //todo заменить на реальное значение из конфигурации

            // создать refresh
            var refresh = RefreshTokenFactory.Create(user.Id, TimeSpan.FromDays(_jwtOptions.RefreshTokenDays));
            user.RefreshTokens.Add(refresh);

            await _userRepository.UpdateUserAsync(user);

            
            return new AuthResponseDto
            {
                AccessToken = access,
                RefreshToken = refresh.Token,
                ExpiresIn = accessTtlSeconds,
                TokenType = "Bearer"
            };

        }

        public async Task<AuthResponseDto> RefreshAsync(string refreshToken)
        {
            var user = await _userRepository.GetByRefreshTokenAsync(refreshToken) ?? throw new UnauthorizedAccessException("Invalid refresh token.");

            var rt = user.RefreshTokens.First(t => t.Token == refreshToken);
            if (rt.RevokedAt != null) throw new UnauthorizedAccessException("Token has revoked.");
            if (rt.Expiration <= DateTime.UtcNow) throw new UnauthorizedAccessException("Token expired.");

            //ротация
            _userRepository.RevokeRefreshToken(user, refreshToken);
            var newRt = RefreshTokenFactory.Create(user.Id, TimeSpan.FromDays(7));
            _userRepository.AddRefreshToken(user,newRt);

            var newAccess = _jwtTokenGenerator.GenerateToken(user);
            await _userRepository.SaveChangesAsync();

            return new AuthResponseDto
            {
                AccessToken = newAccess,
                RefreshToken = newRt.Token,
                ExpiresIn = 3600,
                TokenType = "Bearer"
            };

        }

        public async Task LogoutAsync(string refreshToken)
        {
            var user = await _userRepository.GetByRefreshTokenAsync(refreshToken, track: true);
            if (user is null) return; // не возвращаем состояния

            var ok = _userRepository.RevokeRefreshToken(user, refreshToken);
            if (ok) await _userRepository.SaveChangesAsync();

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
    }
}
