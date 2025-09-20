using AuthServicePlus.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthServicePlus.Application.Interfaces
{
    public interface IAuthService
    {
        Task RegisterAsync(RegisterUserDto dto);
        Task<AuthResponseDto> LoginAsync(LoginUserDto dto);
        Task<AuthResponseDto> RefreshAsync(string refreshTokem);
        Task LogoutAsync(string refreshToken);
    }
}
