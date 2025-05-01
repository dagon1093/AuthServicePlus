using AuthServicePlus.Application.DTOs;
using AuthServicePlus.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AuthServicePlus.Api.Controllers
{
    public class AuthController: ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        public async Task<IActionResult> Register([FromBody] RegisterUserDto dto)
        {
            try
            {
                await _authService.RegisterAsync(dto);
                return Ok(new { message = "Пользователь успешно зарегестрирован" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message});
            }
        }
    }
}
