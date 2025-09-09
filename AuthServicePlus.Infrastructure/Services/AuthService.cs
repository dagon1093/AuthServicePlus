using AuthServicePlus.Application.DTOs;
using AuthServicePlus.Application.Interfaces;
using AuthServicePlus.Domain.Entities;
using AuthServicePlus.Domain.Interfaces;
using AuthServicePlus.Infrastructure.Services;

namespace AuthServicePlus.Persistence.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        public readonly IJwtTokenGenerator _jwtTokenGenerator;

        public AuthService(IUserRepository userRepository, IPasswordHasher passwordHasher, IJwtTokenGenerator jwtTokenGenerator)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _jwtTokenGenerator = jwtTokenGenerator;
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

            // создать refresh
            var refresh = RefreshTokenFactory.Create(user.Id, TimeSpan.FromDays(7));
            user.RefreshTokens.Add(refresh);

            await _userRepository.UpdateUserAsync(user);

            // создать access
            var access = _jwtTokenGenerator.GenerateToken(user);
            var accessTtlSeconds = 3600; // заменить на реальное значение из конфигурации
            return new AuthResponseDto
            {
                AccessToken = access,
                RefreshToken = refresh.Token,
                ExpiresIn = accessTtlSeconds,
                TokenType = "Bearer"
            };


        }
    }
}
