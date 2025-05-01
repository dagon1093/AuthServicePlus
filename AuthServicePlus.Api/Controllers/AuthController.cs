using AuthServicePlus.Application.DTOs;
using AuthServicePlus.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AuthServicePlus.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")] 
    public class AuthController: ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
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

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginUserDto dto)
        {
            try
            {
                var token = await _authService.LoginAsync(dto);
                return Ok(token);
            }
            catch(Exception ex) 
            {
                return Unauthorized(new { error = ex.Message });
            }
        }
    }
}
