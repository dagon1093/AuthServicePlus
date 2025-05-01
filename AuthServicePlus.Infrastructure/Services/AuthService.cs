using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AuthServicePlus.Application.DTOs;
using AuthServicePlus.Application.Interfaces;
using AuthServicePlus.Domain.Entities;
using AuthServicePlus.Domain.Interfaces;

namespace AuthServicePlus.Persistence.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;

        public AuthService(IUserRepository userRepository, IPasswordHasher passwordHasher)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
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

            await _userRepository.AddAsync(user);
        }
    }
}
